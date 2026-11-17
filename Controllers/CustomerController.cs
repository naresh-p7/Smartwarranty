using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;
using SmartWarranty_2.Services;
using SmartWarranty_2.ViewModels;

namespace SmartWarranty_2.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly SmartWarrantyDbContext _context;
    private readonly DashboardService _dashboardService;

    public CustomerController(SmartWarrantyDbContext context, DashboardService dashboardService)
    {
        _context = context;
        _dashboardService = dashboardService;
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var warranty = new Warranty
        {
            ProductId = model.ProductId,
            StartDate = model.StartDate,
            DurationMonths = model.DurationMonths,
            ExpiryDate = model.ExpiryDate,
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

        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType) && !new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
        {
            TempData["ErrorMessage"] = "Unsupported file type.";
            return RedirectToAction(nameof(Documents));
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File size must be less than 5 MB.";
            return RedirectToAction(nameof(Documents));
        }

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var storedName = Guid.NewGuid() + Path.GetExtension(file.FileName);
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
        TempData["SuccessMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Documents));
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

    public async Task<IActionResult> ManufacturerSupport()
    {
        var brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        return View(brands);
    }

    public async Task<IActionResult> ServiceRequests()
    {
        var requests = await _context.ServiceRequests
            .Include(s => s.Product).ThenInclude(p => p.Brand)
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

    public async Task<IActionResult> ServiceHistory()
    {
        var histories = await _context.ServiceRequests
            .Include(s => s.Product).ThenInclude(p => p.Brand)
            .Where(s => s.CustomerId == CurrentCustomerId)
            .OrderByDescending(s => s.RequestDate)
            .ToListAsync();

        return View(histories);
    }

    public async Task<IActionResult> Notifications()
    {
        var notifications = await _context.Notifications.Where(n => n.CustomerId == CurrentCustomerId).OrderByDescending(n => n.CreatedAt).ToListAsync();
        return View(notifications);
    }
}
