using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The orders of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class OrdersController : BaseAreaController
{
    public OrdersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the orders of the customer.</summary>
    public async Task<IActionResult> Index(string? status = null)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var query = Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Vendor)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        var orders = await query.OrderByDescending(o => o.PlacedAt).ToListAsync();

        ViewData["Title"] = "My orders";
        ViewData["Subtitle"] = "Follow the progress of every order you placed.";
        ViewData["CurrentStatus"] = status;

        return View(orders);
    }

    /// <summary>Shows the detail and the timeline of one order.</summary>
    public async Task<IActionResult> Details(string id)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var order = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Plan!.Category)
            .Include(o => o.Vendor)
            .Include(o => o.City)
            .Include(o => o.StatusHistory)
            .Include(o => o.FeasibilityChecks)
            .Include(o => o.Connections)
            .Include(o => o.Documents)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId);

        if (order is null)
        {
            TempData["Error"] = "This order does not belong to your account.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = $"Order {order.OrderId}";
        ViewData["Subtitle"] = order.Plan?.Name;

        return View(order);
    }
}
