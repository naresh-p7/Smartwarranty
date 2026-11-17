using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;

namespace SmartWarranty_2.Services;

public interface IAuthService
{
    Task<Customer?> ValidateCustomerAsync(string usernameOrEmail, string password);
    Task<Administrator?> ValidateAdminAsync(string username, string password);
    Task SignInAsync(HttpContext context, string role, int userId, string userName);
}

public class AuthService : IAuthService
{
    private readonly SmartWarrantyDbContext _context;

    public AuthService(SmartWarrantyDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> ValidateCustomerAsync(string usernameOrEmail, string password)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserName == usernameOrEmail || c.Email == usernameOrEmail);

        if (customer == null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash) ? customer : null;
    }

    public async Task<Administrator?> ValidateAdminAsync(string username, string password)
    {
        var admin = await _context.Administrators
            .FirstOrDefaultAsync(a => a.UserName == username);

        if (admin == null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash) ? admin : null;
    }

    public async Task SignInAsync(HttpContext context, string role, int userId, string userName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", userId.ToString()),
            new Claim("UserType", role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }
}
