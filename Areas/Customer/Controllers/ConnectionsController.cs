using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The connections of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ConnectionsController : BaseAreaController
{
    public ConnectionsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the connections of the customer.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Plan!.Category)
            .Include(c => c.City)
            .Include(c => c.Vendor)
            .Include(c => c.Equipment)
            .Include(c => c.PlanChanges)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        ViewData["Title"] = "My connections";
        ViewData["Subtitle"] = "The live services of your account and their equipment.";

        return View(connections);
    }

    /// <summary>Shows one connection with its bills and the plan changes.</summary>
    public async Task<IActionResult> Details(string accountId)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var connection = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Plan!.Category)
            .Include(c => c.City)
            .Include(c => c.Vendor)
            .Include(c => c.Equipment)
            .Include(c => c.PlanChanges)
            .Include(c => c.Bills)
            .FirstOrDefaultAsync(c => c.AccountId == accountId && c.CustomerId == customerId);

        if (connection is null)
        {
            TempData["Error"] = "This connection does not belong to your account.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Connection {connection.AccountId}";
        ViewData["Subtitle"] = connection.Plan?.Name;

        return View(connection);
    }
}
