using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>
/// The services of the partner. A partner may submit its own plan - the administrator approves it
/// before it appears in the catalogue of the storefront.
/// </summary>
public class ServicesController : VendorControllerBase
{
    public ServicesController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the plans of the partner and the official Nexus plans.</summary>
    public async Task<IActionResult> Index()
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var own = await Db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.VendorId == vendorId.Value && !p.IsDeleted)
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        var official = await Db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.VendorId == null && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        ViewData["Title"] = "My services";
        ViewData["Subtitle"] = "Your own plans and the official Nexus catalogue. A submitted plan is published after the approval of the administrator.";
        ViewData["OwnPlans"] = own;
        ViewData["OfficialPlans"] = official;

        return View();
    }

    /// <summary>Opens the form that submits a new plan to the administrator.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.IsManager(type));
        if (denied is not null)
        {
            return denied;
        }

        var model = new VendorPlanFormViewModel
        {
            Categories = await Db.ServiceCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync()
        };

        ViewData["Title"] = "Submit a plan";
        ViewData["Subtitle"] = "The administrator reviews every plan before it appears on the storefront.";

        return View(model);
    }

    /// <summary>Stores the submitted plan with the state "pending review".</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorPlanFormViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.IsManager(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        model.Categories = await Db.ServiceCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Submit a plan";
            return View(model);
        }

        var plan = new Plan
        {
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId,
            Kind = model.Kind,
            Hours = model.Hours,
            SpeedKbps = model.SpeedKbps,
            ValidityDays = model.ValidityDays,
            BillingPeriod = model.BillingPeriod,
            Price = model.Price,
            LocalRate = model.LocalRate,
            StdRate = model.StdRate,
            MobileMsgRate = model.MobileMsgRate,
            Description = model.Description,
            Features = model.Features,
            Icon = "bi-briefcase",
            IsActive = true,
            ApprovalStatus = PlanApprovalStatus.Pending,
            VendorId = vendorId.Value,
            CreatedAt = DateTime.Now
        };

        Db.Plans.Add(plan);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Submitted", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan '{plan.Name}' was submitted for the approval of the administrator.");

        TempData["Success"] = $"The plan '{plan.Name}' was submitted. The administrator reviews it before it is published.";
        return RedirectToAction(nameof(Index));
    }
}
