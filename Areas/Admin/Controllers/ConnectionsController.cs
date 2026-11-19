using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>Every connection of the network with its state and its account number.</summary>
public class ConnectionsController : AdminControllerBase
{
    private readonly ConnectionService _connections;
    private readonly CsvExportService _csv;

    public ConnectionsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        ConnectionService connections, CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _connections = connections;
        _csv = csv;
    }

    /// <summary>Lists the connections with a search and a filter.</summary>
    public async Task<IActionResult> Index(string? search = null, string? status = null, string? type = null, int page = 1)
    {
        const int pageSize = 25;

        // A bill that stayed unpaid suspends the connection, the list shows the current state.
        await _connections.ApplyOverdueRuleAsync();

        // The query keeps the type of the base set so that the filters can be chained on it.
        IQueryable<Connection> query = Db.Connections.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan).ThenInclude(p => p!.Category)
            .Include(c => c.Vendor)
            .Include(c => c.City)
            .Include(c => c.Bills);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.AccountId.Contains(term) ||
                                     (c.Customer != null && c.Customer.FullName.Contains(term)) ||
                                     (c.Order != null && c.Order.OrderId.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConnectionStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var term = type.Trim();
            query = query.Where(c => c.Plan != null && c.Plan.Category != null && c.Plan.Category.Name.Contains(term));
        }

        var total = await query.CountAsync();

        var connections = await query
            .OrderByDescending(c => c.ActivationDate)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Connections";
        ViewData["Subtitle"] = "Every live connection of the network and its account number.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentType"] = type;
        ViewData["TotalRows"] = total;
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(connections);
    }

    /// <summary>Shows one connection with its bills and the plan changes.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var connection = await Db.Connections.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan).ThenInclude(p => p!.Category)
            .Include(c => c.Vendor)
            .Include(c => c.City)
            .Include(c => c.Order)
            .Include(c => c.Equipment).ThenInclude(e => e.Product)
            .Include(c => c.Bills)
            .Include(c => c.PlanChanges)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (connection is null)
        {
            return Missing("This connection does not exist.");
        }

        ViewData["Title"] = connection.AccountId;
        ViewData["Subtitle"] = $"{connection.Customer?.FullName} - {connection.Plan?.Name}.";

        return View(connection);
    }

    /// <summary>Downloads the connection list as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? status = null)
    {
        IQueryable<Connection> query = Db.Connections.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.Vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.AccountId.Contains(term) ||
                                     (c.Customer != null && c.Customer.FullName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConnectionStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        var connections = await query.OrderByDescending(c => c.ActivationDate).ToListAsync();

        var csv = _csv.Build(
            new[] { "Account id", "Order id", "Customer", "Service", "Plan", "Partner", "Status", "Activated", "Monthly charge", "Deposit" },
            connections.Select(c => new object?[]
            {
                c.AccountId,
                c.Order?.OrderId ?? string.Empty,
                c.Customer?.FullName ?? string.Empty,
                c.Plan?.Category?.Name ?? string.Empty,
                c.Plan?.Name ?? string.Empty,
                c.Vendor?.BusinessName ?? string.Empty,
                c.Status.ToString(),
                c.ActivationDate.ToString("yyyy-MM-dd"),
                c.MonthlyCharge,
                c.SecurityDeposit
            }));

        await Audit.LogAsync("Connection.Exported", "Connection", "Connections", UserId, UserName, IpAddress,
            $"The connection list ({connections.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-connections-{DateTime.Today:yyyyMMdd}.csv");
    }
}
