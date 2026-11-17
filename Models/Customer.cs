using System.ComponentModel.DataAnnotations;

namespace SmartWarranty_2.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
