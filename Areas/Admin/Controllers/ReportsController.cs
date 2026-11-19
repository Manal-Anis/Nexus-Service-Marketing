using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The reports of the network - the income, the orders, the connections and the partners.
/// </summary>
public class ReportsController : AdminControllerBase
{
    public ReportsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Shows the report of the selected period.</summary>
    public async Task<IActionResult> Index(string? from = null, string? to = null)
    {
        var start = DateTime.TryParse(from, out var parsedFrom) ? parsedFrom.Date : DateTime.Today.AddMonths(-5).Date;
        var end = DateTime.TryParse(to, out var parsedTo) ? parsedTo.Date : DateTime.Today;

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Vendor)
            .Include(o => o.City)
            .Include(o => o.Plan)
            .Where(o => !o.IsDeleted && o.PlacedAt >= start && o.PlacedAt <= end.AddDays(1))
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Vendor)
            .Where(b => b.GeneratedOn >= start && b.GeneratedOn <= end.AddDays(1))
            .ToListAsync();

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill).ThenInclude(b => b!.Connection).ThenInclude(c => c!.Vendor)
            .Where(p => p.PaidOn >= start && p.PaidOn <= end.AddDays(1))
            .ToListAsync();

        var model = new AdminReportsViewModel
        {
            From = start,
            To = end,
            TotalOrders = orders.Count,
            TotalBilled = bills.Sum(b => b.TotalAmount),
            TotalCollected = payments.Sum(p => p.Amount),
            TotalOutstanding = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            TaxCollected = bills.Sum(b => b.TaxAmount),
            DiscountGiven = bills.Sum(b => b.DiscountAmount),
            ActiveConnections = await Db.Connections.AsNoTracking().CountAsync(c => c.Status == ConnectionStatus.Active),
            Customers = await Db.Customers.AsNoTracking().CountAsync(c => !c.IsDeleted)
        };

        // The income per month of the selected period.
        model.MonthlyTrend = Enumerable.Range(0, 6).Select(i =>
        {
            var month = DateTime.Today.AddMonths(-5 + i);
            var key = month.ToString("MMM yyyy");
            var billed = bills.Where(b => b.GeneratedOn.Year == month.Year && b.GeneratedOn.Month == month.Month).Sum(b => b.TotalAmount);
            var collected = payments.Where(p => p.PaidOn.Year == month.Year && p.PaidOn.Month == month.Month).Sum(p => p.Amount);
            return (key, billed, collected);
        }).ToList();

        // The turnover per service partner.
        model.PerPartner = orders
            .GroupBy(o => o.Vendor?.BusinessName ?? "Directly by Nexus")
            .Select(g => (Partner: g.Key, Orders: g.Count(), Amount: g.Sum(o => o.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        // The turnover per city.
        model.PerCity = orders
            .GroupBy(o => o.City?.Name ?? "Without a city")
            .Select(g => (City: g.Key, Orders: g.Count(), Amount: g.Sum(o => o.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        // The most ordered plans.
        model.TopPlans = orders
            .GroupBy(o => o.Plan?.Name ?? "Without a plan")
            .Select(g => (Plan: g.Key, Count: g.Count(), Amount: g.Sum(o => o.TotalAmount)))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        ViewData["Title"] = "Reports";
        ViewData["Subtitle"] = "The income, the orders and the connections of the network.";
        ViewData["From"] = start.ToString("yyyy-MM-dd");
        ViewData["To"] = end.ToString("yyyy-MM-dd");

        return View(model);
    }
}
