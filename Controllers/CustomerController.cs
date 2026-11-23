using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;
using SmartWarranty_2.Services;
using SmartWarranty_2.ViewModels;

namespace SmartWarranty_2.Controllers;

[Authorize(Roles = "Customer")]
[ServiceFilter(typeof(WarrantyExpiryNotificationFilter))]
public class CustomerController : Controller
{
    private readonly SmartWarrantyDbContext _context;
    private readonly DashboardService _dashboardService;
    private readonly IInvoiceRecognitionService _invoiceRecognitionService;

    public CustomerController(
        SmartWarrantyDbContext context,
        DashboardService dashboardService,
        IInvoiceRecognitionService invoiceRecognitionService)
    {
        _context = context;
        _dashboardService = dashboardService;
        _invoiceRecognitionService = invoiceRecognitionService;
    }

    private int CurrentCustomerId => int.Parse(User.FindFirstValue("UserId") ?? "0");

    public async Task<IActionResult> Dashboard()
    {
        var customer = await _context.Customers.FindAsync(CurrentCustomerId);
        if (customer == null) return RedirectToAction("Logout", "Account");

        var metrics = await _dashboardService.GetCustomerDashboardMetricsAsync(CurrentCustomerId);
        var recentProducts = await _context.Products.Where(p => p.CustomerId == CurrentCustomerId).OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync();
        var warrantyItems = await _context.Warranties.Include(w => w.Product).Where(w => w.Product.CustomerId == CurrentCustomerId).OrderBy(w => w.ExpiryDate).Take(5).ToListAsync();
        var recentRequests = await _context.ServiceRequests.Where(s => s.CustomerId == CurrentCustomerId).OrderByDescending(s => s.RequestDate).Take(5).ToListAsync();

        ViewBag.Metrics = metrics;
        ViewBag.RecentProducts = recentProducts;
        ViewBag.UpcomingWarranties = warrantyItems;
        ViewBag.RecentRequests = recentRequests;
        return View(customer);
    }

