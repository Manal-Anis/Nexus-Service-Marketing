using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>
/// Decides which part of the vendor dashboard a staff member may open. The rules follow
/// requirement section 3.2 - only the Accounts staff and the Manager may generate and edit bills.
/// </summary>
public static class VendorPermissions
{
    /// <summary>True for the vendor owner / manager.</summary>
    public static bool IsManager(EmployeeType? type) => type is null or EmployeeType.Manager;

    /// <summary>True when the staff member may open the billing screens.</summary>
    public static bool CanBill(EmployeeType? type) => type is null or EmployeeType.Manager or EmployeeType.Accounts;

    /// <summary>True when the staff member may open the feasibility and connection screens.</summary>
    public static bool CanOperate(EmployeeType? type) => type is null or EmployeeType.Manager or EmployeeType.Technical;

    /// <summary>True when the staff member may record a counter receipt.</summary>
    public static bool CanCollect(EmployeeType? type) =>
        type is null or EmployeeType.Manager or EmployeeType.Accounts or EmployeeType.Retail;

    /// <summary>True when the staff member may create a walk-in order.</summary>
    public static bool CanSell(EmployeeType? type) => type is null or EmployeeType.Manager or EmployeeType.Retail;

    /// <summary>True when the staff member may change the plan of a live connection.</summary>
    public static bool CanChangePlan(EmployeeType? type) => type is null or EmployeeType.Manager or EmployeeType.Technical;

    /// <summary>Human readable label of the employee type.</summary>
    public static string Label(EmployeeType? type) => type switch
    {
        EmployeeType.Manager => "Manager",
        EmployeeType.Retail => "Retail counter",
        EmployeeType.Technical => "Technical",
        EmployeeType.Accounts => "Accounts",
        _ => "Manager"
    };
}

