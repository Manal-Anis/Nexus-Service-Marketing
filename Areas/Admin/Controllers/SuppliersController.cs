using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>The suppliers of the equipment.</summary>
public class SuppliersController : AdminControllerBase
{
    public SuppliersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the suppliers.</summary>
    public async Task<IActionResult> Index(string? search = null)
    {
        var query = Db.Suppliers.AsNoTracking()
            .Include(s => s.City)
            .Include(s => s.Products)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) ||
                                     (s.ContactName != null && s.ContactName.Contains(term)));
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync();

        ViewData["Title"] = "Suppliers";
        ViewData["Subtitle"] = "The partners that deliver the equipment to the store room.";
        ViewData["CurrentSearch"] = search;

        return View(suppliers);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new SupplierFormViewModel());

        ViewData["Title"] = "Register a supplier";
        ViewData["Subtitle"] = "The company that delivers the routers, modems and handsets.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        await PopulateFormAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a supplier";
            return View(model);
        }

        var supplier = new Supplier
        {
            Name = model.Name.Trim(),
            ContactName = model.ContactName?.Trim(),
            Phone = model.Phone?.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim(),
            CityId = model.CityId,
            IsActive = model.IsActive
        };

        Db.Suppliers.Add(supplier);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Supplier.Created", "Supplier", supplier.Id.ToString(), UserId, UserName, IpAddress,
            $"The supplier {supplier.Name} was registered.");

        TempData["Success"] = $"The supplier {supplier.Name} has been registered.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await Db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (supplier is null)
        {
            return Missing("This supplier does not exist.");
        }

        var model = await BuildFormModelAsync(new SupplierFormViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            CityId = supplier.CityId,
            IsActive = supplier.IsActive
        });

        ViewData["Title"] = $"Edit {supplier.Name}";
        ViewData["Subtitle"] = "The master data of the supplier.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierFormViewModel model)
    {
        await PopulateFormAsync(model);

        var supplier = await Db.Suppliers.FirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted);

        if (supplier is null)
        {
            return Missing("This supplier does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {supplier.Name}";
            return View(model);
        }

        supplier.Name = model.Name.Trim();
        supplier.ContactName = model.ContactName?.Trim();
        supplier.Phone = model.Phone?.Trim();
        supplier.Email = model.Email?.Trim();
        supplier.Address = model.Address?.Trim();
        supplier.CityId = model.CityId;
        supplier.IsActive = model.IsActive;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Supplier.Updated", "Supplier", supplier.Id.ToString(), UserId, UserName, IpAddress,
            $"The supplier {supplier.Name} was edited.");

        TempData["Success"] = $"{supplier.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SupplierFormViewModel> BuildFormModelAsync(SupplierFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(SupplierFormViewModel model)
    {
        model.Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
    }
}
