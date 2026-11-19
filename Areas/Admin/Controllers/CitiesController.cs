using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The cities of the network. Every city carries the three digit code that becomes the middle part
/// of the sixteen character account number.
/// </summary>
public class CitiesController : AdminControllerBase
{
    public CitiesController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the cities with their codes.</summary>
    public async Task<IActionResult> Index()
    {
        var cities = await Db.Cities.AsNoTracking()
            .Include(c => c.Customers)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewData["Title"] = "Cities";
        ViewData["Subtitle"] = "The three digit code is part of every account number.";

        return View(cities);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Register a city";
        ViewData["Subtitle"] = "The code needs exactly three digits and has to be unique.";

        return View(new CityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a city";
            return View(model);
        }

        var code = model.Code3.Trim();

        if (await Db.Cities.AnyAsync(c => c.Code3 == code))
        {
            ModelState.AddModelError(nameof(model.Code3), "This three digit code is already used by another city.");
            ViewData["Title"] = "Register a city";
            return View(model);
        }

        var city = new City
        {
            Name = model.Name.Trim(),
            Code3 = code,
            IsActive = model.IsActive
        };

        Db.Cities.Add(city);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("City.Created", "City", city.Id.ToString(), UserId, UserName, IpAddress,
            $"The city {city.Name} was registered with the code {city.Code3}.");

        TempData["Success"] = $"{city.Name} is now available with the code {city.Code3}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var city = await Db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (city is null)
        {
            return Missing("This city does not exist.");
        }

        var model = new CityFormViewModel
        {
            Id = city.Id,
            Name = city.Name,
            Code3 = city.Code3,
            IsActive = city.IsActive
        };

        ViewData["Title"] = $"Edit {city.Name}";
        ViewData["Subtitle"] = "Changing the code changes the account numbers of the future connections.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CityFormViewModel model)
    {
        var city = await Db.Cities.FirstOrDefaultAsync(c => c.Id == model.Id);

        if (city is null)
        {
            return Missing("This city does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {city.Name}";
            return View(model);
        }

        var code = model.Code3.Trim();

        if (await Db.Cities.AnyAsync(c => c.Code3 == code && c.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Code3), "This three digit code is already used by another city.");
            ViewData["Title"] = $"Edit {city.Name}";
            return View(model);
        }

        city.Name = model.Name.Trim();
        city.Code3 = code;
        city.IsActive = model.IsActive;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("City.Updated", "City", city.Id.ToString(), UserId, UserName, IpAddress,
            $"The city {city.Name} was edited.");

        TempData["Success"] = $"{city.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
