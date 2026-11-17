# SmartWarranty

SmartWarranty is a database-driven ASP.NET Core MVC application for digital warranty and product lifecycle management.

## Features implemented

- Admin login and dashboard
- Customer registration and login
- Product registration and customer-specific access
- Warranty tracking with active / expiring soon / expired status
- Document upload, download, and deletion for customer-owned products
- Manufacturer support links
- Service request submission and status tracking
- SQL Server-backed persistence through Entity Framework Core
- Seeded default administrator account

## Default admin credentials

- Username: admin
- Password: Admin@123

## Run locally

1. Ensure SQL Server LocalDB or SQL Server is available.
2. Restore packages:
   `dotnet restore`
3. Apply migrations:
   `dotnet ef database update`
4. Start the app:
   `dotnet run`
5. Open: `https://localhost:5001` or the actual port shown in the terminal.

## Database configuration

The default connection string is in `appsettings.json` and points to LocalDB:

`Server=(localdb)\MSSQLLocalDB;Database=SmartWarrantyDb;Trusted_Connection=True;MultipleActiveResultSets=true`

## Project structure

- `Controllers/` - MVC controllers for public, admin, customer, and account flows
- `Data/` - EF Core DbContext and database seeding
- `Models/` - Core entities: Customer, Product, Warranty, Brand, etc.
- `Services/` - Authentication, dashboard, and warranty helpers
- `ViewModels/` - form models for login, registration, product, and warranty operations
- `Views/` - Razor pages for admin and customer interfaces
- `wwwroot/` - static assets and uploads

## ER summary

Customer 1:N Product
Brand 1:N Product
ProductCategory 1:N Product
Product 1:N Warranty
Product 1:N ProductDocument
Product 1:N ServiceRequest
Customer 1:N ServiceRequest
ServiceRequest 1:N ServiceHistory
Customer 1:N Notification

## Notes

This project implements the core SmartWarranty business flow and database-backed workflow for a real-world demo. The final warranty approval remains under the control of the manufacturer, as required by the specification.
