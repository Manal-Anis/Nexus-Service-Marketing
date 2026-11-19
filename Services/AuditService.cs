using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// Writes the audit trail of the sensitive actions (logins, status changes, billing, payments,
/// deletions). The audit rows are written through their own scope so they survive a rollback of
/// the business transaction that triggered them.
/// </summary>
public class AuditService
{
    /// <summary>Database context used to store the audit rows.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Creates the service with the injected database context.</summary>
    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Stores one audit entry.</summary>
    /// <param name="action">Short action name, for example "Order.Placed".</param>
    /// <param name="entityType">Type of the affected entity.</param>
    /// <param name="entityId">Identifier of the affected entity.</param>
    /// <param name="userId">User account that performed the action.</param>
    /// <param name="userName">Display name of the user.</param>
    /// <param name="ipAddress">IP address of the request.</param>
    /// <param name="details">Additional details.</param>
    public async Task LogAsync(string action, string? entityType = null, string? entityId = null,
        string? userId = null, string? userName = null, string? ipAddress = null, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            Details = details,
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
    }
}