    public async Task<IActionResult> Profile()
    {
        var customer = await _context.Customers.FindAsync(CurrentCustomerId);
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(Customer customer)
    {
        var existing = await _context.Customers.FindAsync(CurrentCustomerId);
        if (existing == null) return NotFound();

        existing.FirstName = customer.FirstName;
        existing.LastName = customer.LastName;
        existing.Email = customer.Email;
        existing.PhoneNumber = customer.PhoneNumber;
        existing.Address = customer.Address;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.CustomerId == CurrentCustomerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == CurrentCustomerId);
        if (product == null)
        {
            return NotFound();
        }

        if (await _context.ServiceRequests.AnyAsync(s => s.ProductId == id))
        {
            TempData["ErrorMessage"] = "This product has service request history and cannot be deleted.";
            return RedirectToAction(nameof(Products));
        }

        var storedFileNames = product.Documents.Select(d => d.StoredFileName).ToList();
        var warrantyIds = await _context.Warranties
            .Where(w => w.ProductId == product.Id)
            .Select(w => w.Id)
            .ToArrayAsync();
        if (warrantyIds.Length > 0)
        {
            var warrantyNotifications = await _context.Notifications
                .Where(n => n.WarrantyId.HasValue && warrantyIds.Contains(n.WarrantyId.Value))
                .ToListAsync();
            _context.Notifications.RemoveRange(warrantyNotifications);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        foreach (var storedFileName in storedFileNames)
        {
            var path = Path.Combine(uploadsPath, storedFileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        TempData["SuccessMessage"] = "Product and its warranties and documents were deleted.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> RegisterProduct()
    {
        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Categories = await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync();
        return View(new ProductRegistrationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterProduct(ProductRegistrationViewModel model)
    {
        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Categories = await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync();

        var existingBrand = model.BrandId.HasValue
            ? await _context.Brands.FindAsync(model.BrandId.Value)
            : null;
        if (model.BrandId.HasValue)
        {
            if (existingBrand == null)
            {
                ModelState.AddModelError(nameof(model.BrandId), "Select a valid brand.");
            }
        }
        else if (string.IsNullOrWhiteSpace(model.BrandName))
        {
            ModelState.AddModelError(nameof(model.BrandName), "Select a brand or enter its name.");
        }

        if (model.PurchaseDate.Date > DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.PurchaseDate), "Purchase date cannot be in the future.");
        }

        var categoryExists = await _context.ProductCategories.AnyAsync(c => c.Id == model.CategoryId);
        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Select a valid category.");
        }

        var serialNumber = model.SerialNumber?.Trim() ?? string.Empty;
        if (await _context.Products.AnyAsync(p => p.SerialNumber == serialNumber))
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "A product with this serial number is already registered.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Brand brand;
        if (existingBrand != null)
        {
            brand = existingBrand;
        }
        else
        {
            var brandName = model.BrandName!.Trim();
            brand = await _context.Brands.FirstOrDefaultAsync(b => b.Name == brandName)
                ?? new Brand { Name = brandName };
            if (brand.Id == 0)
            {
                _context.Brands.Add(brand);
            }
        }

        var product = new Product
        {
            ProductCode = "PRD-" + Guid.NewGuid().ToString()[..8].ToUpper(),
            Name = model.Name.Trim(),
            Brand = brand,
            BrandId = brand.Id,
            CategoryId = model.CategoryId,
            ModelNumber = model.ModelNumber.Trim(),
            SerialNumber = serialNumber,
            PurchaseDate = model.PurchaseDate,
            SellerName = model.SellerName.Trim(),
            CustomerId = CurrentCustomerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Product registered successfully.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Warranties()
    {
        var warranties = await _context.Warranties
            .Include(w => w.Product)
            .ThenInclude(p => p.Brand)
            .Where(w => w.Product.CustomerId == CurrentCustomerId)
            .OrderByDescending(w => w.ExpiryDate)
            .ToListAsync();

        foreach (var item in warranties)
        {
            item.Product = item.Product ?? new Product();
        }

        return View(warranties);
    }

    public async Task<IActionResult> AddWarranty(int? productId = null)
    {
        var products = await _context.Products.Where(p => p.CustomerId == CurrentCustomerId).ToListAsync();
        ViewBag.Products = products;
        return View(new WarrantyRegistrationViewModel { ProductId = productId ?? products.FirstOrDefault()?.Id ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWarranty(WarrantyRegistrationViewModel model)
    {
        var products = await _context.Products.Where(p => p.CustomerId == CurrentCustomerId).ToListAsync();
        ViewBag.Products = products;

        if (!products.Any(p => p.Id == model.ProductId))
        {
            ModelState.AddModelError(nameof(model.ProductId), "Select one of your registered products.");
        }

        if (model.StartDate.Date > DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.StartDate), "Warranty start date cannot be in the future.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var expiryDate = model.StartDate.Date.AddMonths(model.DurationMonths);
        var warranty = new Warranty
        {
            ProductId = model.ProductId,
            StartDate = model.StartDate.Date,
            DurationMonths = model.DurationMonths,
            ExpiryDate = expiryDate,
            WarrantyType = model.WarrantyType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Warranty added successfully.";
        return RedirectToAction(nameof(Warranties));
    }

    public async Task<IActionResult> Documents()
    {
        var docs = await _context.ProductDocuments
            .Include(d => d.Product)
            .Where(d => d.Product.CustomerId == CurrentCustomerId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return View(docs);
    }

    public async Task<IActionResult> UploadDocument(int productId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == CurrentCustomerId);
        if (product == null) return Forbid();
        ViewBag.Product = product;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int productId, IFormFile file, string documentType)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CustomerId == CurrentCustomerId);
        if (product == null) return Forbid();

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file.";
            return RedirectToAction(nameof(Documents));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = "Unsupported file type.";
            return RedirectToAction(nameof(Documents));
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File size must be less than 5 MB.";
            return RedirectToAction(nameof(Documents));
        }

        InvoiceRecognitionResult? recognition = null;
        if (documentType == "Purchase Invoice")
        {
            await using var invoiceStream = file.OpenReadStream();
            recognition = await _invoiceRecognitionService.RecognizeAsync(invoiceStream, extension);
        }

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var storedName = Guid.NewGuid() + extension;
        var fullPath = Path.Combine(uploadsPath, storedName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _context.ProductDocuments.Add(new ProductDocument
        {
            ProductId = productId,
            DocumentType = documentType,
            FileName = file.FileName,
            StoredFileName = storedName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        if (recognition != null)
        {
            TempData["InvoiceReview"] = JsonSerializer.Serialize(new InvoiceReviewViewModel
            {
                ProductId = product.Id,
                ProductName = recognition.ProductName ?? product.Name,
                ModelNumber = recognition.ModelNumber ?? product.ModelNumber,
                SerialNumber = recognition.SerialNumber ?? product.SerialNumber,
                PurchaseDate = recognition.PurchaseDate ?? product.PurchaseDate,
                SellerName = recognition.SellerName ?? product.SellerName,
                WarrantyDurationMonths = recognition.WarrantyDurationMonths,
                WarrantyStartDate = recognition.PurchaseDate ?? product.PurchaseDate,
                ExtractedTextFound = recognition.HasRecognizedText
            });
            return RedirectToAction(nameof(ReviewInvoice));
        }

        TempData["SuccessMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Documents));
    }

    public IActionResult ReviewInvoice()
    {
        var serializedReview = TempData["InvoiceReview"] as string;
        if (string.IsNullOrEmpty(serializedReview))
        {
            return RedirectToAction(nameof(Products));
        }

        var model = JsonSerializer.Deserialize<InvoiceReviewViewModel>(serializedReview);
        if (model == null)
        {
            return RedirectToAction(nameof(Products));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewInvoice(InvoiceReviewViewModel model)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == model.ProductId && p.CustomerId == CurrentCustomerId);
        if (product == null)
        {
            return NotFound();
        }

        if (model.PurchaseDate.Date > DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.PurchaseDate), "Purchase date cannot be in the future.");
        }

        if (await _context.Products.AnyAsync(p =>
                p.Id != model.ProductId &&
                p.SerialNumber == model.SerialNumber.Trim()))
        {
            ModelState.AddModelError(nameof(model.SerialNumber), "A product with this serial number is already registered.");
        }

        if (model.WarrantyDurationMonths.HasValue)
        {
            var warrantyStartDate = model.WarrantyStartDate?.Date ?? model.PurchaseDate.Date;
            if (warrantyStartDate > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.WarrantyStartDate), "Warranty start date cannot be in the future.");
            }

            if (string.IsNullOrWhiteSpace(model.WarrantyType))
            {
                ModelState.AddModelError(nameof(model.WarrantyType), "Enter the warranty type.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        product.Name = model.ProductName.Trim();
        product.ModelNumber = model.ModelNumber.Trim();
        product.SerialNumber = model.SerialNumber.Trim();
        product.PurchaseDate = model.PurchaseDate.Date;
        product.SellerName = model.SellerName.Trim();

        if (model.WarrantyDurationMonths.HasValue)
        {
            var startDate = model.WarrantyStartDate?.Date ?? model.PurchaseDate.Date;
            _context.Warranties.Add(new Warranty
            {
                ProductId = product.Id,
                StartDate = startDate,
                DurationMonths = model.WarrantyDurationMonths.Value,
                ExpiryDate = startDate.AddMonths(model.WarrantyDurationMonths.Value),
                WarrantyType = model.WarrantyType,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = model.WarrantyDurationMonths.HasValue
            ? "Invoice details saved and warranty added."
            : "Invoice details saved.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> DownloadDocument(int id)
    {
        var document = await _context.ProductDocuments.Include(d => d.Product).FirstOrDefaultAsync(d => d.Id == id);
        if (document == null || document.Product.CustomerId != CurrentCustomerId)
        {
            return Forbid();
        }

        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", document.StoredFileName);
        return PhysicalFile(path, document.ContentType, document.FileName);
    }

    public async Task<IActionResult> DeleteDocument(int id)
    {
        var document = await _context.ProductDocuments.Include(d => d.Product).FirstOrDefaultAsync(d => d.Id == id);
        if (document == null || document.Product.CustomerId != CurrentCustomerId)
        {
            return Forbid();
        }

        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", document.StoredFileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

        _context.ProductDocuments.Remove(document);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Document deleted.";
        return RedirectToAction(nameof(Documents));
    }

    public async Task<IActionResult> Eligibility()
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Warranties)
            .Where(p => p.CustomerId == CurrentCustomerId)
            .ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> ManufacturerSupport(int? brandId)
    {
        var brands = await _context.Brands
            .Where(b => b.WarrantyClaimUrl != string.Empty
                || b.OfficialWebsiteUrl != string.Empty
                || b.SupportPhoneNumber != string.Empty
                || b.SupportEmailAddress != string.Empty)
            .OrderBy(b => b.Name)
            .ToListAsync();
        ViewBag.Brands = brands;

        var selectedBrand = brandId.HasValue
            ? brands.FirstOrDefault(b => b.Id == brandId.Value)
            : null;
        return View(selectedBrand);
    }

    public async Task<IActionResult> ServiceRequests()
    {
        var requests = await _context.ServiceRequests
            .Include(s => s.Product).ThenInclude(p => p.Brand)
            .Include(s => s.Histories)
            .Where(s => s.CustomerId == CurrentCustomerId)
            .OrderByDescending(s => s.RequestDate)
            .ToListAsync();
        return View(requests);
    }

    public async Task<IActionResult> CreateServiceRequest(int? productId = null)
    {
        var products = await _context.Products.Include(p => p.Brand).Where(p => p.CustomerId == CurrentCustomerId).ToListAsync();
        ViewBag.Products = products;
        var selectedProductId = productId.HasValue && products.Any(p => p.Id == productId.Value)
            ? productId.Value
            : products.FirstOrDefault()?.Id ?? 0;
        return View(new ServiceRequestViewModel { ProductId = selectedProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateServiceRequest(ServiceRequestViewModel model)
    {
        var products = await _context.Products.Include(p => p.Brand).Where(p => p.CustomerId == CurrentCustomerId).ToListAsync();
        ViewBag.Products = products;

        if (!products.Any(p => p.Id == model.ProductId))
        {
            ModelState.AddModelError(nameof(model.ProductId), "Select one of your registered products.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ServiceRequest
        {
            ServiceRequestCode = "SR-" + Guid.NewGuid().ToString()[..8].ToUpper(),
            CustomerId = CurrentCustomerId,
            ProductId = model.ProductId,
            IssueDescription = model.IssueDescription,
            AdditionalInformation = model.AdditionalInformation,
            ClaimReferenceNumber = model.ClaimReferenceNumber,
            RequestDate = DateTime.UtcNow,
            Status = "Submitted",
            TrackingInfo = "Submitted and awaiting manufacturer follow-up.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        _context.ServiceHistories.Add(new ServiceHistory
        {
            ServiceRequestId = request.Id,
            Status = "Submitted",
            Notes = "Service request submitted.",
            ChangedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Service request submitted.";
        return RedirectToAction(nameof(ServiceRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClaimReference(int id, string claimReferenceNumber)
    {
        var request = await _context.ServiceRequests.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == CurrentCustomerId);
        if (request == null)
        {
            return Forbid();
        }

        request.ClaimReferenceNumber = claimReferenceNumber;
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Claim reference updated.";
        return RedirectToAction(nameof(ServiceRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWarrantyClaimStatus(int id, string? warrantyClaimed, string? returnUrl = null)
    {
        bool? claimed;
        if (warrantyClaimed == "true")
        {
            claimed = true;
        }
        else if (warrantyClaimed == "false")
        {
            claimed = false;
        }
        else if (string.IsNullOrEmpty(warrantyClaimed))
        {
            claimed = null;
        }
        else
        {
            return BadRequest();
        }

        var request = await _context.ServiceRequests
            .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == CurrentCustomerId);
        if (request == null)
        {
            return NotFound();
        }

        if (request.WarrantyClaimed != claimed)
        {
            request.WarrantyClaimed = claimed;
            request.UpdatedAt = DateTime.UtcNow;
            _context.ServiceHistories.Add(new ServiceHistory
            {
                ServiceRequestId = request.Id,
                Status = request.Status,
                Notes = $"Warranty claim status changed to {(claimed == true ? "Claimed" : claimed == false ? "Not claimed" : "Not specified")}.",
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Warranty claim status updated.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(ServiceRequests));
    }

    public async Task<IActionResult> ServiceHistory()
    {
        var histories = await _context.ServiceRequests
            .Include(s => s.Product).ThenInclude(p => p.Brand)
            .Include(s => s.Histories)
            .Where(s => s.CustomerId == CurrentCustomerId)
            .OrderByDescending(s => s.RequestDate)
            .ToListAsync();

        return View(histories);
    }

    public async Task<IActionResult> Notifications()
    {
        var notifications = await _context.Notifications
            .Where(n => n.CustomerId == CurrentCustomerId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissNotification(int id, string? returnUrl = null)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CustomerId == CurrentCustomerId);
        if (notification == null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Notifications));
    }
}
