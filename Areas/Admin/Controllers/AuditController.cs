using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>The audit trail of every sensitive action of the system.</summary>
public class AuditController : AdminControllerBase
{
    private readonly CsvExportService _csv;

    public AuditController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        CsvExportService csv)
        : base(db, currentUser, audit)
    {
        _csv = csv;
    }

    /// <summary>Lists the audit entries with a search and a filter.</summary>
    public async Task<IActionResult> Index(string? search = null, string? action = null, int page = 1)
    {
        const int pageSize = 30;

        var query = Db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => (a.UserName != null && a.UserName.Contains(term)) ||
                                     (a.EntityId != null && a.EntityId.Contains(term)) ||
                                     (a.Details != null && a.Details.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var term = action.Trim();
            query = query.Where(a => a.Action != null && a.Action.StartsWith(term));
        }

        var total = await query.CountAsync();

        var entries = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Audit log";
        ViewData["Subtitle"] = "Every sensitive action of the system with the user who performed it.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentAction"] = action;
        ViewData["TotalRows"] = total;
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(entries);
    }

    /// <summary>Downloads the audit trail as a CSV file.</summary>
    public async Task<IActionResult> Export(string? search = null, string? action = null)
    {
        var query = Db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => (a.UserName != null && a.UserName.Contains(term)) ||
                                     (a.Details != null && a.Details.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var term = action.Trim();
            query = query.Where(a => a.Action != null && a.Action.StartsWith(term));
        }

        var entries = await query.OrderByDescending(a => a.CreatedAt).Take(5000).ToListAsync();

        var csv = _csv.Build(
            new[] { "Date", "User", "Action", "Entity", "Entity id", "Details", "Ip address" },
            entries.Select(a => new object?[]
            {
                a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                a.UserName ?? string.Empty,
                a.Action,
                a.EntityType ?? string.Empty,
                a.EntityId ?? string.Empty,
                a.Details ?? string.Empty,
                a.IpAddress ?? string.Empty
            }));

        await Audit.LogAsync("Audit.Exported", "AuditLog", "AuditLog", UserId, UserName, IpAddress,
            $"The audit trail ({entries.Count} record(s)) was exported.");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"nexus-audit-{DateTime.Today:yyyyMMdd}.csv");
    }
}
