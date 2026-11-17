using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Models;

namespace SmartWarranty_2.Data;

public class SmartWarrantyDbContext : DbContext
{
    public SmartWarrantyDbContext(DbContextOptions<SmartWarrantyDbContext> options) : base(options)
    {
    }

    public DbSet<Administrator> Administrators => Set<Administrator>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<ProductDocument> ProductDocuments => Set<ProductDocument>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceHistory> ServiceHistories => Set<ServiceHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Administrator>()
            .HasIndex(a => a.UserName)
            .IsUnique();

        modelBuilder.Entity<Administrator>()
            .HasIndex(a => a.Email)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.UserName)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Brand>()
            .HasIndex(b => b.Name)
            .IsUnique();

        modelBuilder.Entity<ProductCategory>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductCode)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SerialNumber)
            .IsUnique();

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(s => s.ServiceRequestCode)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Warranty>()
            .HasOne(w => w.Product)
            .WithMany(p => p.Warranties)
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductDocument>()
            .HasOne(d => d.Product)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.ServiceRequests)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(s => s.Product)
            .WithMany(p => p.ServiceRequests)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ServiceHistory>()
            .HasOne(h => h.ServiceRequest)
            .WithMany(s => s.Histories)
            .HasForeignKey(h => h.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Customer)
            .WithMany(c => c.Notifications)
            .HasForeignKey(n => n.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
