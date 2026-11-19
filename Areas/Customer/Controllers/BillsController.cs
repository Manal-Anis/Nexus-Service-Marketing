using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The bills of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class BillsController : BaseAreaController
{
    public BillsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the bills of the customer.</summary>
    public async Task<IActionResult> Index(string? status = null)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var query = Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Plan)
            .Where(b => b.Connection != null && b.Connection.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BillStatus>(status, true, out var parsed))
        {
            query = query.Where(b => b.Status == parsed);
        }

        var bills = await query.OrderByDescending(b => b.GeneratedOn).ToListAsync();

        ViewData["Title"] = "My bills";
        ViewData["Subtitle"] = "Every invoice of your account. Payment is made in cash at the counter.";
        ViewData["CurrentStatus"] = status;

        return View(bills);
    }

    /// <summary>Shows one invoice.</summary>
    public async Task<IActionResult> Details(string billNumber)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Plan)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.City)
            .Include(b => b.Connection!.Vendor)
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillNumber == billNumber && b.Connection != null && b.Connection.CustomerId == customerId);

        if (bill is null)
        {
            TempData["Error"] = "This invoice does not belong to your account.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Invoice {bill.BillNumber}";
        return View(bill);
    }

    /// <summary>Printable version of the invoice.</summary>
    public async Task<IActionResult> Print(string billNumber)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Plan)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.City)
            .Include(b => b.Connection!.Vendor)
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillNumber == billNumber && b.Connection != null && b.Connection.CustomerId == customerId);

        if (bill is null)
        {
            TempData["Error"] = "This invoice does not belong to your account.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Invoice {bill.BillNumber}";
        return View(bill);
    }
}
