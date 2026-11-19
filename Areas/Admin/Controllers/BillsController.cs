using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>Every bill of the network with its payment state.</summary>
public class BillsController : AdminControllerBase
{
    private readonly CsvExportService _csv;

    public BillsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Lists the bills with a search and a filter.</summary>
    public async Task<IActionResult> Index(string? search = null, string? status = null, string? type = null, int page = 1)
    {
        const int pageSize = 25;

        // The query keeps the type of the base set so that the filters can be chained on it.
        IQueryable<Bill> query = Db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(b => b.Connection).ThenInclude(c => c!.Plan)
            .Include(b => b.Connection).ThenInclude(c => c!.Vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.BillNumber.Contains(term) ||
                                     (b.Connection != null && b.Connection.AccountId.Contains(term)) ||
                                     (b.Connection != null && b.Connection.Customer != null && b.Connection.Customer.FullName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BillStatus>(status, true, out var parsed))
        {
            query = query.Where(b => b.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<BillType>(type, true, out var parsedType))
        {
            query = query.Where(b => b.BillType == parsedType);
        }

        var total = await query.CountAsync();

        var bills = await query
            .OrderByDescending(b => b.GeneratedOn)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Bills";
        ViewData["Subtitle"] = "Every invoice of the network - activation invoices and periodic bills.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentType"] = type;
        ViewData["TotalRows"] = total;
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(bills);
    }

    /// <summary>Shows one bill with its lines and the receipts.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(b => b.Connection).ThenInclude(c => c!.Plan)
            .Include(b => b.Connection).ThenInclude(c => c!.Vendor)
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill is null)
        {
            return Missing("This bill does not exist.");
        }

        ViewData["Title"] = bill.BillNumber;
        ViewData["Subtitle"] = $"{bill.Connection?.AccountId} - {bill.TotalAmount:N2} Rs.";

        return View(bill);
    }

    /// <summary>Downloads the bill list as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? status = null)
    {
        IQueryable<Bill> query = Db.Bills.AsNoTracking()
            .Include(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(b => b.Connection).ThenInclude(c => c!.Vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => b.BillNumber.Contains(term) ||
                                     (b.Connection != null && b.Connection.AccountId.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BillStatus>(status, true, out var parsed))
        {
            query = query.Where(b => b.Status == parsed);
        }

        var bills = await query.OrderByDescending(b => b.GeneratedOn).ToListAsync();

        var csv = _csv.Build(
            new[] { "Bill number", "Account id", "Partner", "Type", "Period", "Generated on", "Due date", "Sub total", "Tax", "Total", "Paid", "Due", "Status" },
            bills.Select(b => new object?[]
            {
                b.BillNumber,
                b.Connection?.AccountId ?? string.Empty,
                b.Connection?.Vendor?.BusinessName ?? string.Empty,
                b.BillType.ToString(),
                $"{b.PeriodStart:yyyy-MM-dd} to {b.PeriodEnd:yyyy-MM-dd}",
                b.GeneratedOn.ToString("yyyy-MM-dd"),
                b.DueDate.ToString("yyyy-MM-dd"),
                b.SubTotal,
                b.TaxAmount,
                b.TotalAmount,
                b.AmountPaid,
                b.AmountDue,
                b.Status.ToString()
            }));

        await Audit.LogAsync("Bill.Exported", "Bill", "Bills", UserId, UserName, IpAddress,
            $"The bill list ({bills.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-bills-{DateTime.Today:yyyyMMdd}.csv");
    }
}
