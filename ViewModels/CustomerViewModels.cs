using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>Overview of the customer dashboard.</summary>
public class CustomerDashboardViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<SecurityDepositEntry> Deposits { get; set; } = new();
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public int ActiveConnections { get; set; }
    public int PendingOrders { get; set; }
}

/// <summary>The three step order wizard of the customer.</summary>
public class OrderWizardViewModel
{
    [Range(1, 1000, ErrorMessage = "At least one connection is required.")]
    [Display(Name = "Number of connections")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Please choose the plan.")]
    [Display(Name = "Plan")]
    public int PlanId { get; set; }

    [Required(ErrorMessage = "Please enter the installation address.")]
    [StringLength(400)]
    [Display(Name = "Installation address")]
    public string InstallationAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the contact number.")]
    [StringLength(25)]
    [Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "Existing Nexus landline account id")]
    public string? ExistingLandlineAccountId { get; set; }

    [Display(Name = "I need a new landline line")]
    public bool RequiresLandline { get; set; }

    [Display(Name = "Equipment")]
    public int? EquipmentProductId { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>Plan that has been selected (filled by the controller).</summary>
    public Plan? Plan { get; set; }

    /// <summary>Deposit of the category of the plan.</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Discount band that the quantity earns.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Gross deposit of the order.</summary>
    public decimal GrossDeposit { get; set; }

    /// <summary>Gross advance of the order.</summary>
    public decimal GrossAdvance { get; set; }

    /// <summary>Deposit after the discount.</summary>
    public decimal NetDeposit { get; set; }

    /// <summary>Advance after the discount.</summary>
    public decimal NetAdvance { get; set; }

    /// <summary>Equipment charge of the order.</summary>
    public decimal EquipmentAmount { get; set; }

    /// <summary>Service tax in percent.</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Total amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Order id that was issued after the order was placed.</summary>
    public string? PlacedOrderId { get; set; }

    /// <summary>List of the plans the customer can order.</summary>
    public List<Plan> Plans { get; set; } = new();

    /// <summary>List of the cities.</summary>
    public List<City> Cities { get; set; } = new();

    /// <summary>List of the equipment that can be ordered.</summary>
    public List<Product> Equipment { get; set; } = new();

    /// <summary>Discount bands of the system.</summary>
    public List<DiscountRule> DiscountRules { get; set; } = new();
}

/// <summary>Feedback form of the customer.</summary>
public class CustomerFeedbackViewModel
{
    [Required(ErrorMessage = "Please choose a rating.")]
    [Range(1, 5, ErrorMessage = "Please choose a rating between one and five stars.")]
    [Display(Name = "Rating")]
    public int Rating { get; set; } = 5;

    [MaxLength(2000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Display(Name = "Related order")]
    public int? OrderId { get; set; }

    /// <summary>The orders the feedback can be linked to.</summary>
    public List<Order> Orders { get; set; } = new();

    public bool Submitted { get; set; }
}

/// <summary>Profile form of the customer.</summary>
public class CustomerProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [StringLength(25)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your address.")]
    [StringLength(400)]
    [Display(Name = "Installation address")]
    public string Address { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    public List<City> Cities { get; set; } = new();
}

/// <summary>Upload form of a supporting document.</summary>
public class DocumentUploadViewModel
{
    [Display(Name = "Order")]
    public int? OrderId { get; set; }

    [Display(Name = "Document type")]
    public string DocumentType { get; set; } = "Identity proof";

    [Required(ErrorMessage = "Please choose a file.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    public List<Order> Orders { get; set; } = new();

    /// <summary>The documents the customer has already uploaded.</summary>
    public List<OrderDocument> Documents { get; set; } = new();
}

/// <summary>Receipts and deposit ledger of the customer.</summary>
public class CustomerPaymentsViewModel
{
    public List<Payment> Payments { get; set; } = new();
    public List<SecurityDepositEntry> Deposits { get; set; } = new();
}
