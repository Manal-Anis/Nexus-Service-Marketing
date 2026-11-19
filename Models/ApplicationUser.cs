using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NexusServiceMarketingSystem.Models;

/// <summary>
/// Application user of the system. It extends the ASP.NET Core Identity user with the
/// extra profile fields required by the specification (full name, phone and activation flag).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Full name shown across every dashboard and on generated documents.</summary>
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Primary contact number of the user.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Soft deactivate flag - deactivated users cannot sign in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Free form street address (used by customers for installation).</summary>
    [MaxLength(400)]
    public string? Address { get; set; }

    /// <summary>City the user belongs to.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city master record.</summary>
    public City? City { get; set; }

    /// <summary>Profile picture uploaded by the user (stored outside wwwroot).</summary>
    [MaxLength(300)]
    public string? ProfileImagePath { get; set; }
}
