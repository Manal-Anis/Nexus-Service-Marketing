using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// The advanced search of the system. The same query serves the administrator, the vendor staff
/// and the customer - the scope decides which records a caller is allowed to see.
/// </summary>
public class SearchService
{
    private readonly ApplicationDbContext _db;

    public SearchService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Runs the search inside the given scope.</summary>
    /// <param name="filters">The values that were entered in the search form.</param>
    /// <param name="scope">"admin", "vendor" or "customer".</param>
    /// <param name="scopeId">Vendor id or customer id, null for the administrator.</param>
    public async Task<SearchResultViewModel> SearchAsync(AdvancedSearchViewModel filters, string scope, int? scopeId)
    {
        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .ThenInclude(p => p!.Category)
            .Include(o => o.City)
            .Include(o => o.Connections)
            .Where(o => !o.IsDeleted);

        // ------------------------------------------------------------- scope
        if (scope == "vendor" && scopeId.HasValue)
        {
            query = query.Where(o => o.VendorId == scopeId.Value);
        }
        else if (scope == "customer" && scopeId.HasValue)
        {
            query = query.Where(o => o.CustomerId == scopeId.Value);
        }

        // ------------------------------------------------------------- filters
        if (!string.IsNullOrWhiteSpace(filters.UniqueId))
        {
            var term = filters.UniqueId.Trim().ToUpperInvariant();
            query = query.Where(o => o.OrderId == term ||
                                     o.Connections.Any(c => c.AccountId == term) ||
                                     o.CustomerId.ToString() == term);
        }

        if (!string.IsNullOrWhiteSpace(filters.Name))
        {
            var term = filters.Name.Trim();
            query = query.Where(o => o.Customer != null && o.Customer.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filters.ConnectionType))
        {
            var term = filters.ConnectionType.Trim();
            var letter = ServiceTypeLetter(term);
            if (letter is not null)
            {
                query = query.Where(o => o.TypeLetter == letter);
            }
            else
            {
                query = query.Where(o => o.Plan != null && o.Plan.Category != null && o.Plan.Category.Name.Contains(term));
            }
        }

        if (!string.IsNullOrWhiteSpace(filters.ContactNumber))
        {
            var term = filters.ContactNumber.Trim();
            query = query.Where(o => o.ContactNumber != null && o.ContactNumber.Contains(term));
        }

        if (filters.DateFrom.HasValue)
        {
            var from = filters.DateFrom.Value.Date;
            query = query.Where(o => o.PlacedAt >= from);
        }

        if (filters.DateTo.HasValue)
        {
            var to = filters.DateTo.Value.Date.AddDays(1);
            query = query.Where(o => o.PlacedAt < to);
        }

        var total = await query.CountAsync();

        // ------------------------------------------------------------- sorting
        query = filters.SortBy switch
        {
            "date-asc" => query.OrderBy(o => o.PlacedAt),
            "name" => query.OrderBy(o => o.Customer!.FullName),
            "amount-desc" => query.OrderByDescending(o => o.TotalAmount),
            "amount-asc" => query.OrderBy(o => o.TotalAmount),
            "id-asc" => query.OrderBy(o => o.OrderId),
            "id" => query.OrderBy(o => o.OrderId),
            _ => query.OrderByDescending(o => o.PlacedAt)
        };

        // ------------------------------------------------------------- paging
        var pageSize = filters.PageSize is < 5 or > 200 ? 25 : filters.PageSize;
        var page = filters.Page < 1 ? 1 : filters.Page;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = orders.Select(o => new SearchResultRow
        {
            OrderId = o.OrderId,
            AccountId = o.Connections.FirstOrDefault()?.AccountId,
            CustomerName = o.Customer?.FullName ?? "-",
            ServiceType = ServiceTypeName(o.TypeLetter, o.Plan?.Category?.Name),
            PlanName = o.Plan?.Name ?? "-",
            ContactNumber = o.ContactNumber,
            CityName = o.City?.Name,
            OrderStatus = o.Status,
            ConnectionStatus = o.Connections.FirstOrDefault()?.Status,
            TotalAmount = o.TotalAmount,
            Date = o.PlacedAt
        }).ToList();

        return new SearchResultViewModel
        {
            Rows = rows,
            TotalRows = total,
            Page = page,
            PageSize = pageSize,
            Scope = scope
        };
    }

    /// <summary>Maps the type letter of an order to the readable service name.</summary>
    private static string ServiceTypeName(string typeLetter, string? categoryName)
    {
        var name = typeLetter.ToUpperInvariant() switch
        {
            "D" => "Dial-Up",
            "T" => "Landline",
            "B" => "Broadband",
            _ => typeLetter
        };

        return string.IsNullOrWhiteSpace(categoryName) ? name : $"{name} ({categoryName})";
    }

    /// <summary>Accepts either the type letter or the service name and returns the letter.</summary>
    private static string? ServiceTypeLetter(string term)
    {
        var value = term.Trim().ToUpperInvariant();

        if (value is "D" or "T" or "B")
        {
            return value;
        }

        if (value.Contains("DIAL"))
        {
            return "D";
        }

        if (value.Contains("LAND") || value.Contains("TEL"))
        {
            return "T";
        }

        if (value.Contains("BROAD"))
        {
            return "B";
        }

        return null;
    }
}
