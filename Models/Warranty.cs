using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartWarranty_2.Models;

public class Warranty
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Range(1, 240)]
    public int DurationMonths { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required, StringLength(80)]
    public string WarrantyType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
