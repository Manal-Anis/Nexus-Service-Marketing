using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>Connections of the service partner: creation, states and plan changes.</summary>
public class ConnectionsController : VendorControllerBase
{
    private readonly ConnectionService _connections;
    private readonly OrderService _orders;

    public ConnectionsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        ConnectionService connections, OrderService orders)
        : base(db, currentUser, audit)
    {
        _connections = connections;
        _orders = orders;
    }

    /// <summary>Lists the connections of the partner.</summary>
    public async Task<IActionResult> Index(string? status = null, string? search = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var query = Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Plan!.Category)
            .Include(c => c.Customer)
            .Include(c => c.City)
            .Where(c => c.VendorId == vendorId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConnectionStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.AccountId.Contains(term) ||
                                     (c.Customer != null && c.Customer.FullName.Contains(term)) ||
                                     (c.InstallationAddress != null && c.InstallationAddress.Contains(term)));
        }

        var connections = await query.OrderByDescending(c => c.ActivationDate).ToListAsync();

        // A bill that stayed unpaid suspends the connection, the list always shows the current state.
        await _connections.ApplyOverdueRuleAsync(vendorId);

        ViewData["Title"] = "Connections";
        ViewData["Subtitle"] = "The live services of your desk and their state.";
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentSearch"] = search;

        return View(connections);
    }

    /// <summary>Shows the detail of one connection.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        // A bill that stayed unpaid suspends the connection, the screen always shows the current state.
        await _connections.ApplyOverdueRuleAsync(vendorId);

        var connection = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Plan!.Category)
            .Include(c => c.Customer)
            .Include(c => c.City)
            .Include(c => c.Order)
            .Include(c => c.Equipment)
            .ThenInclude(e => e.Product)
            .Include(c => c.Bills)
            .Include(c => c.PlanChanges)
            .ThenInclude(p => p.NewPlan)
            .FirstOrDefaultAsync(c => c.Id == id && c.VendorId == vendorId.Value);

        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Connection {connection.AccountId}";
        ViewData["Subtitle"] = $"{connection.Customer?.FullName} - {connection.Plan?.Name}";

        var type = await GetEmployeeTypeAsync();
        ViewData["CanOperate"] = VendorPermissions.CanOperate(type);
        ViewData["CanBill"] = VendorPermissions.CanBill(type);
        ViewData["Plans"] = await Db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ApprovalStatus == PlanApprovalStatus.Approved)
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        return View(connection);
    }

    /// <summary>Opens the form that creates the connection of a feasible order.</summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? orderId = null)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Customer)
            .Include(o => o.EquipmentProduct)
            .Where(o => o.VendorId == vendorId.Value && o.Status == OrderStatus.Feasible && !o.IsDeleted)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync();

        Order? selected = null;
        if (orderId.HasValue)
        {
            selected = orders.FirstOrDefault(o => o.Id == orderId.Value);
        }
        else if (orders.Count == 1)
        {
            selected = orders[0];
        }

        var model = new CreateConnectionViewModel
        {
            Equipment = await Db.Products.AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted && p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync()
        };

        if (selected is not null)
        {
            model.OrderId = selected.Id;
            model.OrderNumber = selected.OrderId;
            model.PlanName = selected.Plan?.Name ?? string.Empty;
            model.CustomerName = selected.Customer?.FullName ?? string.Empty;
            model.Quantity = selected.Quantity;
            model.EquipmentProductName = selected.EquipmentProduct?.Name ?? string.Empty;
        }

        ViewData["Title"] = "Create a connection";
        ViewData["Subtitle"] = "Only a feasible order can be activated.";
        ViewData["FeasibleOrders"] = orders;

        return View(model);
    }

    /// <summary>Creates the connection, issues the account id and raises the activation invoice.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateConnectionViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var order = await Db.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId && o.VendorId == vendorId.Value);
        if (order is null)
        {
            TempData["Error"] = "This order does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _connections.CreateConnectionAsync(order, new CreateConnectionOptions
        {
            EquipmentSerialNumber = model.EquipmentSerialNumber,
            UserId = UserId,
            UserName = UserName
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The connection could not be created.");
            model.Equipment = await Db.Products.AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted && p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewData["FeasibleOrders"] = await Db.Orders.AsNoTracking()
                .Include(o => o.Plan)
                .Include(o => o.Customer)
                .Where(o => o.VendorId == vendorId.Value && o.Status == OrderStatus.Feasible && !o.IsDeleted)
                .ToListAsync();
            ViewData["Title"] = "Create a connection";
            return View(model);
        }

        await _orders.ChangeStatusAsync(order, OrderStatus.ConnectionProvided,
            $"The connection {result.AccountId} has been activated.", UserId, UserName);

        await Audit.LogAsync("Connection.Created", "Connection", result.AccountId, UserId, UserName, IpAddress,
            $"Connection created for the order {order.OrderId}, activation invoice {result.BillNumber}.");

        TempData["Success"] = $"The connection {result.AccountId} is live. The activation invoice {result.BillNumber} has been raised.";

        var created = await Db.Connections.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == result.AccountId);

        return created is null
            ? RedirectToAction(nameof(Index))
            : RedirectToAction("Details", new { id = created.Id });
    }

    /// <summary>Changes the service state of a connection.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ConnectionStatusViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var connection = await Db.Connections.FirstOrDefaultAsync(c => c.Id == model.ConnectionId && c.VendorId == vendorId.Value);
        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        connection.Status = model.Status;
        connection.StatusReason = model.Reason;
        connection.StatusChangedAt = DateTime.Now;
        connection.StatusChangedByUserId = UserId;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Connection.StatusChanged", "Connection", connection.AccountId, UserId, UserName, IpAddress,
            $"The connection moved to {model.Status.ToDisplayName()}. Reason: {model.Reason}");

        TempData["Success"] = $"The connection {connection.AccountId} is now {model.Status.ToDisplayName().ToLowerInvariant()}.";
        return RedirectToAction("Details", new { id = connection.Id });
    }

    /// <summary>Moves a connection to another plan.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(PlanChangeViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanChangePlan(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var connection = await Db.Connections
            .Include(c => c.PlanChanges)
            .FirstOrDefaultAsync(c => c.Id == model.ConnectionId && c.VendorId == vendorId.Value);

        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var newPlan = await Db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.NewPlanId && p.IsActive);
        if (newPlan is null)
        {
            TempData["Error"] = "The selected plan is not available.";
            return RedirectToAction("Details", new { id = connection.Id });
        }

        var oldPlanId = connection.PlanId;
        connection.PlanId = newPlan.Id;
        connection.MonthlyCharge = newPlan.Price;

        connection.PlanChanges.Add(new PlanChangeHistory
        {
            OldPlanId = oldPlanId,
            NewPlanId = newPlan.Id,
            Reason = model.Reason ?? "Plan changed by the service partner.",
            Status = PlanChangeStatus.Applied,
            RequestedByUserId = UserId,
            RequestedByName = UserName,
            RequestedAt = DateTime.Now,
            AppliedByUserId = UserId,
            AppliedAt = DateTime.Now
        });

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Connection.PlanChanged", "Connection", connection.AccountId, UserId, UserName, IpAddress,
            $"The plan changed from {oldPlanId} to {newPlan.Name}.");

        TempData["Success"] = $"The connection {connection.AccountId} now runs on {newPlan.Name}.";
        return RedirectToAction("Details", new { id = connection.Id });
    }

    /// <summary>Records the replacement of a damaged device.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplaceEquipment(EquipmentReplacementViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var connection = await Db.Connections
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == model.ConnectionId && c.VendorId == vendorId.Value);

        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var device = connection.Equipment.OrderByDescending(e => e.AssignedAt).FirstOrDefault();
        if (device is null)
        {
            TempData["Error"] = "No equipment is issued on this connection.";
            return RedirectToAction("Details", new { id = connection.Id });
        }

        device.IsReplaced = true;
        device.ReplacedAt = DateTime.Now;
        device.ReplacementReason = model.Reason;
        device.ReplacementChargeAmount = model.Charge;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Equipment.Replaced", "Connection", connection.AccountId, UserId, UserName, IpAddress,
            $"The device was replaced. Reason: {model.Reason}. Charge: {model.Charge:N2}");

        TempData["Success"] = $"The replacement of {device.Product?.Name ?? "the device"} has been recorded.";
        return RedirectToAction("Details", new { id = connection.Id });
    }
}
