using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The profile of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ProfileController : BaseAreaController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CatalogService _catalog;

    public ProfileController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        UserManager<ApplicationUser> userManager, CatalogService catalog)
        : base(db, currentUser, audit)
    {
        _userManager = userManager;
        _catalog = catalog;
    }

    /// <summary>Shows the profile form.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var customer = await Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .FirstAsync(c => c.Id == customerId);

        var model = new CustomerProfileViewModel
        {
            FullName = customer.FullName,
            Phone = customer.Phone ?? string.Empty,
            Address = customer.Address ?? string.Empty,
            Email = customer.Email,
            CityId = customer.CityId,
            Cities = await _catalog.GetActiveCitiesAsync()
        };

        ViewData["Title"] = "My profile";
        ViewData["Subtitle"] = "Keep your contact details up to date so the partner can reach you.";

        return View(model);
    }

    /// <summary>Saves the profile form.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CustomerProfileViewModel model)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        model.Cities = await _catalog.GetActiveCitiesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = await Db.Customers.FirstAsync(c => c.Id == customerId);
        customer.FullName = model.FullName.Trim();
        customer.Phone = model.Phone.Trim();
        customer.Address = model.Address.Trim();
        customer.CityId = model.CityId;
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            customer.Email = model.Email.Trim();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            user.FullName = customer.FullName;
            user.Phone = customer.Phone;
            user.Address = customer.Address;
            user.CityId = customer.CityId;

            if (!string.IsNullOrWhiteSpace(model.Email) && !string.Equals(user.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(user, model.Email.Trim());
                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                await _userManager.SetUserNameAsync(user, model.Email.Trim());
            }

            await _userManager.UpdateAsync(user);
        }

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Customer.ProfileUpdated", "Customer", customerId.ToString(), profile.UserId, profile.Email, IpAddress,
            "The customer updated the profile.");

        TempData["Success"] = "Your profile has been saved.";
        return RedirectToAction(nameof(Index));
    }
}
