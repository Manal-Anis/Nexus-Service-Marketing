using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>Browse the plan catalogue and place an order.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class BrowseController : BaseAreaController
{
    private readonly CatalogService _catalog;
    private readonly OrderService _orders;
    private readonly SettingsService _settings;

    public BrowseController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CatalogService catalog, OrderService orders, SettingsService settings)
        : base(db, currentUser, audit)
    {
        _catalog = catalog;
        _orders = orders;
        _settings = settings;
    }

    /// <summary>Shows the plans the customer can order.</summary>
    public async Task<IActionResult> Index(string? category = null, string? search = null)
    {
        var categories = await _catalog.GetActiveCategoriesAsync();
        var plans = await _catalog.GetActivePlansAsync(category, search);

        ViewData["Title"] = "Browse the plans";
        ViewData["Subtitle"] = "Pick a plan and order it in three steps. You pay the installer in cash.";

        return View(new PlansViewModel
        {
            Categories = categories,
            Plans = plans,
            Search = search,
            CategoryCode = category,
            BillingPeriods = Enum.GetValues<BillingPeriod>().ToList()
        });
    }

    /// <summary>Opens the three step order wizard for the selected plan.</summary>
    [HttpGet]
    public async Task<IActionResult> PlaceOrder(int planId)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            TempData["Error"] = "The customer profile is missing. Please sign in again.";
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var customer = await Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId);
        var plan = await _catalog.GetPlanAsync(planId);

        if (plan is null)
        {
            TempData["Error"] = "This plan is not available any more.";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildWizardAsync(new OrderWizardViewModel
        {
            PlanId = plan.Id,
            CityId = customer.CityId,
            InstallationAddress = customer.Address ?? string.Empty,
            ContactNumber = customer.Phone ?? string.Empty
        });

        ViewData["Title"] = "Place the order";
        ViewData["Subtitle"] = plan.Name;
        return View(model);
    }

    /// <summary>Recalculates the order summary when the customer changes the quantity.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(OrderWizardViewModel model)
    {
        if (model.PlanId <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var rebuilt = await BuildWizardAsync(model);
        ViewData["Title"] = "Place the order";
        ViewData["Subtitle"] = rebuilt.Plan?.Name ?? "Order";
        return View("PlaceOrder", rebuilt);
    }

    /// <summary>Places the order and issues the order id.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(OrderWizardViewModel model)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var rebuilt = await BuildWizardAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Place the order";
            ViewData["Subtitle"] = rebuilt.Plan?.Name ?? "Order";
            return View(rebuilt);
        }

        var customer = await Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId);
        var vendor = await Db.Vendors.AsNoTracking()
            .Where(v => v.Status == VendorStatus.Approved && v.CityId == (model.CityId ?? customer.CityId))
            .OrderBy(v => v.Id)
            .FirstOrDefaultAsync();

        var result = await _orders.PlaceOrderAsync(new PlaceOrderRequest
        {
            CustomerId = customerId,
            VendorId = vendor?.Id,
            PlanId = model.PlanId,
            Quantity = model.Quantity,
            CityId = model.CityId ?? customer.CityId,
            InstallationAddress = model.InstallationAddress,
            ContactNumber = model.ContactNumber,
            ExistingLandlineAccountId = string.IsNullOrWhiteSpace(model.ExistingLandlineAccountId)
                ? null
                : model.ExistingLandlineAccountId.Trim().ToUpperInvariant(),
            EquipmentProductId = model.EquipmentProductId,
            PlacedByUserId = profile.UserId,
            Notes = model.Notes
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The order could not be placed.");
            rebuilt.PlacedOrderId = null;
            ViewData["Title"] = "Place the order";
            ViewData["Subtitle"] = rebuilt.Plan?.Name ?? "Order";
            return View(rebuilt);
        }

        await Audit.LogAsync("Order.Placed", "Order", result.OrderId, profile.UserId, profile.Email, IpAddress,
            $"Order of {model.Quantity} connection(s) placed online.");

        TempData["Success"] = $"Your order {result.OrderId} has been placed. The partner will run the feasibility survey.";
        return RedirectToAction("Details", "Orders", new { area = "Customer", id = result.OrderId });
    }

    // ------------------------------------------------------------------ helpers
    private async Task<OrderWizardViewModel> BuildWizardAsync(OrderWizardViewModel model)
    {
        var rules = await Db.DiscountRules.AsNoTracking().ToListAsync();
        var discount = new DiscountService();

        model.Plans = await _catalog.GetActivePlansAsync();
        model.Cities = await _catalog.GetActiveCitiesAsync();
        model.Equipment = await Db.Products.AsNoTracking()
            .Where(p => p.StockQuantity > 0)
            .OrderBy(p => p.Name)
            .ToListAsync();
        model.DiscountRules = rules;

        model.Plan = model.Plans.FirstOrDefault(p => p.Id == model.PlanId);

        if (model.Plan is not null)
        {
            var category = await Db.ServiceCategories.AsNoTracking().FirstAsync(c => c.Id == model.Plan.CategoryId);
            model.SecurityDeposit = category.SecurityDeposit;
            model.GrossDeposit = category.SecurityDeposit * model.Quantity;
            model.GrossAdvance = model.Plan.Price * model.Quantity;

            var breakdown = discount.Calculate(model.GrossDeposit, model.GrossAdvance, model.Quantity, rules);
            model.DiscountPercent = breakdown.DiscountPercent;
            model.NetDeposit = breakdown.NetSecurityDeposit;
            model.NetAdvance = breakdown.NetAdvance;

            var equipment = model.Equipment.FirstOrDefault(p => p.Id == model.EquipmentProductId);
            model.EquipmentAmount = equipment is null ? 0m : equipment.Price * model.Quantity;
        }

        model.TaxPercent = await _settings.GetTaxPercentageAsync();
        var taxable = model.NetDeposit + model.NetAdvance + model.EquipmentAmount;
        model.TotalAmount = Math.Round(taxable + (taxable * model.TaxPercent / 100m), 2);

        // The plan of the wizard is replaced by the fresh catalogue entry.
        model.Plan = model.Plans.FirstOrDefault(p => p.Id == model.PlanId);

        return model;
    }
}
