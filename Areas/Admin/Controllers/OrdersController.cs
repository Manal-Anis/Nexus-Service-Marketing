using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// Every order of the network. The administrator only watches the orders here - the partner that
/// received the order owns the processing.
/// </summary>
public class OrdersController : AdminControllerBase
{
    private readonly CsvExportService _csv;

    public OrdersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Lists the orders with a search, a filter and server side paging.</summary>
    public async Task<IActionResult> Index(string? search = null, string? status = null, string? vendor = null, int page = 1)
    {
        const int pageSize = 25;

        var query = Db.Orders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.Vendor)
            .Include(o => o.City)
            .Include(o => o.Connections)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.OrderId.Contains(term) ||
                                     (o.Customer != null && o.Customer.FullName.Contains(term)) ||
                                     (o.ContactNumber != null && o.ContactNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(vendor))
        {
            var term = vendor.Trim();
            query = query.Where(o => o.Vendor != null && o.Vendor.BusinessName.Contains(term));
        }

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.PlacedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Orders";
        ViewData["Subtitle"] = "Every order that reached the network and the partner that owns it.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentVendor"] = vendor;
        ViewData["TotalRows"] = total;
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(orders);
    }

    /// <summary>Shows one order with its status timeline.</summary>
    public async Task<IActionResult> Details(string id)
    {
        var order = await Db.Orders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan).ThenInclude(p => p!.Category)
            .Include(o => o.Vendor)
            .Include(o => o.City)
            .Include(o => o.EquipmentProduct)
            .Include(o => o.StatusHistory)
            .Include(o => o.FeasibilityChecks)
            .Include(o => o.Connections)
            .ThenInclude(c => c.Bills)
            .FirstOrDefaultAsync(o => o.OrderId == id && !o.IsDeleted);

        if (order is null)
        {
            return Missing("This order does not exist.");
        }

        ViewData["Title"] = $"Order {order.OrderId}";
        ViewData["Subtitle"] = $"{order.Customer?.FullName} - {order.Plan?.Name}.";

        return View(order);
    }

    /// <summary>Downloads the order list as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? status = null, string? vendor = null)
    {
        var query = Db.Orders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.Vendor)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => o.OrderId.Contains(term) ||
                                     (o.Customer != null && o.Customer.FullName.Contains(term)) ||
                                     (o.ContactNumber != null && o.ContactNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(vendor))
        {
            var term = vendor.Trim();
            query = query.Where(o => o.Vendor != null && o.Vendor.BusinessName.Contains(term));
        }

        var orders = await query.OrderByDescending(o => o.PlacedAt).ToListAsync();

        var csv = _csv.Build(
            new[] { "Order id", "Type", "Customer", "Plan", "Partner", "Placed", "Status", "Deposit", "Advance", "Tax", "Total" },
            orders.Select(o => new object?[]
            {
                o.OrderId,
                o.TypeLetter,
                o.Customer?.FullName ?? string.Empty,
                o.Plan?.Name ?? string.Empty,
                o.Vendor?.BusinessName ?? string.Empty,
                o.PlacedAt.ToString("yyyy-MM-dd"),
                o.Status.ToString(),
                o.SecurityDepositPayable,
                o.AdvancePayable,
                o.TaxAmount,
                o.TotalAmount
            }));

        await Audit.LogAsync("Order.Exported", "Order", "Orders", UserId, UserName, IpAddress,
            $"The order list ({orders.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-orders-{DateTime.Today:yyyyMMdd}.csv");
    }
}