/// <summary>Overview of the vendor dashboard.</summary>
public class VendorDashboardViewModel
{
    public Vendor Vendor { get; set; } = null!;
    public string OwnerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int FeasibleOrders { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int InactiveConnections { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<Order> OrdersAwaitingFeasibility { get; set; } = new();
    public List<Connection> RecentConnections { get; set; } = new();

    /// <summary>Number of orders per lifecycle state (chart).</summary>
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();

    /// <summary>Collected amount per month of the last six months (chart).</summary>
    public List<(string Month, decimal Amount)> CollectionTrend { get; set; } = new();

    /// <summary>Number of connections per service type (chart).</summary>
    public Dictionary<string, int> ConnectionsByType { get; set; } = new();
}

/// <summary>Row of the order table of the vendor dashboard.</summary>
public class VendorOrderListViewModel
{
    public List<Order> Orders { get; set; } = new();
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>Walk-in order form of the retail counter.</summary>
public class VendorOrderFormViewModel
{
    [Required(ErrorMessage = "Please choose the customer.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Please enter the customer name.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Customer name")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Contact number")]
    [StringLength(25)]
    public string? ContactNumber { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Required(ErrorMessage = "Please enter the installation address.")]
    [StringLength(400)]
    [Display(Name = "Installation address")]
    public string InstallationAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the plan.")]
    [Display(Name = "Plan")]
    public int PlanId { get; set; }

    [Range(1, 1000, ErrorMessage = "At least one connection is required.")]
    [Display(Name = "Number of connections")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Retail shop")]
    public int? RetailShopId { get; set; }

    [Display(Name = "Existing Nexus landline account id")]
    [StringLength(16)]
    public string? ExistingLandlineAccountId { get; set; }

    [Display(Name = "I need a new landline line")]
    public bool RequiresLandline { get; set; }

    [Display(Name = "Equipment")]
    public int? EquipmentProductId { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>Order id that was issued after the order was placed.</summary>
    public string? PlacedOrderId { get; set; }

    public List<Customer> Customers { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public List<City> Cities { get; set; } = new();
    public List<Product> Equipment { get; set; } = new();
    public List<RetailShop> Shops { get; set; } = new();
    public List<DiscountRule> DiscountRules { get; set; } = new();

    /// <summary>Service tax percentage taken from the settings.</summary>
    public decimal TaxPercent { get; set; } = 12.24m;
}

/// <summary>Form used by the technical department to record a feasibility check.</summary>
public class FeasibilityCheckViewModel
{
    public int OrderId { get; set; }
    public int CheckId { get; set; }

    [Required(ErrorMessage = "Please record the result of the check.")]
    [Display(Name = "Result")]
    public FeasibilityResult Result { get; set; } = FeasibilityResult.Feasible;

    [Display(Name = "Distance parameters")]
    [StringLength(1000)]
    public string? DistanceParameters { get; set; }

    [Display(Name = "Server availability")]
    [StringLength(1000)]
    public string? ServerAvailability { get; set; }

    [Display(Name = "Company rules")]
    [StringLength(1000)]
    public string? CompanyRules { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    public FeasibilityCheckType CheckType { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>Form used to create a connection from a feasible order.</summary>
public class CreateConnectionViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    /// <summary>Device that will be issued with the connection (empty when the order carries no device).</summary>
    public string EquipmentProductName { get; set; } = string.Empty;

    [Display(Name = "Equipment serial number")]
    [StringLength(80)]
    public string? EquipmentSerialNumber { get; set; }

    [Display(Name = "Notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<Product> Equipment { get; set; } = new();
}

/// <summary>Form used to change the state of a connection.</summary>
public class ConnectionStatusViewModel
{
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "Please choose the new state.")]
    [Display(Name = "New state")]
    public ConnectionStatus Status { get; set; }

    [Required(ErrorMessage = "Please enter the reason.")]
    [StringLength(1000, MinimumLength = 3)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Form used to move a connection to another plan.</summary>
public class PlanChangeViewModel
{
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "Please choose the new plan.")]
    [Display(Name = "New plan")]
    public int NewPlanId { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    public string CurrentPlanName { get; set; } = string.Empty;
    public List<Plan> Plans { get; set; } = new();
}

/// <summary>Form used to report a damaged device.</summary>
public class EquipmentReplacementViewModel
{
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "Please describe the damage.")]
    [StringLength(1000, MinimumLength = 3)]
    [Display(Name = "Reason of the replacement")]
    public string Reason { get; set; } = string.Empty;

    [Range(0, 1000000)]
    [Display(Name = "Replacement charge")]
    public decimal Charge { get; set; }
}

/// <summary>Form used by the accounts department to generate a periodic bill.</summary>
public class GenerateBillViewModel
{
    public int ConnectionId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;

    [Display(Name = "Period start")]
    public DateTime PeriodStart { get; set; } = DateTime.Today.AddMonths(-1);

    [Display(Name = "Period end")]
    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    [Range(0, 1000000)]
    [Display(Name = "Local minutes")]
    public decimal LocalMinutes { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "STD minutes")]
    public decimal StdMinutes { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Mobile messaging minutes")]
    public decimal MobileMinutes { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Hours used")]
    public decimal HoursUsed { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Replacement charge")]
    public decimal ReplacementCharge { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Reason of the replacement")]
    public string? ReplacementReason { get; set; }

    [Range(0, 100)]
    [Display(Name = "Discount percent")]
    public decimal DiscountPercent { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }
}

/// <summary>Form used to record a cash receipt at the counter.</summary>
public class RecordPaymentViewModel
{
    public int BillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal TotalAmount { get; set; }

    [Range(0.01, 10000000, ErrorMessage = "Please enter the amount received.")]
    [Display(Name = "Amount received")]
    public decimal Amount { get; set; }

    [Display(Name = "Date of the receipt")]
    public DateTime PaidOn { get; set; } = DateTime.Today;

    [MaxLength(500)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }
}

/// <summary>Payments screen of the vendor dashboard.</summary>
public class VendorPaymentsViewModel
{
    public List<Bill> Bills { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<SecurityDepositEntry> Deposits { get; set; } = new();
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal DepositsHeld { get; set; }
    public decimal DepositsRefunded { get; set; }
}

/// <summary>Customers of the vendor dashboard.</summary>
public class VendorCustomersViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? Search { get; set; }
    public int TotalCount { get; set; }
    public int ActiveConnections { get; set; }
    public decimal OutstandingAmount { get; set; }
}

/// <summary>Detail screen of one customer of the vendor.</summary>
public class VendorCustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<OrderDocument> Documents { get; set; } = new();
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
}

/// <summary>Plan submitted by the vendor to the administrator.</summary>
public class VendorPlanFormViewModel
{
    [Required(ErrorMessage = "Please enter the plan name.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Plan name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the service category.")]
    [Display(Name = "Service category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please choose the plan type.")]
    [Display(Name = "Plan type")]
    public PlanKind Kind { get; set; } = PlanKind.Unlimited;

    [Display(Name = "Hours included")]
    public int? Hours { get; set; }

    [Display(Name = "Speed in Kbps")]
    public int? SpeedKbps { get; set; }

    [Display(Name = "Validity in days")]
    public int? ValidityDays { get; set; }

    [Required(ErrorMessage = "Please choose the billing period.")]
    [Display(Name = "Billing period")]
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;

    [Range(0, 1000000)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Local rate per minute")]
    public decimal? LocalRate { get; set; }

    [Range(0, 1000)]
    [Display(Name = "STD rate per minute")]
    public decimal? StdRate { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Mobile messaging rate per minute")]
    public decimal? MobileMsgRate { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Features")]
    public string? Features { get; set; }

    public List<ServiceCategory> Categories { get; set; } = new();
    public int? PlanId { get; set; }
}

/// <summary>Equipment and stock of the vendor dashboard.</summary>
public class VendorEquipmentViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<ConnectionEquipment> Assignments { get; set; } = new();
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int TotalUnits { get; set; }
    public int AssignedUnits { get; set; }
}

/// <summary>The search page of the vendor.</summary>
public class VendorSearchViewModel
{
    public AdvancedSearchViewModel Filters { get; set; } = new();
    public SearchResultViewModel Result { get; set; } = new();
}

/// <summary>Reports of the vendor dashboard.</summary>
public class VendorReportsViewModel
{
    public string From { get; set; } = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
    public string To { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    public int TotalOrders { get; set; }
    public int TotalConnections { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }

    public List<(string Category, int Orders, decimal Amount)> OrdersByCategory { get; set; } = new();
    public List<(string Category, int Orders, decimal Amount)> ConnectionsByCategory { get; set; } = new();
    public List<(string Status, int Count)> OrdersByStatus { get; set; } = new();
    public List<(string Month, decimal Collected)> MonthlyCollection { get; set; } = new();

    public List<Order> Orders { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
}
