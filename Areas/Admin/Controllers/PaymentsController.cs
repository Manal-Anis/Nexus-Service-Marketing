using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// Every cash receipt of the network. The cash is always collected at the counter of the partner,
/// the administrator only watches the register.
/// </summary>
public class PaymentsController : AdminControllerBase
{
    private readonly CsvExportService _csv;

    public PaymentsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Lists the receipts with a search and a filter.</summary>
    public async Task<IActionResult> Index(string? search = null, string? from = null, string? to = null, int page = 1)
    {
        const int pageSize = 25;

        // The query keeps the type of the base set so that the filters can be chained on it.
        IQueryable<Payment> query = Db.Payments.AsNoTracking()
            .Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c!.Vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.ReceiptNumber.Contains(term) ||
                                     (p.Bill != null && p.Bill.BillNumber.Contains(term)) ||
                                     (p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.AccountId.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
        {
            query = query.Where(p => p.PaidOn >= fromDate.Date);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
        {
            query = query.Where(p => p.PaidOn < toDate.Date.AddDays(1));
        }

        var total = await query.CountAsync();

        var payments = await query
            .OrderByDescending(p => p.PaidOn)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Payments";
        ViewData["Subtitle"] = "The cash register of the network - cash on delivery only.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentFrom"] = from;
        ViewData["CurrentTo"] = to;
        ViewData["TotalRows"] = total;
        ViewData["TotalCollected"] = await query.SumAsync(p => p.Amount);
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(payments);
    }

    /// <summary>Downloads the receipt list as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? from = null, string? to = null)
    {
        IQueryable<Payment> query = Db.Payments.AsNoTracking()
            .Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c!.Customer)
            .Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c!.Vendor);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.ReceiptNumber.Contains(term) ||
                                     (p.Bill != null && p.Bill.BillNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
        {
            query = query.Where(p => p.PaidOn >= fromDate.Date);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
        {
            query = query.Where(p => p.PaidOn < toDate.Date.AddDays(1));
        }

        var payments = await query.OrderByDescending(p => p.PaidOn).ToListAsync();

        var csv = _csv.Build(
            new[] { "Receipt", "Bill number", "Account id", "Customer", "Partner", "Amount", "Method", "Paid on", "Received by" },
            payments.Select(p => new object?[]
            {
                p.ReceiptNumber,
                p.Bill?.BillNumber ?? string.Empty,
                p.Bill?.Connection?.AccountId ?? string.Empty,
                p.Bill?.Connection?.Customer?.FullName ?? string.Empty,
                p.Bill?.Connection?.Vendor?.BusinessName ?? string.Empty,
                p.Amount,
                p.Method.ToString(),
                p.PaidOn.ToString("yyyy-MM-dd"),
                p.ReceivedByName ?? string.Empty
            }));

        await Audit.LogAsync("Payment.Exported", "Payment", "Payments", UserId, UserName, IpAddress,
            $"The receipt list ({payments.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-payments-{DateTime.Today:yyyyMMdd}.csv");
    }
}
