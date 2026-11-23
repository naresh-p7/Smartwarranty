using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;

namespace SmartWarranty_2.Services;

public class DashboardService
{
    private readonly SmartWarrantyDbContext _context;

    public DashboardService(SmartWarrantyDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, int>> GetAdminDashboardMetricsAsync()
    {
        var totalCustomers = await _context.Customers.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        var totalWarranties = await _context.Warranties.CountAsync();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var activeWarranties = await _context.Warranties.CountAsync(w => w.ExpiryDate >= today && w.StartDate < tomorrow);
        var expiredWarranties = await _context.Warranties.CountAsync(w => w.ExpiryDate < today);

        var expiringSoonThresholdDays = 30;
        var expiringSoonWarranties = await _context.Warranties.CountAsync(w => w.ExpiryDate >= today
            && w.ExpiryDate < today.AddDays(expiringSoonThresholdDays + 1));

        var totalServiceRequests = await _context.ServiceRequests.CountAsync();
        var pendingRequests = await _context.ServiceRequests.CountAsync(s => s.Status != "Resolved" && s.Status != "Closed");
        var resolvedRequests = await _context.ServiceRequests.CountAsync(s => s.Status == "Resolved" || s.Status == "Closed");

        return new Dictionary<string, int>
        {
            ["TotalCustomers"] = totalCustomers,
            ["TotalProducts"] = totalProducts,
            ["TotalWarranties"] = totalWarranties,
            ["ActiveWarranties"] = activeWarranties,
            ["ExpiredWarranties"] = expiredWarranties,
            ["ExpiringSoonWarranties"] = expiringSoonWarranties,
            ["TotalServiceRequests"] = totalServiceRequests,
            ["PendingRequests"] = pendingRequests,
            ["ResolvedRequests"] = resolvedRequests
        };
    }

    public async Task<Dictionary<string, int>> GetCustomerDashboardMetricsAsync(int customerId)
    {
        var products = await _context.Products.Where(p => p.CustomerId == customerId).ToListAsync();
        var productIds = products.Select(p => p.Id).ToList();

        var warranties = await _context.Warranties
            .Where(w => productIds.Contains(w.ProductId))
            .ToListAsync();

        var serviceRequests = await _context.ServiceRequests
            .Where(s => s.CustomerId == customerId)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var active = warranties.Count(w => w.ExpiryDate.Date >= today && w.StartDate.Date <= today);
        var expired = warranties.Count(w => w.ExpiryDate.Date < today);
        var expiringSoon = warranties.Count(w => w.ExpiryDate.Date >= today && w.ExpiryDate.Date <= today.AddDays(30));

        return new Dictionary<string, int>
        {
            ["Products"] = products.Count,
            ["ActiveWarranties"] = active,
            ["ExpiredWarranties"] = expired,
            ["ExpiringSoonWarranties"] = expiringSoon,
            ["TotalServiceRequests"] = serviceRequests.Count,
            ["ResolvedServiceRequests"] = serviceRequests.Count(s => s.Status == "Resolved" || s.Status == "Closed")
        };
    }
}
