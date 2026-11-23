using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartWarranty_2.Models;

public class ServiceRequest
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string ServiceRequestCode { get; set; } = string.Empty;

    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string AdditionalInformation { get; set; } = string.Empty;

    [StringLength(100)]
    public string ClaimReferenceNumber { get; set; } = string.Empty;

    public bool? WarrantyClaimed { get; set; }

    [Required, StringLength(40)]
    public string Status { get; set; } = "Submitted";

    [StringLength(1000)]
    public string TrackingInfo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ServiceHistory> Histories { get; set; } = new List<ServiceHistory>();
}
