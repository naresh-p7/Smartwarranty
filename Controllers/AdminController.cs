using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;
using SmartWarranty_2.Services;

namespace SmartWarranty_2.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly SmartWarrantyDbContext _context;
    private readonly DashboardService _dashboardService;

    public AdminController(SmartWarrantyDbContext context, DashboardService dashboardService)
    {
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var metrics = await _dashboardService.GetAdminDashboardMetricsAsync();
        var customers = await _context.Customers.OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync();
        var recentServiceRequests = await _context.ServiceRequests.Include(s => s.Customer).Include(s => s.Product).OrderByDescending(s => s.RequestDate).Take(5).ToListAsync();

        ViewBag.Metrics = metrics;
        ViewBag.Customers = customers;
        ViewBag.RecentRequests = recentServiceRequests;
        return View();
    }

    public async Task<IActionResult> Customers()
    {
        var customers = await _context.Customers.OrderBy(c => c.FirstName).ToListAsync();
        return View(customers);
    }

    public async Task<IActionResult> Brands()
    {
        var brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        return View(brands);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand(Brand brand)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter valid brand and support details.";
            return RedirectToAction(nameof(Brands));
        }

        var exists = await _context.Brands.AnyAsync(b => b.Name == brand.Name);
        if (exists)
        {
            TempData["ErrorMessage"] = "Brand already exists.";
            return RedirectToAction(nameof(Brands));
        }

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Brand added successfully.";
        return RedirectToAction(nameof(Brands));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBrand([Bind("Id,Name,OfficialWebsiteUrl,WarrantyClaimUrl,SupportPhoneNumber,SupportEmailAddress,Description")] Brand brand)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter valid brand and support details.";
            return RedirectToAction(nameof(Brands));
        }

        var existingBrand = await _context.Brands.FindAsync(brand.Id);
        if (existingBrand == null)
        {
            return NotFound();
        }

        var duplicateName = await _context.Brands.AnyAsync(b => b.Id != brand.Id && b.Name == brand.Name);
        if (duplicateName)
        {
            TempData["ErrorMessage"] = "Another brand already uses that name.";
            return RedirectToAction(nameof(Brands));
        }

        existingBrand.Name = brand.Name;
        existingBrand.OfficialWebsiteUrl = brand.OfficialWebsiteUrl;
        existingBrand.WarrantyClaimUrl = brand.WarrantyClaimUrl;
        existingBrand.SupportPhoneNumber = brand.SupportPhoneNumber;
        existingBrand.SupportEmailAddress = brand.SupportEmailAddress;
        existingBrand.Description = brand.Description;
        existingBrand.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Brand details updated successfully.";
        return RedirectToAction(nameof(Brands));
    }

    public async Task<IActionResult> Categories()
    {
        var categories = await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync();
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(ProductCategory category)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Categories));
        }

        var exists = await _context.ProductCategories.AnyAsync(c => c.Name == category.Name);
        if (exists)
        {
            TempData["ErrorMessage"] = "Category already exists.";
            return RedirectToAction(nameof(Categories));
        }

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Category added successfully.";
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Support()
    {
        var brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        return View(brands);
    }

    public async Task<IActionResult> Products()
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Customer)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> Warranties()
    {
        var warranties = await _context.Warranties
            .Include(w => w.Product)
            .ThenInclude(p => p.Customer)
            .OrderByDescending(w => w.ExpiryDate)
            .ToListAsync();
        return View(warranties);
    }

    public async Task<IActionResult> ServiceRequests()
    {
        var requests = await _context.ServiceRequests
            .Include(s => s.Customer)
            .Include(s => s.Product)
            .ThenInclude(p => p.Brand)
            .OrderByDescending(s => s.RequestDate)
            .ToListAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateServiceStatus(int id, string status, string trackingInfo)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        request.Status = status;
        request.TrackingInfo = trackingInfo;
        request.UpdatedAt = DateTime.UtcNow;

        _context.ServiceHistories.Add(new ServiceHistory
        {
            ServiceRequestId = request.Id,
            Status = status,
            Notes = trackingInfo,
            ChangedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Service request updated.";
        return RedirectToAction(nameof(ServiceRequests));
    }

    public async Task<IActionResult> Reports()
    {
        var customers = await _context.Customers.CountAsync();
        var products = await _context.Products.CountAsync();
        var today = DateTime.UtcNow.Date;
        var active = await _context.Warranties.CountAsync(w => w.StartDate < today.AddDays(1) && w.ExpiryDate >= today);
        var expired = await _context.Warranties.CountAsync(w => w.ExpiryDate < today);
        var expiringSoon = await _context.Warranties.CountAsync(w => w.ExpiryDate >= today && w.ExpiryDate < today.AddDays(31));
        var serviceRequests = await _context.ServiceRequests.CountAsync();
        var resolved = await _context.ServiceRequests.CountAsync(s => s.Status == "Resolved" || s.Status == "Closed");

        ViewBag.Report = new
        {
            Customers = customers,
            Products = products,
            Active = active,
            Expired = expired,
            ExpiringSoon = expiringSoon,
            ServiceRequests = serviceRequests,
            Resolved = resolved
        };

        return View();
    }
}
