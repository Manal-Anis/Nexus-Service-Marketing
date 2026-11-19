using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Controllers;

/// <summary>Public pages of the Nexus website.</summary>
public class HomeController : Controller
{
    private readonly CatalogService _catalog;
    private readonly SupportService _support;

    public HomeController(CatalogService catalog, SupportService support)
    {
        _catalog = catalog;
        _support = support;
    }

    /// <summary>Home page with the hero, the catalogue preview and the trust signals.</summary>
    public async Task<IActionResult> Index()
    {
        var counters = await _catalog.GetPublicCountersAsync();

        var model = new HomeViewModel
        {
            Categories = await _catalog.GetActiveCategoriesAsync(),
            FeaturedPlans = await _catalog.GetFeaturedPlansAsync(6),
            Vendors = await _catalog.GetApprovedVendorsAsync(),
            Testimonials = await _catalog.GetTestimonialsAsync(6),
            CustomerCount = counters.Customers,
            ConnectionCount = counters.Connections,
            VendorCount = counters.Vendors,
            CityCount = counters.Cities
        };

        return View(model);
    }

    /// <summary>The searchable plan catalogue.</summary>
    public async Task<IActionResult> Plans(string? search, string? category, string? period, decimal? minPrice, decimal? maxPrice)
    {
        var model = new PlansViewModel
        {
            Categories = await _catalog.GetActiveCategoriesAsync(),
            Search = search,
            CategoryCode = category,
            BillingPeriod = period,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            BillingPeriods = Enum.GetValues<BillingPeriod>().ToList()
        };

        var plans = await _catalog.GetActivePlansAsync(category, search);

        if (!string.IsNullOrWhiteSpace(period) && Enum.TryParse<BillingPeriod>(period, true, out var parsedPeriod))
        {
            plans = plans.Where(p => p.BillingPeriod == parsedPeriod).ToList();
        }

        if (minPrice.HasValue)
        {
            plans = plans.Where(p => p.Price >= minPrice.Value).ToList();
        }

        if (maxPrice.HasValue)
        {
            plans = plans.Where(p => p.Price <= maxPrice.Value).ToList();
        }

        model.Plans = plans;
        ViewData["MetaDescription"] = "Browse the Nexus dial-up, broadband and landline plans with transparent pricing and cash on delivery billing.";

        return View(model);
    }

    /// <summary>Details of a single plan.</summary>
    public async Task<IActionResult> PlanDetails(int id)
    {
        var plan = await _catalog.GetPlanAsync(id);
        if (plan is null)
        {
            return RedirectToAction(nameof(Plans));
        }

        ViewData["MetaDescription"] = plan.Description;
        return View(plan);
    }

    /// <summary>Public order tracking.</summary>
    public IActionResult TrackOrder() => View(new TrackOrderViewModel());

    /// <summary>Looks up the order and shows its timeline.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackOrder(TrackOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Searched = true;
            return View(model);
        }

        model.Searched = true;
        model.Order = await _catalog.GetOrderForTrackingAsync(model.OrderId);

        if (model.Order is not null)
        {
            model.Order.StatusHistory = model.Order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .ToList();
        }

        return View(model);
    }

    /// <summary>Public connection check.</summary>
    public IActionResult CheckConnection() => View(new CheckConnectionViewModel());

    /// <summary>Looks up the connection and shows the non sensitive information.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckConnection(CheckConnectionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Searched = true;
            return View(model);
        }

        model.Searched = true;
        model.Connection = await _catalog.GetConnectionForCheckAsync(model.AccountId);
        return View(model);
    }

    /// <summary>About page of the company.</summary>
    public IActionResult About() => View();

    /// <summary>Contact page with the message form.</summary>
    public IActionResult Contact() => View(new ContactViewModel());

    /// <summary>Stores a message of the contact form.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _support.SaveContactMessageAsync(new ContactMessage
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Subject = model.Subject,
            Message = model.Message
        });

        TempData["Success"] = "Thank you, your message has reached our support desk.";
        return RedirectToAction(nameof(Contact));
    }

    /// <summary>Privacy policy.</summary>
    public IActionResult Privacy() => View();

    /// <summary>Terms of service.</summary>
    public IActionResult Terms() => View();

    /// <summary>Friendly error page for the 404 and 500 status codes.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int id)
    {
        Response.StatusCode = id is 404 or 500 ? id : 404;
        ViewData["StatusCode"] = id;
        ViewData["Title"] = id == 404 ? "Page not found" : "Something went wrong";

        return View();
    }

    /// <summary>Standard error view kept for the developer exception page.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
