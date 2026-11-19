using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>The reports of the partner: the order book, the billing and the collection.</summary>
public class ReportsController : VendorControllerBase
{
    private readonly CsvExportService _csv;

    public ReportsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit, CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Shows the report of the selected period.</summary>
    public async Task<IActionResult> Index(string? from = null, string? to = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var start = ParseDate(from, DateTime.Today.AddMonths(-1));
        var end = ParseDate(to, DateTime.Today).AddDays(1);

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Plan!.Category)
            .Include(o => o.Customer)
            .Where(o => o.VendorId == vendorId.Value && !o.IsDeleted && o.PlacedAt >= start && o.PlacedAt < end)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.Plan)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendorId.Value && b.GeneratedOn >= start && b.GeneratedOn < end)
            .ToListAsync();

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill)
            .Where(p => p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.VendorId == vendorId.Value
                        && p.PaidOn >= start && p.PaidOn < end)
            .ToListAsync();

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Plan!.Category)
            .Where(c => c.VendorId == vendorId.Value && c.ActivationDate >= start && c.ActivationDate < end)
            .ToListAsync();

        var model = new VendorReportsViewModel
        {
            From = start.ToString("yyyy-MM-dd"),
            To = end.AddDays(-1).ToString("yyyy-MM-dd"),
            TotalOrders = orders.Count,
            TotalConnections = connections.Count,
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalBilled = bills.Sum(b => b.TotalAmount),
            TotalCollected = payments.Sum(p => p.Amount),
            TotalOutstanding = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            TotalDiscount = orders.Sum(o => (o.SecurityDepositAmount - o.SecurityDepositPayable) + (o.AdvanceAmount - o.AdvancePayable)),
            TotalTax = bills.Sum(b => b.TaxAmount) + orders.Sum(o => o.TaxAmount),
            Orders = orders,
            Bills = bills
        };

        foreach (var group in orders.GroupBy(o => o.Plan?.Category?.Name ?? "Other"))
        {
            model.OrdersByCategory.Add((group.Key, group.Count(), group.Sum(o => o.TotalAmount)));
        }

        foreach (var group in connections.GroupBy(c => c.Plan?.Category?.Name ?? "Other"))
        {
            model.ConnectionsByCategory.Add((group.Key, group.Count(), group.Sum(c => c.MonthlyCharge)));
        }

        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var count = orders.Count(o => o.Status == status);
            if (count > 0)
            {
                model.OrdersByStatus.Add((status.ToDisplayName(), count));
            }
        }

        var firstMonth = new DateTime(start.Year, start.Month, 1);
        for (var i = 0; i < 6; i++)
        {
            var month = firstMonth.AddMonths(i);
            var next = month.AddMonths(1);
            var amount = payments.Where(p => p.PaidOn >= month && p.PaidOn < next).Sum(p => p.Amount);
            model.MonthlyCollection.Add((month.ToString("MMM yyyy"), amount));
        }

        ViewData["Title"] = "Reports";
        ViewData["Subtitle"] = "The numbers of your service desk for the selected period.";

        return View(model);
    }

    /// <summary>Downloads the order book of the period as a CSV file.</summary>
    public async Task<IActionResult> ExportOrders(string? from = null, string? to = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var start = ParseDate(from, DateTime.Today.AddMonths(-1));
        var end = ParseDate(to, DateTime.Today).AddDays(1);

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Plan!.Category)
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Where(o => o.VendorId == vendorId.Value && !o.IsDeleted && o.PlacedAt >= start && o.PlacedAt < end)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync();

        var csv = _csv.Build(
            new[] { "Order ID", "Date", "Customer", "Contact", "Service", "Plan", "Quantity", "Status", "Discount %", "Total" },
            orders.Select(o => new object?[]
            {
                o.OrderId,
                o.PlacedAt.ToString("yyyy-MM-dd"),
                o.Customer?.FullName ?? string.Empty,
                o.ContactNumber ?? string.Empty,
                o.Plan?.Category?.Name ?? string.Empty,
                o.Plan?.Name ?? string.Empty,
                o.Quantity,
                o.Status.ToDisplayName(),
                o.DiscountPercent,
                o.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)
            }));

        await Audit.LogAsync("Report.Exported", "Order", "Orders", UserId, UserName, IpAddress,
            $"The order book of {start:yyyy-MM-dd} to {end.AddDays(-1):yyyy-MM-dd} was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-orders-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}.csv");
    }

    /// <summary>Downloads the invoice book of the period as a CSV file.</summary>
    public async Task<IActionResult> ExportBills(string? from = null, string? to = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var start = ParseDate(from, DateTime.Today.AddMonths(-1));
        var end = ParseDate(to, DateTime.Today).AddDays(1);

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendorId.Value && b.GeneratedOn >= start && b.GeneratedOn < end)
            .OrderBy(b => b.GeneratedOn)
            .ToListAsync();

        var csv = _csv.Build(
            new[] { "Bill number", "Generated on", "Account ID", "Customer", "Type", "Period start", "Period end", "Sub total", "Tax", "Total", "Paid", "Due", "Status" },
            bills.Select(b => new object?[]
            {
                b.BillNumber,
                b.GeneratedOn.ToString("yyyy-MM-dd"),
                b.Connection?.AccountId ?? string.Empty,
                b.Connection?.Customer?.FullName ?? string.Empty,
                b.BillType.ToDisplayName(),
                b.PeriodStart.ToString("yyyy-MM-dd"),
                b.PeriodEnd.ToString("yyyy-MM-dd"),
                b.SubTotal.ToString("N2", CultureInfo.InvariantCulture),
                b.TaxAmount.ToString("N2", CultureInfo.InvariantCulture),
                b.TotalAmount.ToString("N2", CultureInfo.InvariantCulture),
                b.AmountPaid.ToString("N2", CultureInfo.InvariantCulture),
                b.AmountDue.ToString("N2", CultureInfo.InvariantCulture),
                b.Status.ToDisplayName()
            }));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-invoices-{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}.csv");
    }

    /// <summary>Reads a date from the query string and falls back to the supplied default.</summary>
    private static DateTime ParseDate(string? value, DateTime fallback)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : fallback;
}
