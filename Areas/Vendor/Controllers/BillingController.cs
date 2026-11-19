using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>
/// Billing of the service partner. Only the accounts staff and the manager may open these
/// screens (requirement section 3.2).
/// </summary>
public class BillingController : VendorControllerBase
{
    private readonly BillService _bills;
    private readonly SettingsService _settings;
    private readonly ConnectionService _connections;

    public BillingController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        BillService bills, SettingsService settings, ConnectionService connections)
        : base(db, currentUser, audit)
    {
        _bills = bills;
        _settings = settings;
        _connections = connections;
    }

    /// <summary>Lists the bills of the partner.</summary>
    public async Task<IActionResult> Index(string? status = null)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanBill(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var query = Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.Plan)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BillStatus>(status, true, out var parsed))
        {
            query = query.Where(b => b.Status == parsed);
        }

        var bills = await query.OrderByDescending(b => b.GeneratedOn).ToListAsync();

        ViewData["Title"] = "Billing";
        ViewData["Subtitle"] = "The invoices raised by your desk and the money that is still open.";
        ViewData["CurrentStatus"] = status;

        return View(bills);
    }

    /// <summary>Opens the form that generates the periodic bill of a connection.</summary>
    [HttpGet]
    public async Task<IActionResult> Create(int? connectionId = null)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanBill(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Customer)
            .Where(c => c.VendorId == vendorId.Value && c.Status == ConnectionStatus.Active)
            .OrderBy(c => c.AccountId)
            .ToListAsync();

        var model = new GenerateBillViewModel();

        if (connectionId.HasValue)
        {
            var selected = connections.FirstOrDefault(c => c.Id == connectionId.Value);
            if (selected is not null)
            {
                model.ConnectionId = selected.Id;
                model.AccountId = selected.AccountId;
                model.CustomerName = selected.Customer?.FullName ?? string.Empty;
                model.PlanName = selected.Plan?.Name ?? string.Empty;
            }
        }

        ViewData["Title"] = "Generate a bill";
        ViewData["Subtitle"] = "Postpaid billing - the invoice is raised after the period has ended.";
        ViewData["Connections"] = connections;

        return View(model);
    }

    /// <summary>Calculates and stores the periodic bill of one connection.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GenerateBillViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanBill(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var connection = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == model.ConnectionId && c.VendorId == vendorId.Value);

        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        if (model.PeriodEnd < model.PeriodStart)
        {
            ModelState.AddModelError("PeriodEnd", "The end of the period cannot be before its start.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Connections"] = await Db.Connections.AsNoTracking()
                .Include(c => c.Plan)
                .Include(c => c.Customer)
                .Where(c => c.VendorId == vendorId.Value && c.Status == ConnectionStatus.Active)
                .ToListAsync();
            ViewData["Title"] = "Generate a bill";
            return View(model);
        }

        var result = await _bills.GeneratePeriodicBillAsync(new GenerateBillInput
        {
            ConnectionId = model.ConnectionId,
            PeriodStart = model.PeriodStart,
            PeriodEnd = model.PeriodEnd,
            LocalMinutes = model.LocalMinutes,
            StdMinutes = model.StdMinutes,
            MobileMinutes = model.MobileMinutes,
            HoursUsed = model.HoursUsed,
            ReplacementCharge = model.ReplacementCharge,
            ReplacementReason = model.ReplacementReason,
            DiscountPercent = model.DiscountPercent,
            Remarks = model.Remarks,
            UserId = UserId,
            UserName = UserName
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The bill could not be generated.");
            ViewData["Connections"] = await Db.Connections.AsNoTracking()
                .Include(c => c.Plan)
                .Include(c => c.Customer)
                .Where(c => c.VendorId == vendorId.Value && c.Status == ConnectionStatus.Active)
                .ToListAsync();
            ViewData["Title"] = "Generate a bill";
            return View(model);
        }

        await Audit.LogAsync("Bill.Generated", "Bill", result.BillNumber, UserId, UserName, IpAddress,
            $"Periodic bill for {connection.AccountId} raised for {model.PeriodStart:dd MMM yyyy} to {model.PeriodEnd:dd MMM yyyy}.");

        // A bill that is already overdue suspends the connection.
        await _connections.ApplyOverdueRuleAsync(vendorId);

        TempData["Success"] = $"The bill {result.BillNumber} has been raised - {result.TotalAmount:N2} Rs are payable at the counter.";
        return RedirectToAction("Details", new { id = result.BillId });
    }

    /// <summary>Shows the printable invoice.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanBill(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.Plan)
            .Include(b => b.Connection!.City)
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id && b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (bill is null)
        {
            TempData["Error"] = "This bill does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Bill {bill.BillNumber}";
        ViewData["Subtitle"] = $"{bill.Connection?.Customer?.FullName} - {bill.TotalAmount:N2} Rs";

        return View(bill);
    }

    /// <summary>Shows the printable invoice of the partner.</summary>
    public async Task<IActionResult> Print(int id)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanBill(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.Plan)
            .Include(b => b.Connection!.City)
            .Include(b => b.Connection!.Vendor)
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id && b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (bill is null)
        {
            TempData["Error"] = "This bill does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Invoice {bill.BillNumber}";

        return View(bill);
    }
}
