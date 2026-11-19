using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Read only queries used by the public website and the shared lookups.</summary>
public class CatalogService
{
    private readonly ApplicationDbContext _db;

    public CatalogService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Service categories that can be ordered online.</summary>
    public Task<List<ServiceCategory>> GetActiveCategoriesAsync() =>
        _db.ServiceCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

    /// <summary>All approved and active plans, optionally filtered by category.</summary>
    public async Task<List<Plan>> GetActivePlansAsync(string? categoryCode = null, string? search = null)
    {
        var query = _db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ApprovalStatus == PlanApprovalStatus.Approved);

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            query = query.Where(p => p.Category.Code == categoryCode);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        return await query
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();
    }

    /// <summary>A single plan with its category, used by the plan details page.</summary>
    public Task<Plan?> GetPlanAsync(int id) =>
        _db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.ApprovalStatus == PlanApprovalStatus.Approved);

    /// <summary>The plans shown in the featured tabs of the home page.</summary>
    public async Task<List<Plan>> GetFeaturedPlansAsync(int take = 6) =>
        await _db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ApprovalStatus == PlanApprovalStatus.Approved)
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .Take(take)
            .ToListAsync();

    /// <summary>Approved vendors that are shown in the showcase.</summary>
    public Task<List<Vendor>> GetApprovedVendorsAsync() =>
        _db.Vendors.AsNoTracking()
            .Include(v => v.City)
            .Where(v => v.Status == VendorStatus.Approved && !v.IsDeleted)
            .OrderBy(v => v.BusinessName)
            .ToListAsync();

    /// <summary>Approved testimonials of the home page.</summary>
    public Task<List<Feedback>> GetTestimonialsAsync(int take = 6) =>
        _db.Feedback.AsNoTracking()
            .Include(f => f.Customer)
            .Where(f => f.IsApproved && !f.IsDeleted && f.Rating >= 4)
            .OrderByDescending(f => f.CreatedAt)
            .Take(take)
            .ToListAsync();

    /// <summary>Active cities, used by every city drop down.</summary>
    public Task<List<City>> GetActiveCitiesAsync() =>
        _db.Cities.AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

    /// <summary>Looks up an order for the public order tracking page.</summary>
    public Task<Order?> GetOrderForTrackingAsync(string orderId) =>
        _db.Orders.AsNoTracking()
            .Include(o => o.StatusHistory)
            .Include(o => o.Plan)
            .Include(o => o.FeasibilityChecks)
            .FirstOrDefaultAsync(o => o.OrderId == orderId.Trim().ToUpperInvariant());

    /// <summary>Looks up a connection for the public connection check page.</summary>
    public Task<Connection?> GetConnectionForCheckAsync(string accountId) =>
        _db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.Customer)
            .Include(c => c.City)
            .FirstOrDefaultAsync(c => c.AccountId == accountId.Trim().ToUpperInvariant());

    /// <summary>Counts of the numbers shown on the home page.</summary>
    public async Task<(int Customers, int Connections, int Vendors, int Cities)> GetPublicCountersAsync()
    {
        var customers = await _db.Customers.CountAsync(c => !c.IsDeleted);
        var connections = await _db.Connections.CountAsync();
        var vendors = await _db.Vendors.CountAsync(v => v.Status == VendorStatus.Approved && !v.IsDeleted);
        var cities = await _db.Cities.CountAsync(c => c.IsActive && !c.IsDeleted);
        return (customers, connections, vendors, cities);
    }
}
