using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The catalogue of the service plans. The administrator owns the official plans and approves or
/// rejects the plans that a partner submits.
/// </summary>
public class PlansController : AdminControllerBase
{
    public PlansController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the plans of the catalogue.</summary>
    public async Task<IActionResult> Index(string? category = null, string? approval = null, string? search = null)
    {
        var query = Db.Plans.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ServiceCategoryCode>(category, true, out var code))
        {
            query = query.Where(p => p.Category.Code == code.ToString());
        }

        if (!string.IsNullOrWhiteSpace(approval) && Enum.TryParse<PlanApprovalStatus>(approval, true, out var parsed))
        {
            query = query.Where(p => p.ApprovalStatus == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        var plans = await query
            .OrderBy(p => p.Category.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        ViewData["Title"] = "Plans";
        ViewData["Subtitle"] = "The official catalogue and the plans submitted by the partners.";
        ViewData["Categories"] = await Db.ServiceCategories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync();
        ViewData["CurrentCategory"] = category;
        ViewData["CurrentApproval"] = approval;
        ViewData["CurrentSearch"] = search;

        return View(plans);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new PlanFormViewModel());

        ViewData["Title"] = "Register a plan";
        ViewData["Subtitle"] = "The plan is created in the approved state.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanFormViewModel model)
    {
        await PopulateFormAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a plan";
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
            Description = model.Description?.Trim(),
            Features = model.Features?.Trim(),
            IsActive = model.IsActive,
            ApprovalStatus = PlanApprovalStatus.Approved,
            ApprovalRemarks = "Added to the official catalogue by the administrator."
        };

        Db.Plans.Add(plan);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Created", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan {plan.Name} was added to the catalogue.");

        TempData["Success"] = $"The plan {plan.Name} has been added to the catalogue.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await Db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan is null)
        {
            return Missing("This plan does not exist.");
        }

        var model = await BuildFormModelAsync(new PlanFormViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            CategoryId = plan.CategoryId,
            Kind = plan.Kind,
            Hours = plan.Hours,
            SpeedKbps = plan.SpeedKbps,
            ValidityDays = plan.ValidityDays,
            BillingPeriod = plan.BillingPeriod,
            Price = plan.Price,
            LocalRate = plan.LocalRate,
            StdRate = plan.StdRate,
            MobileMsgRate = plan.MobileMsgRate,
            Description = plan.Description,
            Features = plan.Features,
            IsActive = plan.IsActive
        });

        ViewData["Title"] = $"Edit {plan.Name}";
        ViewData["Subtitle"] = "The master data of the plan.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PlanFormViewModel model)
    {
        await PopulateFormAsync(model);

        var plan = await Db.Plans.FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);

        if (plan is null)
        {
            return Missing("This plan does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {plan.Name}";
            return View(model);
        }

        plan.Name = model.Name.Trim();
        plan.CategoryId = model.CategoryId;
        plan.Kind = model.Kind;
        plan.Hours = model.Hours;
        plan.SpeedKbps = model.SpeedKbps;
        plan.ValidityDays = model.ValidityDays;
        plan.BillingPeriod = model.BillingPeriod;
        plan.Price = model.Price;
        plan.LocalRate = model.LocalRate;
        plan.StdRate = model.StdRate;
        plan.MobileMsgRate = model.MobileMsgRate;
        plan.Description = model.Description?.Trim();
        plan.Features = model.Features?.Trim();
        plan.IsActive = model.IsActive;
        plan.UpdatedAt = DateTime.Now;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Updated", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan {plan.Name} was edited.");

        TempData["Success"] = $"{plan.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Approves a plan that a partner has submitted.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var plan = await Db.Plans.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan is null)
        {
            return Missing("This plan does not exist.");
        }

        plan.ApprovalStatus = PlanApprovalStatus.Approved;
        plan.ApprovalRemarks = "Approved by the administrator.";
        plan.IsActive = true;
        plan.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Approved", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan {plan.Name} of {plan.Vendor?.BusinessName ?? "a partner"} was approved.");

        TempData["Success"] = $"The plan {plan.Name} is now part of the catalogue.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Rejects a plan that a partner has submitted.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var plan = await Db.Plans.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan is null)
        {
            return Missing("This plan does not exist.");
        }

        plan.ApprovalStatus = PlanApprovalStatus.Rejected;
        plan.ApprovalRemarks = string.IsNullOrWhiteSpace(reason) ? "Rejected by the administrator." : reason.Trim();
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Rejected", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan {plan.Name} was rejected. {plan.ApprovalRemarks}");

        TempData["Success"] = $"The plan {plan.Name} has been rejected.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Switches a plan on or off in the catalogue.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var plan = await Db.Plans.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan is null)
        {
            return Missing("This plan does not exist.");
        }

        plan.IsActive = !plan.IsActive;
        plan.UpdatedAt = DateTime.Now;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Plan.Toggled", "Plan", plan.Id.ToString(), UserId, UserName, IpAddress,
            $"The plan {plan.Name} is now {(plan.IsActive ? "active" : "inactive")}.");

        TempData["Success"] = $"{plan.Name} is now {(plan.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PlanFormViewModel> BuildFormModelAsync(PlanFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(PlanFormViewModel model)
    {
        model.Categories = await Db.ServiceCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }
}
