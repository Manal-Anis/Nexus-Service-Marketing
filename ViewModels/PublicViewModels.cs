using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>Data of the public home page.</summary>
public class HomeViewModel
{
    public List<ServiceCategory> Categories { get; set; } = new();
    public List<Plan> FeaturedPlans { get; set; } = new();
    public List<Vendor> Vendors { get; set; } = new();
    public List<Feedback> Testimonials { get; set; } = new();
    public int CustomerCount { get; set; }
    public int ConnectionCount { get; set; }
    public int VendorCount { get; set; }
    public int CityCount { get; set; }
}

/// <summary>Filter and result of the public plan catalogue.</summary>
public class PlansViewModel
{
    public List<ServiceCategory> Categories { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public string? Search { get; set; }
    public string? CategoryCode { get; set; }
    public string? BillingPeriod { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<BillingPeriod> BillingPeriods { get; set; } = new();
}

/// <summary>Order tracking form.</summary>
public class TrackOrderViewModel
{
    [Required(ErrorMessage = "Please enter the order id.")]
    [Display(Name = "Order ID")]
    [RegularExpression("^[A-Za-z][0-9]{10}$", ErrorMessage = "An order id has one letter followed by ten digits, for example D0000000001.")]
    public string OrderId { get; set; } = string.Empty;

    public Order? Order { get; set; }
    public bool Searched { get; set; }
}

/// <summary>Connection check form.</summary>
public class CheckConnectionViewModel
{
    [Required(ErrorMessage = "Please enter the account id.")]
    [Display(Name = "Account ID")]
    [RegularExpression("^[A-Za-z][0-9]{15}$", ErrorMessage = "An account id has one letter, the three digit city code and twelve digits, for example B001000000000001.")]
    public string AccountId { get; set; } = string.Empty;

    public Connection? Connection { get; set; }
    public bool Searched { get; set; }
}

/// <summary>Contact form of the public website.</summary>
public class ContactViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(25)]
    public string? Phone { get; set; }

    [MaxLength(250)]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Please write your message.")]
    [MaxLength(4000)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    public bool Sent { get; set; }
}
