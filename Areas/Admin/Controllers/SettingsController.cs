using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The settings of the application. The administrator owns the service tax, the currency, the
/// overdue rule, the security deposits and the bulk discount tiers.
/// </summary>
public class SettingsController : AdminControllerBase
{
    private readonly SettingsService _settings;

    public SettingsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        SettingsService settings)
        : base(db, currentUser, audit)
    {
        _settings = settings;
    }

    /// <summary>Shows the general settings, the deposits and the discount tiers.</summary>
    public async Task<IActionResult> Index()
    {
        var model = new SettingsFormViewModel
        {
            TaxPercent = await _settings.GetTaxPercentageAsync(),
            Currency = await _settings.GetCurrencyAsync(),
            OverdueDays = await _settings.GetOverdueDaysAsync(),
            PaymentGraceDays = ParseInt(await _settings.GetAsync(SettingsService.PaymentGraceDaysKey, "0")),
            SupportPhone = await _settings.GetAsync(SettingsService.SupportPhoneKey),
            SupportEmail = await _settings.GetAsync(SettingsService.SupportEmailKey),
            CompanyAddress = await _settings.GetAsync(SettingsService.CompanyAddressKey)
        };

        ViewData["Title"] = "Settings";
        ViewData["Subtitle"] = "The rules of the network - tax, currency, deposits and discounts.";
        ViewData["Categories"] = await Db.ServiceCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        ViewData["DiscountRules"] = await Db.DiscountRules.AsNoTracking()
            .OrderBy(r => r.MinConnections)
            .ToListAsync();

        return View(model);
    }

    /// <summary>Saves the general settings of the application.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SettingsFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Settings";
            ViewData["Categories"] = await Db.ServiceCategories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync();
            ViewData["DiscountRules"] = await Db.DiscountRules.AsNoTracking().OrderBy(r => r.MinConnections).ToListAsync();
            return View("Index", model);
        }

        await _settings.SetAsync(SettingsService.TaxPercentageKey, model.TaxPercent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settings.SetAsync(SettingsService.CurrencyKey, model.Currency.Trim());
        await _settings.SetAsync(SettingsService.OverdueDaysKey, model.OverdueDays.ToString());
        await _settings.SetAsync(SettingsService.PaymentGraceDaysKey, model.PaymentGraceDays.ToString());
        await _settings.SetAsync(SettingsService.SupportPhoneKey, model.SupportPhone?.Trim() ?? string.Empty);
        await _settings.SetAsync(SettingsService.SupportEmailKey, model.SupportEmail?.Trim() ?? string.Empty);
        await _settings.SetAsync(SettingsService.CompanyAddressKey, model.CompanyAddress?.Trim() ?? string.Empty);

        await Audit.LogAsync("Settings.Updated", "Setting", "General", UserId, UserName, IpAddress,
            $"The general settings were saved - tax {model.TaxPercent}%, currency {model.Currency}, overdue after {model.OverdueDays} days.");

        TempData["Success"] = "The settings have been saved.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Saves the security deposit of one service category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeposit(SecurityDepositFormViewModel model)
    {
        var category = await Db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == model.CategoryId);

        if (category is null)
        {
            return Missing("This service category does not exist.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a valid security deposit.";
            return RedirectToAction(nameof(Index));
        }

        var before = category.SecurityDeposit;
        category.SecurityDeposit = model.SecurityDeposit;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("SecurityDeposit.Updated", "ServiceCategory", category.Name, UserId, UserName, IpAddress,
            $"The security deposit of {category.Name} was moved from {before:N2} to {category.SecurityDeposit:N2} Rs.");

        TempData["Success"] = $"The security deposit of {category.Name} is now {category.SecurityDeposit:N2} Rs.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Registers a new bulk discount tier.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDiscountRule(DiscountRuleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the values of the discount tier.";
            return RedirectToAction(nameof(Index));
        }

        if (model.MaxConnections.HasValue && model.MaxConnections.Value < model.MinConnections)
        {
            TempData["Error"] = "The upper limit of the tier cannot be smaller than the lower limit.";
            return RedirectToAction(nameof(Index));
        }

        // The tiers must not overlap, otherwise the calculation of the discount would be ambiguous.
        var overlapping = await Db.DiscountRules
            .Where(r => r.IsActive)
            .Where(r => r.MinConnections <= (model.MaxConnections ?? int.MaxValue) &&
                        (r.MaxConnections == null || r.MaxConnections.Value >= model.MinConnections))
            .ToListAsync();

        if (overlapping.Count > 0)
        {
            TempData["Error"] = "This range overlaps an existing discount tier.";
            return RedirectToAction(nameof(Index));
        }

        var rule = new DiscountRule
        {
            MinConnections = model.MinConnections,
            MaxConnections = model.MaxConnections,
            DiscountPercent = model.DiscountPercent,
            Description = model.Description?.Trim(),
            IsActive = model.IsActive
        };

        Db.DiscountRules.Add(rule);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("DiscountRule.Created", "DiscountRule", rule.Id.ToString(), UserId, UserName, IpAddress,
            $"The discount tier {model.MinConnections}-{(model.MaxConnections?.ToString() ?? "and more")} at {model.DiscountPercent}% was added.");

        TempData["Success"] = "The discount tier has been added.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Edits an existing bulk discount tier.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDiscountRule(DiscountRuleFormViewModel model)
    {
        var rule = await Db.DiscountRules.FirstOrDefaultAsync(r => r.Id == model.Id);

        if (rule is null)
        {
            return Missing("This discount tier does not exist.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the values of the discount tier.";
            return RedirectToAction(nameof(Index));
        }

        rule.MinConnections = model.MinConnections;
        rule.MaxConnections = model.MaxConnections;
        rule.DiscountPercent = model.DiscountPercent;
        rule.Description = model.Description?.Trim();
        rule.IsActive = model.IsActive;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("DiscountRule.Updated", "DiscountRule", rule.Id.ToString(), UserId, UserName, IpAddress,
            $"The discount tier was changed to {rule.MinConnections}-{(rule.MaxConnections?.ToString() ?? "and more")} at {rule.DiscountPercent}%.");

        TempData["Success"] = "The discount tier has been updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Switches a discount tier on or off.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDiscountRule(int id)
    {
        var rule = await Db.DiscountRules.FirstOrDefaultAsync(r => r.Id == id);

        if (rule is null)
        {
            return Missing("This discount tier does not exist.");
        }

        rule.IsActive = !rule.IsActive;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("DiscountRule.Toggled", "DiscountRule", rule.Id.ToString(), UserId, UserName, IpAddress,
            $"The discount tier {rule.MinConnections} is now {(rule.IsActive ? "active" : "inactive")}.");

        TempData["Success"] = $"The discount tier is now {(rule.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes a discount tier that was entered by mistake.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDiscountRule(int id)
    {
        var rule = await Db.DiscountRules.FirstOrDefaultAsync(r => r.Id == id);

        if (rule is null)
        {
            return Missing("This discount tier does not exist.");
        }

        Db.DiscountRules.Remove(rule);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("DiscountRule.Deleted", "DiscountRule", id.ToString(), UserId, UserName, IpAddress,
            $"The discount tier {rule.MinConnections} was deleted.");

        TempData["Success"] = "The discount tier has been deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Reads an integer from a setting value.</summary>
    private static int ParseInt(string value) => int.TryParse(value, out var number) ? number : 0;
}
