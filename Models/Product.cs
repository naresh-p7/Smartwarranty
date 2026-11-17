using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartWarranty_2.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ModelNumber { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    public DateTime PurchaseDate { get; set; }

    [Required, StringLength(120)]
    public string SellerName { get; set; } = string.Empty;

    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(Brand))]
    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
    public ICollection<ProductDocument> Documents { get; set; } = new List<ProductDocument>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
