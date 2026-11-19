using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The employees of the service partners. The administrator is the only one who may create them
/// and he is the one who hands out the login credentials.
/// </summary>
public class EmployeesController : AdminControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeesController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        UserManager<ApplicationUser> userManager)
        : base(db, currentUser, audit)
    {
        _userManager = userManager;
    }

    /// <summary>Lists the employees of the network.</summary>
    public async Task<IActionResult> Index(int? vendorId = null, EmployeeType? type = null, string? search = null)
    {
        var query = Db.Employees.AsNoTracking()
            .Include(e => e.Vendor)
            .Include(e => e.RetailShop)
            .Include(e => e.User)
            .Where(e => !e.IsDeleted);

        if (vendorId.HasValue)
        {
            query = query.Where(e => e.VendorId == vendorId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(e => e.EmployeeType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => (e.User != null && e.User.FullName.Contains(term)) ||
                                     (e.Designation != null && e.Designation.Contains(term)));
        }

        var employees = await query.OrderBy(e => e.EmployeeType).ThenBy(e => e.Id).ToListAsync();

        ViewData["Title"] = "Employees";
        ViewData["Subtitle"] = "The staff of the service partners and their login credentials.";
        ViewData["Vendors"] = await Db.Vendors.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.BusinessName).ToListAsync();
        ViewData["CurrentVendorId"] = vendorId;
        ViewData["CurrentType"] = type;
        ViewData["CurrentSearch"] = search;

        return View(employees);
    }

    /// <summary>Opens the form that registers a new employee.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new EmployeeFormViewModel());

        ViewData["Title"] = "Register an employee";
        ViewData["Subtitle"] = "The employee receives the login of the partner dashboard.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        await PopulateFormAsync(model);

        var email = model.Email.Trim();

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Please enter a password for the employee.");
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register an employee";
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            Phone = model.Phone?.Trim(),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["Title"] = "Register an employee";
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Vendor");

        var employee = new Employee
        {
            UserId = user.Id,
            VendorId = model.VendorId,
            RetailShopId = model.RetailShopId,
            EmployeeType = model.EmployeeType,
            Designation = model.Designation?.Trim(),
            JoiningDate = model.JoiningDate,
            Phone = model.Phone?.Trim(),
            NationalId = model.NationalId?.Trim(),
            IsActive = model.IsActive
        };

        Db.Employees.Add(employee);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Employee.Created", "Employee", employee.Id.ToString(), UserId, UserName, IpAddress,
            $"The employee {model.FullName} ({model.EmployeeType.ToDisplayName()}) was registered.");

        TempData["Success"] = $"{model.FullName} can now sign in with {email}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Opens the form that edits an employee.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await Db.Employees.AsNoTracking()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee is null)
        {
            return Missing("This employee does not exist.");
        }

        var model = await BuildFormModelAsync(new EmployeeFormViewModel
        {
            Id = employee.Id,
            FullName = employee.User?.FullName ?? string.Empty,
            Email = employee.User?.Email ?? string.Empty,
            Phone = employee.Phone ?? employee.User?.Phone,
            VendorId = employee.VendorId,
            RetailShopId = employee.RetailShopId,
            EmployeeType = employee.EmployeeType,
            Designation = employee.Designation,
            NationalId = employee.NationalId,
            JoiningDate = employee.JoiningDate,
            IsActive = employee.IsActive
        });

        ViewData["Title"] = $"Edit {model.FullName}";
        ViewData["Subtitle"] = "The password stays unchanged when the field is left empty.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel model)
    {
        await PopulateFormAsync(model);

        var employee = await Db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == model.Id && !e.IsDeleted);

        if (employee is null)
        {
            return Missing("This employee does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {model.FullName}";
            return View(model);
        }

        employee.VendorId = model.VendorId;
        employee.RetailShopId = model.RetailShopId;
        employee.EmployeeType = model.EmployeeType;
        employee.Designation = model.Designation?.Trim();
        employee.JoiningDate = model.JoiningDate;
        employee.Phone = model.Phone?.Trim();
        employee.NationalId = model.NationalId?.Trim();
        employee.IsActive = model.IsActive;

        if (employee.User is not null)
        {
            employee.User.FullName = model.FullName.Trim();
            employee.User.Phone = model.Phone?.Trim();
            employee.User.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(employee.User);
                var reset = await _userManager.ResetPasswordAsync(employee.User, token, model.Password!);

                if (!reset.Succeeded)
                {
                    foreach (var error in reset.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    ViewData["Title"] = $"Edit {model.FullName}";
                    return View(model);
                }
            }
        }

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Employee.Updated", "Employee", employee.Id.ToString(), UserId, UserName, IpAddress,
            $"The employee {model.FullName} was edited.");

        TempData["Success"] = $"{model.FullName} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deactivates an employee so that he cannot sign in anymore.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var employee = await Db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee is null)
        {
            return Missing("This employee does not exist.");
        }

        employee.IsActive = !employee.IsActive;

        if (employee.User is not null)
        {
            employee.User.IsActive = employee.IsActive;
        }

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Employee.Toggled", "Employee", employee.Id.ToString(), UserId, UserName, IpAddress,
            $"The employee {employee.User?.FullName ?? employee.Id.ToString()} is now {(employee.IsActive ? "active" : "inactive")}.");

        TempData["Success"] = $"The employee is now {(employee.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeeFormViewModel> BuildFormModelAsync(EmployeeFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(EmployeeFormViewModel model)
    {
        model.Vendors = await Db.Vendors.AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.BusinessName)
            .ToListAsync();

        model.Shops = await Db.RetailShops.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
