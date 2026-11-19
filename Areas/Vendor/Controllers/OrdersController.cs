using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>Orders of the service partner: the online orders and the walk-in counter orders.</summary>
public class OrdersController : VendorControllerBase
{
    private readonly OrderService _orders;
    private readonly CatalogService _catalog;
    private readonly SettingsService _settings;

    public OrdersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        OrderService orders, CatalogService catalog, SettingsService settings)
        : base(db, currentUser, audit)
    {
        _orders = orders;
        _catalog = catalog;
        _settings = settings;
    }

    /// <summary>Lists the orders of the partner.</summary>
    public async Task<IActionResult> Index(string? status = null, string? search = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var query = Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Include(o => o.RetailShop)
            .Where(o => o.VendorId == vendorId.Value && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.OrderId.Contains(term) ||
                                     (o.Customer != null && o.Customer.FullName.Contains(term)) ||
                                     (o.ContactNumber != null && o.ContactNumber.Contains(term)));
        }

        var orders = await query.OrderByDescending(o => o.PlacedAt).ToListAsync();

        ViewData["Title"] = "Orders";
        ViewData["Subtitle"] = "Every order your desk received - online and at the counter.";
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentSearch"] = search;

        return View(orders);
    }

    /// <summary>Shows the detail, the timeline and the feasibility checks of one order.</summary>
    public async Task<IActionResult> Details(string id)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var order = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Plan!.Category)
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Include(o => o.Vendor)
            .Include(o => o.RetailShop)
            .Include(o => o.StatusHistory)
            .Include(o => o.FeasibilityChecks)
            .Include(o => o.Connections)
            .Include(o => o.Documents)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.VendorId == vendorId.Value);

        if (order is null)
        {
            TempData["Error"] = "This order does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Order {order.OrderId}";
        ViewData["Subtitle"] = $"{order.Customer?.FullName} - {order.Plan?.Name}";

        var type = await GetEmployeeTypeAsync();
        ViewData["CanOperate"] = VendorPermissions.CanOperate(type);
        ViewData["CanCancel"] = VendorPermissions.IsManager(type);

        return View(order);
    }

    /// <summary>Opens the walk-in order form of the retail counter.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanSell(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var model = await BuildOrderFormAsync(new VendorOrderFormViewModel());
        ViewData["Title"] = "Walk-in order";
        ViewData["Subtitle"] = "Register a customer who came to your counter and place the order.";
        return View(model);
    }

    /// <summary>Places the walk-in order on behalf of the customer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorOrderFormViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanSell(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        if (!ModelState.IsValid)
        {
            await BuildOrderFormAsync(model);
            ViewData["Title"] = "Walk-in order";
            return View(model);
        }

        // A walk-in customer is created on the spot when the counter staff picks a new name.
        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.Id == model.CustomerId && !c.IsDeleted);

        if (customer is null)
        {
            customer = new Models.Customer
            {
                FullName = model.CustomerName.Trim(),
                Phone = model.ContactNumber,
                Email = model.Email,
                Address = model.InstallationAddress.Trim(),
                CityId = model.CityId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            Db.Customers.Add(customer);
            await Db.SaveChangesAsync();
        }

        var result = await _orders.PlaceOrderAsync(new PlaceOrderRequest
        {
            CustomerId = customer.Id,
            VendorId = vendorId.Value,
            RetailShopId = model.RetailShopId,
            PlanId = model.PlanId,
            Quantity = model.Quantity,
            CityId = model.CityId ?? customer.CityId,
            InstallationAddress = model.InstallationAddress,
            ContactNumber = model.ContactNumber,
            ExistingLandlineAccountId = string.IsNullOrWhiteSpace(model.ExistingLandlineAccountId)
                ? null
                : model.ExistingLandlineAccountId.Trim().ToUpperInvariant(),
            EquipmentProductId = model.EquipmentProductId,
            PlacedByUserId = UserId,
            Notes = model.Notes
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The order could not be placed.");
            await BuildOrderFormAsync(model);
            ViewData["Title"] = "Walk-in order";
            return View(model);
        }

        await Audit.LogAsync("Order.PlacedAtCounter", "Order", result.OrderId, UserId, UserName, IpAddress,
            $"Walk-in order of {model.Quantity} connection(s) registered at the counter.");

        TempData["Success"] = $"The order {result.OrderId} has been registered. It now waits for the feasibility survey.";
        return RedirectToAction("Details", new { id = result.OrderId });
    }

    /// <summary>Cancels an order that cannot be served.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id, string reason)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.IsManager(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var order = await Db.Orders.FirstOrDefaultAsync(o => o.OrderId == id && o.VendorId == vendorId.Value);
        if (order is null)
        {
            TempData["Error"] = "This order does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        if (order.Status == OrderStatus.ConnectionProvided)
        {
            TempData["Error"] = "An order with a live connection cannot be cancelled.";
            return RedirectToAction("Details", new { id });
        }

        order.CancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by the service partner." : reason.Trim();
        await _orders.ChangeStatusAsync(order, OrderStatus.Cancelled, order.CancelReason, UserId, UserName);

        await Audit.LogAsync("Order.Cancelled", "Order", order.OrderId, UserId, UserName, IpAddress, order.CancelReason);

        TempData["Success"] = $"The order {order.OrderId} has been cancelled.";
        return RedirectToAction("Details", new { id });
    }

    // ------------------------------------------------------------------ helpers
    private async Task<VendorOrderFormViewModel> BuildOrderFormAsync(VendorOrderFormViewModel model)
    {
        var vendorId = await GetVendorIdAsync();

        model.Customers = await Db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        model.Plans = await _catalog.GetActivePlansAsync();
        model.Cities = await _catalog.GetActiveCitiesAsync();

        model.Equipment = await Db.Products.AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.StockQuantity > 0)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (vendorId.HasValue)
        {
            model.Shops = await Db.RetailShops.AsNoTracking()
                .Where(s => s.VendorId == vendorId.Value && s.IsActive && !s.IsDeleted)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        model.DiscountRules = await Db.DiscountRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();
        model.TaxPercent = await _settings.GetTaxPercentageAsync();

        // The counter staff is posted at a shop - that shop is the default of the form.
        var profile = await CurrentUser.GetProfileAsync();
        if (model.RetailShopId is null && profile.RetailShopId.HasValue)
        {
            model.RetailShopId = profile.RetailShopId;
        }

        return model;
    }
}
