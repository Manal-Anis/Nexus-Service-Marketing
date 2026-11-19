using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Profile of the user that is currently signed in.</summary>
public class CurrentUserProfile
{
    /// <summary>Identifier of the signed in user (empty when nobody is signed in).</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Display name of the signed in user.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Email address of the signed in user.</summary>
    public string? Email { get; init; }

    /// <summary>True when the user is an administrator.</summary>
    public bool IsAdmin { get; init; }

    /// <summary>True when the user belongs to the vendor dashboard.</summary>
    public bool IsVendor { get; init; }

    /// <summary>True when the user belongs to the customer dashboard.</summary>
    public bool IsCustomer { get; init; }

    /// <summary>Vendor the signed in staff member belongs to.</summary>
    public int? VendorId { get; init; }

    /// <summary>Retail shop the signed in retail employee is posted at.</summary>
    public int? RetailShopId { get; init; }

    /// <summary>Type of the employee - decides which dashboard tabs are visible.</summary>
    public EmployeeType? EmployeeType { get; init; }

    /// <summary>Customer record of the signed in customer.</summary>
    public int? CustomerId { get; init; }
}

/// <summary>
/// Resolves the profile of the signed in user. Every dashboard uses this service to make sure
/// that a staff member can only reach the data of their own vendor and that a customer can
/// only reach their own records (protection against IDOR).
/// </summary>
public class CurrentUserService
{
    /// <summary>HTTP context of the current request.</summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Database context used to resolve the vendor / employee / customer links.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Cached profile of the current request.</summary>
    private CurrentUserProfile? _profile;

    /// <summary>Creates the service with the injected dependencies.</summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    /// <summary>Returns the profile of the user that is signed in.</summary>
    public async Task<CurrentUserProfile> GetProfileAsync()
    {
        if (_profile != null)
        {
            return _profile;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
        {
            _profile = new CurrentUserProfile();
            return _profile;
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var isAdmin = principal?.IsInRole("Admin") == true;
        var isVendor = principal?.IsInRole("Vendor") == true;
        var isCustomer = principal?.IsInRole("Customer") == true;

        // A vendor employee is linked to a vendor through the employee record.
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted && e.IsActive);

        // The owner of a vendor has the vendor record linked directly to his account.
        var ownedVendor = await _db.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == userId && !v.IsDeleted);

        var vendorId = employee?.VendorId ?? ownedVendor?.Id;

        var employeeType = employee?.EmployeeType;
        if (employeeType == null && ownedVendor != null)
        {
            // The owner of a vendor always behaves like a manager.
            employeeType = EmployeeType.Manager;
        }

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

        _profile = new CurrentUserProfile
        {
            UserId = userId,
            FullName = user?.FullName ?? principal?.FindFirstValue(ClaimTypes.Name) ?? "Guest",
            Email = user?.Email,
            IsAdmin = isAdmin,
            IsVendor = isVendor || vendorId.HasValue,
            IsCustomer = isCustomer || customer != null,
            VendorId = vendorId,
            RetailShopId = employee?.RetailShopId,
            EmployeeType = employeeType,
            CustomerId = customer?.Id
        };

        return _profile;
    }

    /// <summary>Returns the IP address of the current request (stored in the audit trail).</summary>
    public string? GetIpAddress()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
