using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>Advanced search over the records of the service partner.</summary>
public class SearchController : VendorControllerBase
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
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var result = await _search.SearchAsync(filters, "vendor", vendorId);

        ViewData["Title"] = "Advanced search";
        ViewData["Subtitle"] = "Search the orders and connections of your service desk.";

        return View(new VendorSearchViewModel { Filters = filters, Result = result });
    }

    /// <summary>Exports the result of the search as a CSV file.</summary>
    [HttpGet]
    public async Task<IActionResult> Export(AdvancedSearchViewModel filters)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var result = await _search.SearchAsync(filters, "vendor", vendorId);

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
        return File(bytes, "text/csv", "nexus-partner-search.csv");
    }
}
