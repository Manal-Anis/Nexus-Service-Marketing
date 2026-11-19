using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// Shared base of the administrator screens. Only the administrators of Nexus Communication
/// System may open them.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public abstract class AdminControllerBase : BaseAreaController
{
    protected AdminControllerBase(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Redirects the visitor to the administrator dashboard when the record is missing.</summary>
    protected IActionResult Missing(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
