using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartWarranty_2.Models;

public class ProductDocument
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, StringLength(80)]
    public string DocumentType { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
