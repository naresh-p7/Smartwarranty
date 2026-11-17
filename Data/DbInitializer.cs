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

        if (!db.ProductCategories.Any())
        {
            db.ProductCategories.AddRange(
                new ProductCategory { Name = "Electronics", Description = "Electronics products" },
                new ProductCategory { Name = "Home Appliances", Description = "Home appliances" },
                new ProductCategory { Name = "Computers", Description = "Computers and accessories" },
                new ProductCategory { Name = "Mobile Devices", Description = "Phones and tablets" }
            );
        }

        db.SaveChanges();
    }
}
