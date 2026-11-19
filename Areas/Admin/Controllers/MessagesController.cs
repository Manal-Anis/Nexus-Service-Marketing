using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>The inbox of the public contact form.</summary>
public class MessagesController : AdminControllerBase
{
    public MessagesController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the messages that the customers and the visitors sent.</summary>
    public async Task<IActionResult> Index(string? state = null, int page = 1)
    {
        const int pageSize = 25;

        var query = Db.ContactMessages.AsNoTracking();

        if (string.Equals(state, "unread", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => !m.IsRead);
        }
        else if (string.Equals(state, "read", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.IsRead);
        }

        var total = await query.CountAsync();

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Title"] = "Messages";
        ViewData["Subtitle"] = "The messages that arrived through the contact form of the website.";
        ViewData["CurrentState"] = state;
        ViewData["TotalRows"] = total;
        ViewData["UnreadCount"] = await Db.ContactMessages.AsNoTracking().CountAsync(m => !m.IsRead);
        ViewData["Page"] = Math.Max(1, page);
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(messages);
    }

    /// <summary>Opens one message and marks it as read.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var message = await Db.ContactMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

        if (message is null)
        {
            return Missing("This message does not exist.");
        }

        if (!message.IsRead)
        {
            var tracked = await Db.ContactMessages.FirstAsync(m => m.Id == id);
            tracked.IsRead = true;
            await Db.SaveChangesAsync();
        }

        ViewData["Title"] = message.Subject ?? "Message";
        ViewData["Subtitle"] = $"From {message.Name} - {message.CreatedAt:dd MMM yyyy}.";

        return View(message);
    }

    /// <summary>Marks a message as read or as unread.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(int id)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);

        if (message is null)
        {
            return Missing("This message does not exist.");
        }

        message.IsRead = !message.IsRead;
        await Db.SaveChangesAsync();

        TempData["Success"] = message.IsRead ? "The message is marked as read." : "The message is marked as unread.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes a message.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);

        if (message is null)
        {
            return Missing("This message does not exist.");
        }

        Db.ContactMessages.Remove(message);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Message.Deleted", "ContactMessage", id.ToString(), UserId, UserName, IpAddress,
            "A message of the contact inbox was deleted.");

        TempData["Success"] = "The message has been deleted.";
        return RedirectToAction(nameof(Index));
    }
}
