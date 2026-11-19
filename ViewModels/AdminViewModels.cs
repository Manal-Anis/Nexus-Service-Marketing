using System.ComponentModel.DataAnnotations;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>Keys of the administrator dashboard.</summary>
public class AdminDashboardViewModel
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int FeasibleOrders { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int InactiveConnections { get; set; }
    public int TotalCustomers { get; set; }
    public int ApprovedVendors { get; set; }
    public int PendingVendors { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal Collected { get; set; }
    public decimal Outstanding { get; set; }
    public int OpenTickets { get; set; }
    public int LowStockItems { get; set; }

    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public Dictionary<string, int> ConnectionsByType { get; set; } = new();
    public List<(string Month, decimal Amount)> RevenueTrend { get; set; } = new();
    public List<(string Month, int Count)> OrderTrend { get; set; } = new();
    public List<(string Name, int Count)> TopPlans { get; set; } = new();
    public List<(string City, int Count)> TopCities { get; set; } = new();
}

/// <summary>Form used to register or edit a service partner.</summary>
public class VendorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the business name.")]
    [StringLength(150)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Owner name")]
    public string? OwnerName { get; set; }

    [StringLength(25)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(300)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "State")]
    public VendorStatus Status { get; set; } = VendorStatus.Pending;

    [StringLength(300)]
    [Display(Name = "Remarks")]
    public string? StatusRemarks { get; set; }

    public List<City> Cities { get; set; } = new();
}

/// <summary>Form used to register an employee and to give him the login credentials.</summary>
public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the full name.")]
    [StringLength(120)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the email address of the employee.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(25)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Password")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password needs at least 8 characters.")]
    public string? Password { get; set; }

    [Display(Name = "Vendor")]
    public int? VendorId { get; set; }

    [Display(Name = "Retail shop")]
    public int? RetailShopId { get; set; }

    [Display(Name = "Department")]
    public EmployeeType EmployeeType { get; set; } = EmployeeType.Retail;

    [StringLength(80)]
    [Display(Name = "Designation")]
    public string? Designation { get; set; }

    [StringLength(25)]
    [Display(Name = "National id")]
    public string? NationalId { get; set; }

    [Display(Name = "Joining date")]
    public DateTime JoiningDate { get; set; } = DateTime.Today;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Vendor> Vendors { get; set; } = new();
    public List<RetailShop> Shops { get; set; } = new();
}

/// <summary>Form used to register a retail shop.</summary>
public class RetailShopFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the name of the shop.")]
    [StringLength(150)]
    [Display(Name = "Name of the shop")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Vendor")]
    public int VendorId { get; set; }

    [StringLength(120)]
    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }

    [StringLength(25)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(300)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Vendor> Vendors { get; set; } = new();
    public List<City> Cities { get; set; } = new();
}

/// <summary>Form used to register a city and its three digit code.</summary>
public class CityFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the name of the city.")]
    [StringLength(80)]
    [Display(Name = "City")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the three digit code of the city.")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "The code must have exactly three digits.")]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Three digit code")]
    public string Code3 { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Form used to register a plan of the catalogue.</summary>
public class PlanFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the name of the plan.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Service")]
    public int CategoryId { get; set; }

    [Display(Name = "Shape of the service")]
    public PlanKind Kind { get; set; } = PlanKind.Unlimited;

    [Display(Name = "Hours")]
    public int? Hours { get; set; }

    [Display(Name = "Speed (Kbps)")]
    public int? SpeedKbps { get; set; }

    [Display(Name = "Validity (days)")]
    public int? ValidityDays { get; set; }

    [Display(Name = "Billing period")]
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;

    [Range(0, 1000000, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Range(0, 10000, ErrorMessage = "Please enter a valid rate.")]
    [Display(Name = "Local rate per minute")]
    public decimal? LocalRate { get; set; }

    [Range(0, 10000, ErrorMessage = "Please enter a valid rate.")]
    [Display(Name = "STD rate per minute")]
    public decimal? StdRate { get; set; }

    [Range(0, 10000, ErrorMessage = "Please enter a valid rate.")]
    [Display(Name = "Mobile message rate")]
    public decimal? MobileMsgRate { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "Features")]
    public string? Features { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<ServiceCategory> Categories { get; set; } = new();
}

/// <summary>Form used to register a piece of equipment.</summary>
public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the name of the product.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Type")]
    public string? Type { get; set; }

    [Range(0, 1000000, ErrorMessage = "Please enter a valid price.")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Range(0, 100000, ErrorMessage = "Please enter a valid quantity.")]
    [Display(Name = "Stock")]
    public int StockQuantity { get; set; }

    [Range(0, 100000, ErrorMessage = "Please enter a valid level.")]
    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    [Range(0, 1000000, ErrorMessage = "Please enter a valid amount.")]
    [Display(Name = "Replacement charge")]
    public decimal ReplacementCharge { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Supplier> Suppliers { get; set; } = new();
}

/// <summary>Form used to register a supplier.</summary>
public class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the name of the supplier.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Contact person")]
    public string? ContactName { get; set; }

    [StringLength(25)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(300)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<City> Cities { get; set; } = new();
}

/// <summary>Form used to raise a purchase order.</summary>
public class PurchaseOrderFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Display(Name = "Order date")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Expected date")]
    public DateTime? ExpectedDate { get; set; }

    [Display(Name = "Payment status")]
    public SupplierPaymentStatus PaymentStatus { get; set; } = SupplierPaymentStatus.Unpaid;

    public List<PurchaseOrderItem> Items { get; set; } = new();

    public List<Supplier> Suppliers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}

