using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Handles the messages and the feedback that arrive from the public website.</summary>
public class SupportService
{
    private readonly ApplicationDbContext _db;

    public SupportService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Stores a message sent through the contact form.</summary>
    public async Task SaveContactMessageAsync(ContactMessage message)
    {
        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    /// <summary>Stores a feedback entry of a customer.</summary>
    public async Task SaveFeedbackAsync(Feedback feedback)
    {
        _db.Feedback.Add(feedback);
        await _db.SaveChangesAsync();
    }

    /// <summary>Feedback of a single customer, newest first.</summary>
    public Task<List<Feedback>> GetCustomerFeedbackAsync(int customerId) =>
        _db.Feedback.AsNoTracking()
            .Where(f => f.CustomerId == customerId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    /// <summary>All feedback entries for the administrator.</summary>
    public async Task<List<Feedback>> GetAllFeedbackAsync(string? search = null, bool? approved = null)
    {
        var query = _db.Feedback.AsNoTracking().Include(f => f.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f => f.Comments != null && f.Comments.Contains(term));
        }

        if (approved.HasValue)
        {
            query = query.Where(f => f.IsApproved == approved.Value);
        }

        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    /// <summary>Approves or rejects a testimonial so it can appear on the home page.</summary>
    public async Task<bool> SetFeedbackApprovalAsync(int id, bool approved, string? adminResponse)
    {
        var feedback = await _db.Feedback.FirstOrDefaultAsync(f => f.Id == id);
        if (feedback is null) return false;

        feedback.IsApproved = approved;
        if (!string.IsNullOrWhiteSpace(adminResponse))
        {
            feedback.AdminResponse = adminResponse;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Deletes a feedback entry.</summary>
    public async Task<bool> DeleteFeedbackAsync(int id)
    {
        var feedback = await _db.Feedback.FirstOrDefaultAsync(f => f.Id == id);
        if (feedback is null) return false;

        feedback.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>All contact messages for the administrator.</summary>
    public Task<List<ContactMessage>> GetContactMessagesAsync(bool? unread = null) =>
        _db.ContactMessages.AsNoTracking()
            .Where(m => !unread.HasValue || m.IsRead == unread.Value)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

    /// <summary>Marks a contact message as read.</summary>
    public async Task<bool> MarkMessageReadAsync(int id, bool isRead)
    {
        var message = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return false;

        message.IsRead = isRead;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Deletes a contact message.</summary>
    public async Task<bool> DeleteMessageAsync(int id)
    {
        var message = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null) return false;

        _db.ContactMessages.Remove(message);
        await _db.SaveChangesAsync();
        return true;
    }
}
