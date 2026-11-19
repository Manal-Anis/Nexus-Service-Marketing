using System.Text;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>Advanced search over every record of the network.</summary>
public class SearchController : AdminControllerBase
{
    private readonly SearchService _search;
    private readonly CsvExportService _csv;

    public SearchController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        SearchService search, CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _search = search;
        _csv = csv;
    }

    /// <summary>Shows the search form and the result.</summary>
    public async Task<IActionResult> Index(AdvancedSearchViewModel filters)
    {
        var result = await _search.SearchAsync(filters, "admin", null);

        ViewData["Title"] = "Advanced search";
        ViewData["Subtitle"] = "Combine the unique number, the name, the service and the period in one query.";

        return View(new AdminSearchViewModel { Filters = filters, Result = result });
    }

    /// <summary>Exports the result of the search as a CSV file.</summary>
    [HttpGet]
    public async Task<IActionResult> Export(AdvancedSearchViewModel filters)
    {
        var result = await _search.SearchAsync(filters, "admin", null);

        var csv = _csv.Build(
            new[] { "OrderId", "AccountId", "Customer", "Type", "Plan", "Phone", "City", "OrderStatus", "ConnectionStatus", "Amount", "Date" },
            result.Rows.Select(r => new object[]
            {
                r.OrderId, r.AccountId ?? string.Empty, r.CustomerName, r.ServiceType, r.PlanName,
                r.ContactNumber ?? string.Empty, r.CityName ?? string.Empty, r.OrderStatus.ToString(),
                r.ConnectionStatus?.ToString() ?? string.Empty, r.TotalAmount.ToString("0.00"), r.Date.ToString("yyyy-MM-dd")
            }));

        await Audit.LogAsync("Search.Exported", "Order", null, UserId, UserName, IpAddress,
            $"CSV export of {result.Rows.Count} rows.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", "nexus-admin-search.csv");
    }
}
