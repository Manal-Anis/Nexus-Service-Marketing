using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>Overview of the customer dashboard.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController : BaseAreaController
{
    private readonly CatalogService _catalog;

    public DashboardController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit, CatalogService catalog)
        : base(db, currentUser, audit)
    {
        _catalog = catalog;
    }

    /// <summary>Shows the summary of the account of the signed in customer.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Profile", new { area = "Customer" });
        }

        var customer = await Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId);

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Where(b => b.Connection != null && b.Connection.CustomerId == customerId)
            .OrderByDescending(b => b.GeneratedOn)
            .ToListAsync();

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill)
            .Where(p => p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.CustomerId == customerId)
            .OrderByDescending(p => p.PaidOn)
            .ToListAsync();

        var deposits = await Db.SecurityDepositEntries.AsNoTracking()
            .Include(d => d.Connection)
            .Include(d => d.Order)
            .Where(d => d.Connection != null && d.Connection.CustomerId == customerId)
            .ToListAsync();

        var model = new CustomerDashboardViewModel
        {
            Customer = customer,
            Orders = orders,
            Connections = connections,
            Bills = bills,
            Payments = payments,
            Deposits = deposits,
            TotalDue = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            TotalPaid = bills.Sum(b => b.AmountPaid),
            ActiveConnections = connections.Count(c => c.Status == ConnectionStatus.Active),
            PendingOrders = orders.Count(o => o.Status is OrderStatus.Placed or OrderStatus.FeasibilityPending or OrderStatus.Feasible)
        };

        ViewData["Title"] = "My dashboard";
        ViewData["Subtitle"] = $"Welcome back, {customer.FullName}";
        return View(model);
    }
}
