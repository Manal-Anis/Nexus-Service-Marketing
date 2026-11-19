using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>The retail counters of the service partners.</summary>
public class RetailShopsController : AdminControllerBase
{
    public RetailShopsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the retail counters.</summary>
    public async Task<IActionResult> Index(int? vendorId = null, string? search = null)
    {
        var query = Db.RetailShops.AsNoTracking()
            .Include(s => s.Vendor)
            .Include(s => s.City)
            .Include(s => s.Employees)
            .Where(s => !s.IsDeleted);

        if (vendorId.HasValue)
        {
            query = query.Where(s => s.VendorId == vendorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) ||
                                     (s.ContactPerson != null && s.ContactPerson.Contains(term)));
        }

        var shops = await query.OrderBy(s => s.Vendor!.BusinessName).ThenBy(s => s.Name).ToListAsync();

        ViewData["Title"] = "Retail shops";
        ViewData["Subtitle"] = "The counters where the orders are taken and the cash is collected.";
        ViewData["Vendors"] = await Db.Vendors.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.BusinessName).ToListAsync();
        ViewData["CurrentVendorId"] = vendorId;
        ViewData["CurrentSearch"] = search;

        return View(shops);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new RetailShopFormViewModel());

        ViewData["Title"] = "Register a retail shop";
        ViewData["Subtitle"] = "The counter that belongs to a service partner.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RetailShopFormViewModel model)
    {
        await PopulateFormAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a retail shop";
            return View(model);
        }

        var shop = new RetailShop
        {
            Name = model.Name.Trim(),
            VendorId = model.VendorId,
            ContactPerson = model.ContactPerson?.Trim(),
            Phone = model.Phone?.Trim(),
            Address = model.Address?.Trim(),
            CityId = model.CityId,
            IsActive = model.IsActive
        };

        Db.RetailShops.Add(shop);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("RetailShop.Created", "RetailShop", shop.Id.ToString(), UserId, UserName, IpAddress,
            $"The retail shop {shop.Name} was registered.");

        TempData["Success"] = $"The retail shop {shop.Name} has been registered.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var shop = await Db.RetailShops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (shop is null)
        {
            return Missing("This retail shop does not exist.");
        }

        var model = await BuildFormModelAsync(new RetailShopFormViewModel
        {
            Id = shop.Id,
            Name = shop.Name,
            VendorId = shop.VendorId,
            ContactPerson = shop.ContactPerson,
            Phone = shop.Phone,
            Address = shop.Address,
            CityId = shop.CityId,
            IsActive = shop.IsActive
        });

        ViewData["Title"] = $"Edit {shop.Name}";
        ViewData["Subtitle"] = "The master data of the retail counter.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RetailShopFormViewModel model)
    {
        await PopulateFormAsync(model);

        var shop = await Db.RetailShops.FirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted);

        if (shop is null)
        {
            return Missing("This retail shop does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {shop.Name}";
            return View(model);
        }

        shop.Name = model.Name.Trim();
        shop.VendorId = model.VendorId;
        shop.ContactPerson = model.ContactPerson?.Trim();
        shop.Phone = model.Phone?.Trim();
        shop.Address = model.Address?.Trim();
        shop.CityId = model.CityId;
        shop.IsActive = model.IsActive;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("RetailShop.Updated", "RetailShop", shop.Id.ToString(), UserId, UserName, IpAddress,
            $"The retail shop {shop.Name} was edited.");

        TempData["Success"] = $"{shop.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<RetailShopFormViewModel> BuildFormModelAsync(RetailShopFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(RetailShopFormViewModel model)
    {
        model.Vendors = await Db.Vendors.AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.BusinessName)
            .ToListAsync();

        model.Cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
    }
}
