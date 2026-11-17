using System.ComponentModel.DataAnnotations;

namespace SmartWarranty_2.Models;

public class Administrator
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