/// <summary>One line of a purchase order.</summary>
public class PurchaseOrderItemViewModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }
}

/// <summary>The editable settings of the application.</summary>
public class SettingsFormViewModel
{
    [Range(0, 100, ErrorMessage = "The tax percentage must be between 0 and 100.")]
    [Display(Name = "Service tax percentage")]
    public decimal TaxPercent { get; set; } = 12.24m;

    [Required]
    [StringLength(10)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "Rs";

    [Range(1, 365, ErrorMessage = "The overdue days must be between 1 and 365.")]
    [Display(Name = "Overdue days")]
    public int OverdueDays { get; set; } = 30;

    [Range(0, 365, ErrorMessage = "The grace days must be between 0 and 365.")]
    [Display(Name = "Payment grace days")]
    public int PaymentGraceDays { get; set; } = 0;

    [StringLength(25)]
    [Display(Name = "Support phone")]
    public string? SupportPhone { get; set; }

    [EmailAddress]
    [StringLength(150)]
    [Display(Name = "Support email")]
    public string? SupportEmail { get; set; }

    [StringLength(300)]
    [Display(Name = "Company address")]
    public string? CompanyAddress { get; set; }
}

/// <summary>The security deposits of the three service categories.</summary>
public class SecurityDepositFormViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Please enter a valid amount.")]
    [Display(Name = "Security deposit")]
    public decimal SecurityDeposit { get; set; }
}

/// <summary>The bulk discount tiers.</summary>
public class DiscountRuleFormViewModel
{
    public int Id { get; set; }

    [Range(1, 100000, ErrorMessage = "Please enter a valid number of connections.")]
    [Display(Name = "From (connections)")]
    public int MinConnections { get; set; }

    [Display(Name = "To (connections)")]
    public int? MaxConnections { get; set; }

    [Range(0, 100, ErrorMessage = "The discount must be between 0 and 100 percent.")]
    [Display(Name = "Discount percent")]
    public decimal DiscountPercent { get; set; }

    [StringLength(300)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Filters of the administrator record lists.</summary>
public class AdminListViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalRows { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)PageSize);
}

/// <summary>The advanced search page of the administrator.</summary>
public class AdminSearchViewModel
{
    public AdvancedSearchViewModel Filters { get; set; } = new();
    public SearchResultViewModel Result { get; set; } = new();
}

/// <summary>
/// The paging bar of the administrator lists. The route values keep the active filters while the
/// visitor walks through the pages.
/// </summary>
public class PagerViewModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRows { get; set; }

    /// <summary>The filters of the list that are kept while the pages are walked through.</summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    /// <summary>The query key that carries the page number.</summary>
    public string PageRouteKey { get; set; } = "page";
}

/// <summary>One line of the smart reorder suggestion.</summary>
public class ReorderSuggestionViewModel
{
    public Product Product { get; set; } = null!;

    /// <summary>The quantity that should be ordered.</summary>
    public int SuggestedQuantity { get; set; }

    /// <summary>"Out of stock" or "Below the reorder level".</summary>
    public string Severity { get; set; } = string.Empty;
}

/// <summary>The report of the whole network for a selected period.</summary>
public class AdminReportsViewModel
{
    public DateTime From { get; set; } = DateTime.Today.AddMonths(-5);
    public DateTime To { get; set; } = DateTime.Today;

    public int TotalOrders { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal DiscountGiven { get; set; }
    public int ActiveConnections { get; set; }
    public int Customers { get; set; }

    /// <summary>Billed and collected amount per month.</summary>
    public List<(string Month, decimal Billed, decimal Collected)> MonthlyTrend { get; set; } = new();

    /// <summary>Turnover per service partner.</summary>
    public List<(string Partner, int Orders, decimal Amount)> PerPartner { get; set; } = new();

    /// <summary>Turnover per city.</summary>
    public List<(string City, int Orders, decimal Amount)> PerCity { get; set; } = new();

    /// <summary>The most ordered plans.</summary>
    public List<(string Plan, int Count, decimal Amount)> TopPlans { get; set; } = new();
}
