using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>Overview of the whole network for the administrators of Nexus.</summary>
public class DashboardController : AdminControllerBase
{
    private readonly ConnectionService _connections;

    public DashboardController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        ConnectionService connections)
        : base(db, currentUser, audit)
    {
        _connections = connections;
    }

    public async Task<IActionResult> Index()
    {
        // A bill that stayed unpaid suspends the connection, the counters always show the current state.
        await _connections.ApplyOverdueRuleAsync();

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.City)
            .Where(o => !o.IsDeleted)
            .ToListAsync();
        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan).ThenInclude(p => p!.Category)
            .Include(c => c.City)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking().ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status is OrderStatus.Placed or OrderStatus.FeasibilityPending),
            FeasibleOrders = orders.Count(o => o.Status == OrderStatus.Feasible),
            TotalConnections = connections.Count,
            ActiveConnections = connections.Count(c => c.Status == ConnectionStatus.Active),
            InactiveConnections = connections.Count(c => c.Status != ConnectionStatus.Active),
            TotalCustomers = await Db.Customers.AsNoTracking().CountAsync(c => !c.IsDeleted),
            ApprovedVendors = await Db.Vendors.AsNoTracking().CountAsync(v => v.Status == VendorStatus.Approved && !v.IsDeleted),
            PendingVendors = await Db.Vendors.AsNoTracking().CountAsync(v => v.Status == VendorStatus.Pending && !v.IsDeleted),
            TotalInvoices = bills.Count,
            TotalBilled = bills.Sum(b => b.TotalAmount),
            Collected = bills.Where(b => b.Status == BillStatus.Paid).Sum(b => b.AmountPaid),
            Outstanding = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            OpenTickets = await Db.Feedback.AsNoTracking().CountAsync(f => !f.IsApproved),
            LowStockItems = await Db.Products.AsNoTracking()
                .CountAsync(p => !p.IsDeleted && p.IsActive && p.StockQuantity <= p.ReorderLevel)
        };

        model.OrdersByStatus = orders
            .GroupBy(o => o.Status.ToDisplayName())
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        model.ConnectionsByType = connections
            .GroupBy(c => c.Plan?.Category?.Name ?? "Other")
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        model.RevenueTrend = Enumerable.Range(0, 6).Select(i =>
        {
            var month = DateTime.Today.AddMonths(-5 + i);
            var key = month.ToString("MMM yyyy");
            var amount = bills
                .Where(b => b.GeneratedOn.Year == month.Year && b.GeneratedOn.Month == month.Month)
                .Sum(b => b.TotalAmount);
            return (key, amount);
        }).ToList();

        model.OrderTrend = Enumerable.Range(0, 6).Select(i =>
        {
            var month = DateTime.Today.AddMonths(-5 + i);
            var key = month.ToString("MMM yyyy");
            var count = orders.Count(o => o.PlacedAt.Year == month.Year && o.PlacedAt.Month == month.Month);
            return (key, count);
        }).ToList();

        model.TopPlans = orders
            .GroupBy(o => o.Plan?.Name ?? "Without a plan")
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => (g.Key, g.Count()))
            .ToList();

        model.TopCities = orders
            .GroupBy(o => o.City?.Name ?? "Without a city")
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => (g.Key, g.Count()))
            .ToList();

        ViewData["Title"] = "Administrator dashboard";
        ViewData["Subtitle"] = "The whole Nexus network in a single screen.";

        return View(model);
    }
}
