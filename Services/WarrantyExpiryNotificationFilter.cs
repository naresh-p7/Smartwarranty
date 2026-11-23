using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;

namespace SmartWarranty_2.Services;

public sealed class WarrantyExpiryNotificationFilter : IAsyncActionFilter
{
    private readonly SmartWarrantyDbContext _context;
    private readonly ILogger<WarrantyExpiryNotificationFilter> _logger;

    public WarrantyExpiryNotificationFilter(
        SmartWarrantyDbContext context,
        ILogger<WarrantyExpiryNotificationFilter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var customerIdValue = context.HttpContext.User.FindFirstValue("UserId");
        if (int.TryParse(customerIdValue, out var customerId))
        {
            var today = DateTime.UtcNow.Date;
            var lastReminderDate = today.AddDays(WarrantyStatusService.ExpiringSoonThresholdDays);
            var expiringWarranties = await _context.Warranties
                .Where(w => w.Product.CustomerId == customerId
                    && w.StartDate <= today
                    && w.ExpiryDate >= today
                    && w.ExpiryDate < lastReminderDate.AddDays(1))
                .Select(w => new { w.Id, ProductName = w.Product.Name, w.ExpiryDate })
                .ToListAsync();

            var warrantyIds = expiringWarranties.Select(w => w.Id).ToArray();
            var existingReminderIds = new HashSet<int>();
            if (warrantyIds.Length > 0)
            {
                var existingReminderIdList = await _context.Notifications
                    .Where(n => n.CustomerId == customerId && n.WarrantyId.HasValue && warrantyIds.Contains(n.WarrantyId.Value))
                    .Select(n => n.WarrantyId!.Value)
                    .ToListAsync();
                existingReminderIds = existingReminderIdList.ToHashSet();
            }

            foreach (var warranty in expiringWarranties.Where(w => !existingReminderIds.Contains(w.Id)))
            {
                _context.Notifications.Add(new Notification
                {
                    CustomerId = customerId,
                    WarrantyId = warranty.Id,
                    Title = "Warranty expiring soon",
                    Message = $"{warranty.ProductName} warranty expires on {warranty.ExpiryDate:d}.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (_context.ChangeTracker.HasChanges())
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
                {
                    foreach (var entry in _context.ChangeTracker.Entries<Notification>()
                                 .Where(entry => entry.State == EntityState.Added))
                    {
                        entry.State = EntityState.Detached;
                    }

                    _logger.LogDebug(exception, "A warranty reminder was created by a concurrent request.");
                }
            }

            var activeNotifications = await _context.Notifications
                .Where(n => n.CustomerId == customerId
                    && !n.IsRead
                    && (n.WarrantyId == null
                        || (n.Warranty != null
                            && n.Warranty.ExpiryDate >= today
                            && n.Warranty.ExpiryDate < lastReminderDate.AddDays(1))))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (context.Controller is Controller controller)
            {
                controller.ViewData["ActiveNotifications"] = activeNotifications;
            }
        }

        await next();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && (sqlException.Number == 2601 || sqlException.Number == 2627);
}
