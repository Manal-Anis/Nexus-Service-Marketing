using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>
/// Shared base of the vendor dashboard controllers. It resolves the vendor of the signed in
/// staff member and blocks every screen that the employee type is not allowed to open
/// (requirement section 3.2 - protection against IDOR and privilege escalation).
/// </summary>
[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public abstract class VendorControllerBase : BaseAreaController
{
    protected VendorControllerBase(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>
    /// Every vendor screen is closed while the vendor is not approved (pending, rejected or blocked),
    /// even for a staff member that is still signed in from an earlier session.
    /// </summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var vendor = await GetVendorAsync();

        if (vendor is null || vendor.Status != VendorStatus.Approved)
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["Error"] = vendor is null
                    ? "Your account is not linked to an active service partner."
                    : $"The partner account of {vendor.BusinessName} is {vendor.Status.ToDisplayName().ToLowerInvariant()}. The dashboard is closed until the administrator approves it.";
            }

            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
            return;
        }

        await next();
    }

    /// <summary>
    /// Returns the vendor of the signed in staff member. When the account is not linked to a
    /// vendor the request is refused.
    /// </summary>
    protected async Task<Models.Vendor?> GetVendorAsync()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.VendorId is not int vendorId)
        {
            return null;
        }

        return await Db.Vendors.AsNoTracking()
            .Include(v => v.City)
            .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted);
    }

    /// <summary>Identifier of the vendor of the signed in staff member.</summary>
    protected async Task<int?> GetVendorIdAsync()
    {
        var profile = await CurrentUser.GetProfileAsync();
        return profile.VendorId;
    }

    /// <summary>Employee type of the signed in staff member (null for the owner = manager).</summary>
    protected async Task<EmployeeType?> GetEmployeeTypeAsync()
    {
        var profile = await CurrentUser.GetProfileAsync();
        return profile.EmployeeType;
    }

    /// <summary>
    /// Returns "no access" when the employee type may not open the requested screen.
    /// </summary>
    protected IActionResult? DenyIfNotAllowed(bool allowed)
        => allowed ? null : RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
}
