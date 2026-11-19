using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Controllers;

/// <summary>
/// Shared base of the dashboard controllers. It carries the database context, the profile of the
/// signed in user and the audit trail so that every area controller stays thin.
/// </summary>
[Authorize]
public abstract class BaseAreaController : Controller
{
    /// <summary>Database context of the area.</summary>
    protected ApplicationDbContext Db { get; }

    /// <summary>Profile of the signed in user (vendor id, customer id, employee type).</summary>
    protected CurrentUserService CurrentUser { get; }

    /// <summary>Audit trail of the sensitive actions.</summary>
    protected AuditService Audit { get; }

    protected BaseAreaController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
    {
        Db = db;
        CurrentUser = currentUser;
        Audit = audit;
    }

    /// <summary>Identifier of the signed in user.</summary>
    protected string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <summary>Display name of the signed in user.</summary>
    protected string UserName => User.Identity?.Name ?? "Unknown";

    /// <summary>IP address of the request, written to the audit trail.</summary>
    protected string? IpAddress => CurrentUser.GetIpAddress();
}
