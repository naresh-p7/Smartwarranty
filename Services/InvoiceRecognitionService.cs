using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Docnet.Core;
using Docnet.Core.Models;
using Tesseract;
using UglyToad.PdfPig;

namespace SmartWarranty_2.Services;

public sealed record InvoiceRecognitionResult(
    string? ProductName,
    string? ModelNumber,
    string? SerialNumber,
    DateTime? PurchaseDate,
    string? SellerName,
    int? WarrantyDurationMonths,
    bool HasRecognizedText);

public interface IInvoiceRecognitionService
{
    Task<InvoiceRecognitionResult> RecognizeAsync(Stream invoice, string fileExtension);
}

public sealed class InvoiceRecognitionService : IInvoiceRecognitionService
{
    private static readonly string[] DateFormats =
    {
        "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy",
        "yyyy-MM-dd", "MMM d, yyyy", "MMMM d, yyyy"
    };

    public async Task<InvoiceRecognitionResult> RecognizeAsync(Stream invoice, string fileExtension)
    {
        string text;
        if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using var pdfStream = new MemoryStream();
            await invoice.CopyToAsync(pdfStream);
            var pdfBytes = pdfStream.ToArray();
            using (var pdfTextStream = new MemoryStream(pdfBytes))
            using (var document = PdfDocument.Open(pdfTextStream))
            {
                text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                text = RecognizeScannedPdf(pdfBytes);
            }
        }
        else
        {
            using var memoryStream = new MemoryStream();
            await invoice.CopyToAsync(memoryStream);
            using var engine = CreateEngine();
            using var image = Pix.LoadFromMemory(memoryStream.ToArray());
            using var page = engine.Process(image);
            text = page.GetText() ?? string.Empty;
        }

        return new InvoiceRecognitionResult(
            ExtractLabeledValue(text, @"(?:product|item)(?:\s+name)?|description"),
            ExtractLabeledValue(text, @"model(?:\s*(?:no\.?|number))?|sku"),
            ExtractLabeledValue(text, @"serial(?:\s*(?:no\.?|number))?"),
            ExtractDate(text),
            ExtractLabeledValue(text, @"seller|sold\s+by|merchant|vendor|store"),
            ExtractWarrantyDuration(text),
            !string.IsNullOrWhiteSpace(text));
    }

    private static string RecognizeScannedPdf(byte[] pdfBytes)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException("Scanned PDF recognition requires Windows 6.1 or later. Upload a text PDF or an image instead.");
        }

        using var engine = CreateEngine();
        using var document = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1600, 2200));
        var text = new StringBuilder();
        var pageCount = Math.Min(document.GetPageCount(), 3);

        for (var index = 0; index < pageCount; index++)
        {
            using var pageReader = document.GetPageReader(index);
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var pixels = pageReader.GetImage();
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bounds = new Rectangle(0, 0, width, height);
            var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            using var png = new MemoryStream();
            bitmap.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            using var image = Pix.LoadFromMemory(png.ToArray());
            using var recognizedPage = engine.Process(image);
            text.AppendLine(recognizedPage.GetText());
        }

        return text.ToString();
    }

    private static TesseractEngine CreateEngine()
    {
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!File.Exists(Path.Combine(tessdataPath, "eng.traineddata")))
        {
            throw new InvalidOperationException("Local invoice recognition data is not installed.");
        }

        return new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
    }

    private static string? ExtractLabeledValue(string text, string labelPattern)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*(?:{labelPattern})\s*[:#-]\s*(?<value>[^\r\n]{{1,120}})",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static DateTime? ExtractDate(string text)
    {
        var match = Regex.Match(
            text,
            @"(?im)^\s*(?:purchase|invoice|order|transaction|sale)\s+date\s*[:#-]?\s*(?<value>[A-Za-z0-9,./ -]{5,24})",
            RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim();
        return DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date.Date
            : null;
    }

    private static int? ExtractWarrantyDuration(string text)
    {
        var match = Regex.Match(
            text,
            @"(?i)(?:warranty\s*(?:period|duration|term)?\s*[:#-]?\s*(?<number>\d{1,3})\s*(?<unit>years?|months?)|(?<number>\d{1,3})\s*(?<unit>years?|months?)\s+(?:manufacturer\s+)?warranty)",
            RegexOptions.CultureInvariant);
        if (!match.Success || !int.TryParse(match.Groups["number"].Value, out var duration))
        {
            return null;
        }

        var months = match.Groups["unit"].Value.StartsWith("year", StringComparison.OrdinalIgnoreCase)
            ? duration * 12
            : duration;
        return months is >= 1 and <= 240 ? months : null;
    }
}
