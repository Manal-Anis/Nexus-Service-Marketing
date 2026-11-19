using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The feedback of the customers. Only the approved entries are shown as testimonials on the
/// public home page.
/// </summary>
public class FeedbackController : AdminControllerBase
{
    public FeedbackController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the feedback with a filter on the approval state.</summary>
    public async Task<IActionResult> Index(string? state = null, string? search = null, int page = 1)
    {
        const int pageSize = 25;

        var query = Db.Feedback.AsNoTracking()
            .Include(f => f.Customer)
            .Include(f => f.Order)
            .Where(f => !f.IsDeleted);

        if (string.Equals(state, "pending", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(f => !f.IsApproved);
        }
        else if (string.Equals(state, "approved", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(f => f.IsApproved);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f => (f.Customer != null && f.Customer.FullName.Contains(term)) ||
                                     (f.Comments != null && f.Comments.Contains(term)));
        }

        var total = await query.CountAsync();

        var feedback = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Feedback";
        ViewData["Subtitle"] = "The ratings and the comments of the customers.";
        ViewData["CurrentState"] = state;
        ViewData["CurrentSearch"] = search;
        ViewData["TotalRows"] = total;
        ViewData["PendingCount"] = await Db.Feedback.AsNoTracking().CountAsync(f => !f.IsDeleted && !f.IsApproved);
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(feedback);
    }

    /// <summary>Publishes a feedback entry as a testimonial.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var feedback = await Db.Feedback.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (feedback is null)
        {
            return Missing("This feedback does not exist.");
        }

        feedback.IsApproved = true;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Feedback.Approved", "Feedback", id.ToString(), UserId, UserName, IpAddress,
            $"The feedback of {feedback.Customer?.FullName} was published as a testimonial.");

        TempData["Success"] = "The feedback is now published on the home page.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Hides a feedback entry from the home page.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var feedback = await Db.Feedback.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (feedback is null)
        {
            return Missing("This feedback does not exist.");
        }

        feedback.IsApproved = false;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Feedback.Rejected", "Feedback", id.ToString(), UserId, UserName, IpAddress,
            $"The feedback of {feedback.Customer?.FullName} was hidden from the home page.");

        TempData["Success"] = "The feedback is no longer published.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Publishes the reply of the administrator under the feedback.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, string? response)
    {
        var feedback = await Db.Feedback.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (feedback is null)
        {
            return Missing("This feedback does not exist.");
        }

        feedback.AdminResponse = response?.Trim();
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Feedback.Answered", "Feedback", id.ToString(), UserId, UserName, IpAddress,
            "The administrator answered a feedback entry.");

        TempData["Success"] = "Your reply has been saved.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes a feedback entry that breaks the rules.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var feedback = await Db.Feedback.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (feedback is null)
        {
            return Missing("This feedback does not exist.");
        }

        feedback.IsDeleted = true;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Feedback.Deleted", "Feedback", id.ToString(), UserId, UserName, IpAddress,
            "A feedback entry was deleted.");

        TempData["Success"] = "The feedback has been deleted.";
        return RedirectToAction(nameof(Index));
    }
}
