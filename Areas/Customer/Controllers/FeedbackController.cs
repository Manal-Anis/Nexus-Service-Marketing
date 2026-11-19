using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The feedback of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class FeedbackController : BaseAreaController
{
    private readonly SupportService _support;

    public FeedbackController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit, SupportService support)
        : base(db, currentUser, audit)
    {
        _support = support;
    }

    /// <summary>Shows the feedback form and the entries of the customer.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        ViewData["Title"] = "Feedback";
        ViewData["Subtitle"] = "Tell us how the installation and the service went.";

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        return View(new CustomerFeedbackViewModel { Orders = orders });
    }

    /// <summary>Stores the feedback of the customer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CustomerFeedbackViewModel model)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        if (!ModelState.IsValid)
        {
            model.Orders = await Db.Orders.AsNoTracking()
                .Include(o => o.Plan)
                .Where(o => o.CustomerId == customerId && !o.IsDeleted)
                .OrderByDescending(o => o.PlacedAt)
                .ToListAsync();
            return View(model);
        }

        if (model.OrderId.HasValue)
        {
            var owns = await Db.Orders.AnyAsync(o => o.Id == model.OrderId.Value && o.CustomerId == customerId);
            if (!owns)
            {
                ModelState.AddModelError(nameof(model.OrderId), "This order does not belong to your account.");
                model.Orders = await Db.Orders.AsNoTracking()
                    .Include(o => o.Plan)
                    .Where(o => o.CustomerId == customerId && !o.IsDeleted)
                    .OrderByDescending(o => o.PlacedAt)
                    .ToListAsync();
                return View(model);
            }
        }

        await _support.SaveFeedbackAsync(new Feedback
        {
            CustomerId = customerId,
            OrderId = model.OrderId,
            Rating = model.Rating,
            Comments = model.Comments,
            CreatedAt = DateTime.Now,
            IsApproved = false
        });

        await Audit.LogAsync("Feedback.Submitted", "Feedback", customerId.ToString(), profile.UserId, profile.Email, IpAddress,
            $"Feedback with {model.Rating} star(s) submitted.");

        model.Submitted = true;
        TempData["Success"] = "Thank you, your feedback has been sent to the administrator.";
        return RedirectToAction(nameof(Index));
    }
}
