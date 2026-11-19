using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>Every customer of the network with the orders and the connections.</summary>
public class CustomersController : AdminControllerBase
{
    private readonly CsvExportService _csv;

    public CustomersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Lists the customers.</summary>
    public async Task<IActionResult> Index(string? search = null, string? city = null, int page = 1)
    {
        const int pageSize = 25;

        var query = Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .Include(c => c.Orders)
            .Include(c => c.Connections)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) ||
                                     (c.Email != null && c.Email.Contains(term)) ||
                                     (c.Phone != null && c.Phone.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City != null && c.City.Name == city);
        }

        var total = await query.CountAsync();

        var customers = await query
            .OrderBy(c => c.FullName)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Customers";
        ViewData["Subtitle"] = "Every customer of the network with the orders and the connections.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentCity"] = city;
        ViewData["Cities"] = await Db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewData["TotalRows"] = total;
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(customers);
    }

    /// <summary>Shows the profile of one customer.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var customer = await Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .Include(c => c.Orders).ThenInclude(o => o.Plan)
            .Include(c => c.Connections).ThenInclude(x => x.Plan)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (customer is null)
        {
            return Missing("This customer does not exist.");
        }

        ViewData["Title"] = customer.FullName;
        ViewData["Subtitle"] = $"{customer.Orders.Count} order(s) - {customer.Connections.Count} connection(s).";

        return View(customer);
    }

    /// <summary>Downloads the customer list as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? city = null)
    {
        var query = Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .Include(c => c.Orders)
            .Include(c => c.Connections)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) ||
                                     (c.Email != null && c.Email.Contains(term)) ||
                                     (c.Phone != null && c.Phone.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City != null && c.City.Name == city);
        }

        var customers = await query.OrderBy(c => c.FullName).ToListAsync();

        var csv = _csv.Build(
            new[] { "Customer", "Email", "Contact", "City", "Orders", "Connections", "Registered" },
            customers.Select(c => new object?[]
            {
                c.FullName,
                c.Email ?? string.Empty,
                c.Phone ?? string.Empty,
                c.City?.Name ?? string.Empty,
                c.Orders.Count,
                c.Connections.Count,
                c.CreatedAt.ToString("yyyy-MM-dd")
            }));

        await Audit.LogAsync("Customer.Exported", "Customer", "Customers", UserId, UserName, IpAddress,
            $"The customer list ({customers.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-customers-{DateTime.Today:yyyyMMdd}.csv");
    }
}
