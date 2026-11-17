using System.ComponentModel.DataAnnotations;

namespace SmartWarranty_2.Models;

public class Brand
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Url]
    public string OfficialWebsiteUrl { get; set; } = string.Empty;

    [Url]
    public string WarrantyClaimUrl { get; set; } = string.Empty;

    [Phone]
    public string SupportPhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string SupportEmailAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
