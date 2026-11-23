using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Models;

namespace SmartWarranty_2.Data;

public static class DbInitializer
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartWarrantyDbContext>();
        db.Database.Migrate();

        if (!db.Administrators.Any())
        {
            db.Administrators.Add(new Administrator
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@smartwarranty.local",
                UserName = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();
    }
}
