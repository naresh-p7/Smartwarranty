using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWarranty_2.Data;
using SmartWarranty_2.Models;
using SmartWarranty_2.Services;
using SmartWarranty_2.ViewModels;

namespace SmartWarranty_2.Controllers;

public class AccountController : Controller
{
    private readonly SmartWarrantyDbContext _context;
    private readonly IAuthService _authService;

    public AccountController(SmartWarrantyDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public IActionResult AccessDenied() => View();

    public IActionResult CustomerLogin() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerLogin(CustomerLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = await _authService.ValidateCustomerAsync(model.UsernameOrEmail, model.Password);
        if (customer == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(model);
        }

        await _authService.SignInAsync(HttpContext, "Customer", customer.Id, customer.UserName);
        return RedirectToAction("Dashboard", "Customer");
    }

    public IActionResult CustomerRegister() => View(new CustomerRegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerRegister(CustomerRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _context.Customers.AnyAsync(c => c.UserName == model.UserName))
        {
            ModelState.AddModelError(nameof(model.UserName), "Username is already in use.");
            return View(model);
        }

        if (await _context.Customers.AnyAsync(c => c.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        var customer = new Customer
        {
            CustomerCode = "CUST-" + Guid.NewGuid().ToString()[..8].ToUpper(),
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            UserName = model.UserName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Customer registration successful. Please log in.";
        return RedirectToAction(nameof(CustomerLogin));
    }

    public IActionResult AdminLogin() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var admin = await _authService.ValidateAdminAsync(model.UserName, model.Password);
        if (admin == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
            return View(model);
        }

        await _authService.SignInAsync(HttpContext, "Admin", admin.Id, admin.UserName);
        return RedirectToAction("Dashboard", "Admin");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
