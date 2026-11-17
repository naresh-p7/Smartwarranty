using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartWarranty_2.Models;

public class ServiceHistory
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(ServiceRequest))]
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    [Required, StringLength(40)]
    public string Status { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
