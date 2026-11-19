using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The service partners. The administrator approves, rejects or blocks them, and he is the only
/// one who may change the master data of the network.
/// </summary>
public class VendorsController : AdminControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public VendorsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        UserManager<ApplicationUser> userManager)
        : base(db, currentUser, audit)
    {
        _userManager = userManager;
    }

    // ------------------------------------------------------------------ login account handling
    /// <summary>
    /// Moves the vendor to a new state and keeps the login accounts in step with it:
    /// Approved = the owner and the active employees can sign in, every other state locks them out.
    /// A partner without a login account (created by the administrator or registered before the
    /// password field existed) receives one on approval, with a temporary password.
    /// </summary>
    /// <returns>The credential note for the administrator (when an account was created) and an error text.</returns>
    private async Task<(string? Note, string? Error)> ApplyStatusAsync(Models.Vendor vendor, VendorStatus newStatus)
    {
        string? note = null;

        if (newStatus == VendorStatus.Approved)
        {
            (note, var error) = await EnsureOwnerAccountAsync(vendor);
            if (error is not null)
            {
                return (null, error);
            }
        }

        vendor.Status = newStatus;
        await SetAccessAsync(vendor, newStatus == VendorStatus.Approved);
        return (note, null);
    }

    /// <summary>Creates the owner login of a vendor that has none yet.</summary>
    private async Task<(string? Note, string? Error)> EnsureOwnerAccountAsync(Models.Vendor vendor)
    {
        if (!string.IsNullOrEmpty(vendor.UserId))
        {
            // The owner account exists, make sure that it carries the vendor role.
            var owner = await _userManager.FindByIdAsync(vendor.UserId);
            if (owner is not null && !await _userManager.IsInRoleAsync(owner, "Vendor"))
            {
                await _userManager.AddToRoleAsync(owner, "Vendor");
            }

            return (null, null);
        }

        if (string.IsNullOrWhiteSpace(vendor.Email))
        {
            return (null, "This partner has no email address. Enter an email address first, it becomes the login name.");
        }

        var email = vendor.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return (null, $"An account with the email address {email} already exists. Use another email address for the partner.");
        }

        // The temporary password satisfies the password policy (upper, lower, digit, symbol, 8+ characters).
        var password = "Nx@" + RandomNumberGenerator.GetInt32(100000, 999999) + "a";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(vendor.OwnerName) ? vendor.BusinessName : vendor.OwnerName,
            Phone = vendor.Phone,
            IsActive = true
        };

        var created = await _userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return (null, string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, "Vendor");
        vendor.UserId = user.Id;

        return ($"A login was created for {vendor.BusinessName}. Email: {email}   Temporary password: {password}   (give it to the partner, he can change it after the first sign in)", null);
    }

    /// <summary>Unlocks or locks the owner and the employees of a vendor.</summary>
    private async Task SetAccessAsync(Models.Vendor vendor, bool enabled)
    {
        var staff = await Db.Employees
            .Where(e => e.VendorId == vendor.Id && !e.IsDeleted && e.UserId != null)
            .ToListAsync();

        var userIds = staff.Select(e => e.UserId!).ToList();
        if (!string.IsNullOrEmpty(vendor.UserId))
        {
            userIds.Add(vendor.UserId);
        }

        foreach (var id in userIds.Distinct())
        {
            var account = await _userManager.FindByIdAsync(id);
            if (account is null)
            {
                continue;
            }

            // An employee that the administrator switched off stays off when the partner is unblocked.
            var employee = staff.FirstOrDefault(e => e.UserId == id);
            var shouldBeActive = enabled && (id == vendor.UserId || employee?.IsActive == true);

            if (account.IsActive != shouldBeActive)
            {
                account.IsActive = shouldBeActive;
                await _userManager.UpdateAsync(account);
            }
        }
    }

    /// <summary>Lists the partners with their state.</summary>
    public async Task<IActionResult> Index(string? status = null, string? search = null)
    {
        // The query keeps the type of the base set so that the filters can be chained on it.
        IQueryable<Models.Vendor> query = Db.Vendors.AsNoTracking()
            .Include(v => v.City)
            .Include(v => v.RetailShops)
            .Include(v => v.Employees)
            .Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VendorStatus>(status, true, out var parsed))
        {
            query = query.Where(v => v.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(v => v.BusinessName.Contains(term) ||
                                     (v.OwnerName != null && v.OwnerName.Contains(term)) ||
                                     (v.Email != null && v.Email.Contains(term)));
        }

        var vendors = await query.OrderBy(v => v.BusinessName).ToListAsync();

        ViewData["Title"] = "Service partners";
        ViewData["Subtitle"] = "Approve the registrations and control the partners of the network.";
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentSearch"] = search;

        return View(vendors);
    }

    /// <summary>Shows the profile of one partner.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var vendor = await Db.Vendors.AsNoTracking()
            .Include(v => v.City)
            .Include(v => v.RetailShops)
            .Include(v => v.Employees).ThenInclude(e => e.User)
            .Include(v => v.Orders)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        ViewData["Title"] = vendor.BusinessName;
        ViewData["Subtitle"] = $"{vendor.Orders.Count} order(s) - {vendor.RetailShops.Count} retail shop(s).";

        return View(vendor);
    }

    /// <summary>Opens the form that registers a new partner.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new VendorFormViewModel
        {
            Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
        };

        ViewData["Title"] = "Register a partner";
        ViewData["Subtitle"] = "The partner is created in the approved state.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorFormViewModel model)
    {
        model.Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a partner";
            return View(model);
        }

        var vendor = new Models.Vendor
        {
            BusinessName = model.BusinessName.Trim(),
            OwnerName = model.OwnerName?.Trim(),
            Phone = model.Phone?.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim(),
            CityId = model.CityId,
            Status = model.Status,
            StatusRemarks = model.StatusRemarks?.Trim()
        };

        // The login account follows the chosen state (an approved partner needs a login).
        var (note, error) = await ApplyStatusAsync(vendor, model.Status);
        if (error is not null)
        {
            ModelState.AddModelError(nameof(model.Email), error);
            ViewData["Title"] = "Register a partner";
            return View(model);
        }

        Db.Vendors.Add(vendor);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Vendor.Created", "Vendor", vendor.Id.ToString(), UserId, UserName, IpAddress,
            $"The service partner {vendor.BusinessName} was registered.");

        TempData["Success"] = $"The service partner {vendor.BusinessName} has been registered.";
        TempData["Credentials"] = note;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Opens the form that edits a partner.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vendor = await Db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        var model = new VendorFormViewModel
        {
            Id = vendor.Id,
            BusinessName = vendor.BusinessName,
            OwnerName = vendor.OwnerName,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            CityId = vendor.CityId,
            Status = vendor.Status,
            StatusRemarks = vendor.StatusRemarks,
            Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
        };

        ViewData["Title"] = $"Edit {vendor.BusinessName}";
        ViewData["Subtitle"] = "The master data of the service partner.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorFormViewModel model)
    {
        model.Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        var vendor = await Db.Vendors.FirstOrDefaultAsync(v => v.Id == model.Id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {vendor.BusinessName}";
            return View(model);
        }

        vendor.BusinessName = model.BusinessName.Trim();
        vendor.OwnerName = model.OwnerName?.Trim();
        vendor.Phone = model.Phone?.Trim();
        vendor.Email = model.Email?.Trim();
        vendor.Address = model.Address?.Trim();
        vendor.CityId = model.CityId;
        vendor.StatusRemarks = model.StatusRemarks?.Trim();

        // A change of the state also locks or unlocks the login accounts of the partner.
        var (note, error) = await ApplyStatusAsync(vendor, model.Status);
        if (error is not null)
        {
            ModelState.AddModelError(nameof(model.Email), error);
            ViewData["Title"] = $"Edit {vendor.BusinessName}";
            return View(model);
        }

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Vendor.Updated", "Vendor", vendor.Id.ToString(), UserId, UserName, IpAddress,
            $"The service partner {vendor.BusinessName} was edited.");

        TempData["Success"] = $"{vendor.BusinessName} has been updated.";
        TempData["Credentials"] = note;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Approves a pending registration.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var vendor = await Db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        // Approval unlocks the login of the partner (and creates one when it is missing).
        var (note, error) = await ApplyStatusAsync(vendor, VendorStatus.Approved);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id = vendor.Id });
        }

        vendor.StatusRemarks = "Approved by the administrator.";
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Vendor.Approved", "Vendor", vendor.Id.ToString(), UserId, UserName, IpAddress,
            $"The service partner {vendor.BusinessName} was approved.");

        TempData["Success"] = $"{vendor.BusinessName} is now an approved service partner and can sign in.";
        TempData["Credentials"] = note;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Rejects a registration.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var vendor = await Db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        await ApplyStatusAsync(vendor, VendorStatus.Rejected);
        vendor.StatusRemarks = string.IsNullOrWhiteSpace(reason) ? "Rejected by the administrator." : reason.Trim();
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Vendor.Rejected", "Vendor", vendor.Id.ToString(), UserId, UserName, IpAddress,
            $"The service partner {vendor.BusinessName} was rejected. {vendor.StatusRemarks}");

        TempData["Success"] = $"The registration of {vendor.BusinessName} has been rejected.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Blocks or unblocks an approved partner.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(int id)
    {
        var vendor = await Db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vendor is null)
        {
            return Missing("This service partner does not exist.");
        }

        // Blocking also locks the owner and all employees, unblocking opens the accounts again.
        var target = vendor.Status == VendorStatus.Blocked ? VendorStatus.Approved : VendorStatus.Blocked;
        var (_, blockError) = await ApplyStatusAsync(vendor, target);
        if (blockError is not null)
        {
            TempData["Error"] = blockError;
            return RedirectToAction(nameof(Details), new { id = vendor.Id });
        }

        vendor.StatusRemarks = vendor.Status == VendorStatus.Blocked
            ? "Blocked by the administrator."
            : "Unblocked by the administrator.";

        await Db.SaveChangesAsync();

        await Audit.LogAsync(vendor.Status == VendorStatus.Blocked ? "Vendor.Blocked" : "Vendor.Unblocked",
            "Vendor", vendor.Id.ToString(), UserId, UserName, IpAddress,
            $"The service partner {vendor.BusinessName} is now {vendor.Status.ToDisplayName()}.");

        TempData["Success"] = $"{vendor.BusinessName} is now {vendor.Status.ToDisplayName().ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }
}
