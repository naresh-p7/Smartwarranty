using System.ComponentModel.DataAnnotations;

namespace SmartWarranty_2.ViewModels;

public class ProductRegistrationViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [Display(Name = "Brand")]
    public int? BrandId { get; set; }

    [StringLength(120)]
    [Display(Name = "New Brand / Manufacturer")]
    public string? BrandName { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Model Number")]
    public string ModelNumber { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Serial Number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(120)]
    [Display(Name = "Seller Name")]
    public string SellerName { get; set; } = string.Empty;
}

public class WarrantyRegistrationViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Warranty Start Date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Range(1, 240)]
    [Display(Name = "Warranty Duration (Months)")]
    public int DurationMonths { get; set; } = 12;

    [Required]
    [Display(Name = "Warranty Type")]
    public string WarrantyType { get; set; } = "Standard";
}

public class InvoiceReviewViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Model Number")]
    public string ModelNumber { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Serial Number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Purchase Date")]
    public DateTime PurchaseDate { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Seller Name")]
    public string SellerName { get; set; } = string.Empty;

    [Range(1, 240)]
    [Display(Name = "Warranty Duration (Months, optional)")]
    public int? WarrantyDurationMonths { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Warranty Start Date")]
    public DateTime? WarrantyStartDate { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Warranty Type")]
    public string WarrantyType { get; set; } = "Standard";

    public bool ExtractedTextFound { get; set; }
}

public class ServiceRequestViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Display(Name = "Issue Description")]
    public string IssueDescription { get; set; } = string.Empty;

    [Display(Name = "Relevant Additional Information")]
    public string AdditionalInformation { get; set; } = string.Empty;

    [Display(Name = "Claim Reference Number")]
    public string ClaimReferenceNumber { get; set; } = string.Empty;
}
