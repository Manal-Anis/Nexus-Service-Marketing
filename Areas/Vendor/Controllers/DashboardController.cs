using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>Overview of the vendor (service partner) dashboard.</summary>
public class DashboardController : VendorControllerBase
{
    public DashboardController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Shows the KPI cards and the charts of the partner.</summary>
    public async Task<IActionResult> Index()
    {
        var vendor = await GetVendorAsync();
        if (vendor is null)
        {
            TempData["Error"] = "Your account is not linked to a service partner yet.";
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Where(o => o.VendorId == vendor.Id && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Customer)
            .Where(c => c.VendorId == vendor.Id)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendor.Id)
            .ToListAsync();

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill)
            .Where(p => p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.VendorId == vendor.Id)
            .ToListAsync();

        var model = new VendorDashboardViewModel
        {
            Vendor = vendor,
            OwnerName = vendor.OwnerName ?? vendor.BusinessName,
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status is OrderStatus.Placed or OrderStatus.FeasibilityPending),
            FeasibleOrders = orders.Count(o => o.Status == OrderStatus.Feasible),
            TotalConnections = connections.Count,
            ActiveConnections = connections.Count(c => c.Status == ConnectionStatus.Active),
            InactiveConnections = connections.Count(c => c.Status != ConnectionStatus.Active),
            CollectedAmount = payments.Sum(p => p.Amount),
            OutstandingAmount = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            MonthlyRevenue = bills.Where(b => b.PeriodEnd >= DateTime.Today.AddMonths(-1)).Sum(b => b.TotalAmount),
            RecentOrders = orders.Take(8).ToList(),
            OrdersAwaitingFeasibility = orders.Where(o => o.Status == OrderStatus.FeasibilityPending || o.Status == OrderStatus.Placed).Take(6).ToList(),
            RecentConnections = connections.OrderByDescending(c => c.ActivationDate).Take(6).ToList()
        };

        // ------------------------------------------------------------- chart: orders per state
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var count = orders.Count(o => o.Status == status);
            if (count > 0)
            {
                model.OrdersByStatus[status.ToDisplayName()] = count;
            }
        }

        // ------------------------------------------------------------- chart: connections per service
        foreach (var group in connections.GroupBy(c => c.Plan?.Category?.Name ?? "Other"))
        {
            model.ConnectionsByType[group.Key] = group.Count();
        }

        // ------------------------------------------------------------- chart: collected per month
        var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        for (var i = 0; i < 6; i++)
        {
            var month = firstMonth.AddMonths(i);
            var next = month.AddMonths(1);
            var amount = payments.Where(p => p.PaidOn >= month && p.PaidOn < next).Sum(p => p.Amount);
            model.CollectionTrend.Add((month.ToString("MMM yyyy"), amount));
        }

        ViewData["Title"] = "Partner dashboard";
        ViewData["Subtitle"] = $"{vendor.BusinessName} - the numbers of your service desk.";

        return View(model);
    }
}
