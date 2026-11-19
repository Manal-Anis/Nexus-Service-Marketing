using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

// ---------------------------------------------------------------------------------------------
// MASTER DATA: cities, vendors, shops, employees, customers, suppliers, products and settings.
// ---------------------------------------------------------------------------------------------

/// <summary>A city together with its unique three digit Nexus city code (used in Account IDs).</summary>
public class City
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>City name, for example "Karachi".</summary>
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique three digit numeric city code, for example 001 for Karachi.</summary>
    [Required, MaxLength(3)]
    public string Code3 { get; set; } = string.Empty;

    /// <summary>Soft delete marker so the code history is preserved.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Only active cities can be selected on the forms.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>City in which the customer lives.</summary>
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

/// <summary>Partner seller / retail outlet of Nexus that lists and sells services.</summary>
public class Vendor
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Login account of the vendor owner (created after admin approval).</summary>
    public string? UserId { get; set; }

    /// <summary>Navigation to the owner account.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>Registered business name of the vendor.</summary>
    [Required, MaxLength(150)]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Name of the proprietor / owner person.</summary>
    [MaxLength(120)]
    public string? OwnerName { get; set; }

    /// <summary>Contact number of the vendor office.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Contact email of the vendor office.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Office address.</summary>
    [MaxLength(400)]
    public string? Address { get; set; }

    /// <summary>City of the vendor office.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Approval state maintained by the administrator.</summary>
    public VendorStatus Status { get; set; } = VendorStatus.Pending;

    /// <summary>Reason recorded when the admin rejects or blocks the vendor.</summary>
    [MaxLength(500)]
    public string? StatusRemarks { get; set; }

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the vendor was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Retail shops belonging to this vendor.</summary>
    public ICollection<RetailShop> RetailShops { get; set; } = new List<RetailShop>();

    /// <summary>Staff members assigned to this vendor.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>Plans submitted by this vendor.</summary>
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();

    /// <summary>Orders that this partner has to fulfil.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

/// <summary>Physical retail counter of a vendor where walk-in customers are served.</summary>
public class RetailShop
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Owning vendor.</summary>
    public int VendorId { get; set; }

    /// <summary>Navigation to the owning vendor.</summary>
    public Vendor Vendor { get; set; } = null!;

    /// <summary>Shop name displayed on bills and orders.</summary>
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Person responsible for the counter.</summary>
    [MaxLength(120)]
    public string? ContactPerson { get; set; }

    /// <summary>Counter contact number.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Full street address of the shop.</summary>
    [MaxLength(400)]
    public string? Address { get; set; }

    /// <summary>City in which the shop is located.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Only active shops can be selected on order forms.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Staff members working in this shop.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

/// <summary>Employee record created and maintained by the administrator.</summary>
public class Employee
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Login account of the employee (created together with the employee record).</summary>
    public string? UserId { get; set; }

    /// <summary>Navigation to the login account.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>Vendor the employee works for.</summary>
    public int? VendorId { get; set; }

    /// <summary>Navigation to the vendor.</summary>
    public Vendor? Vendor { get; set; }

    /// <summary>Retail shop the employee is posted at (retail staff).</summary>
    public int? RetailShopId { get; set; }

    /// <summary>Navigation to the retail shop.</summary>
    public RetailShop? RetailShop { get; set; }

    /// <summary>Determines the dashboard tabs the employee may open.</summary>
    public EmployeeType EmployeeType { get; set; } = EmployeeType.Retail;

    /// <summary>Designation printed on the employee card.</summary>
    [MaxLength(120)]
    public string? Designation { get; set; }

    /// <summary>Date the employee joined the organisation.</summary>
    public DateTime JoiningDate { get; set; } = DateTime.Now;

    /// <summary>Direct contact number.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>CNIC / national identity number kept for employee records.</summary>
    [MaxLength(40)]
    public string? NationalId { get; set; }

    /// <summary>Only active employees can sign in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }
}

/// <summary>Customer record. Created automatically on registration or by a vendor for a walk-in buyer.</summary>
public class Customer
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Login account of the customer (null for walk-in customers created by a vendor).</summary>
    public string? UserId { get; set; }

    /// <summary>Navigation to the login account.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>Full name of the customer.</summary>
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Contact number.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Email address.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Installation / home address.</summary>
    [MaxLength(400)]
    public string? Address { get; set; }

    /// <summary>City of residence.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Type of the identity document supplied by the customer.</summary>
    [MaxLength(60)]
    public string? IdProofType { get; set; }

    /// <summary>Number of the supplied identity document.</summary>
    [MaxLength(60)]
    public string? IdProofNumber { get; set; }

    /// <summary>Only active customers can order new services.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the customer record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Orders placed by this customer.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Connections owned by this customer.</summary>
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
}

/// <summary>Supplier of equipment (modems, routers) - named "Supplier" to avoid confusion with sales vendors.</summary>
public class Supplier
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Company name of the supplier.</summary>
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Contact person name.</summary>
    [MaxLength(120)]
    public string? ContactName { get; set; }

    /// <summary>Contact number.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Contact email.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Supplier office address.</summary>
    [MaxLength(400)]
    public string? Address { get; set; }

    /// <summary>City of the supplier.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Only active suppliers can be selected on purchase orders.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the supplier was added.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Products supplied by this vendor.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>Equipment / product master maintained by the administrator.</summary>
public class Product
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Product name, for example "Wi-Fi Router".</summary>
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Equipment type, for example "Router", "Modem", "Adapter".</summary>
    [MaxLength(80)]
    public string? Type { get; set; }

    /// <summary>Unit sale price in Rupees.</summary>
    public decimal Price { get; set; }

    /// <summary>Quantity currently available in the stock room.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Level at which the administrator is warned to reorder.</summary>
    public int ReorderLevel { get; set; }

    /// <summary>Charge applied when the customer damages the equipment.</summary>
    public decimal ReplacementCharge { get; set; }

    /// <summary>Supplier that provides this product.</summary>
    public int? SupplierId { get; set; }

    /// <summary>Navigation to the supplier.</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>Product description shown on the storefront.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Icon class used to render the product on cards.</summary>
    [MaxLength(80)]
    public string? Icon { get; set; }

    /// <summary>Only active products can be assigned to a connection.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the product was added to the master.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>A service category (Dial-Up, Broadband, Landline) with its security deposit.</summary>
public class ServiceCategory
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Category name shown on the storefront.</summary>
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Single character code used as the first character of Order and Account IDs.</summary>
    [Required, MaxLength(1)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Icon class used for the category card.</summary>
    [MaxLength(80)]
    public string? Icon { get; set; }

    /// <summary>Short marketing description.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Refundable security deposit charged per connection (admin editable).</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Display order on the home page.</summary>
    public int SortOrder { get; set; }

    /// <summary>Only active categories appear on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Plans belonging to this category.</summary>
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
}

/// <summary>A service plan offered by Nexus (official) or submitted by a vendor.</summary>
public class Plan
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Plan name, for example "Unlimited 128 Kbps - Quarterly".</summary>
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Service category the plan belongs to.</summary>
    public int CategoryId { get; set; }

    /// <summary>Navigation to the category.</summary>
    public ServiceCategory Category { get; set; } = null!;

    /// <summary>Hourly, Unlimited or Rental.</summary>
    public PlanKind Kind { get; set; }

    /// <summary>Number of hours included (hourly packs only).</summary>
    public int? Hours { get; set; }

    /// <summary>Advertised speed in Kbps.</summary>
    public int? SpeedKbps { get; set; }

    /// <summary>Validity expressed in days (hourly / monthly packs).</summary>
    public int? ValidityDays { get; set; }

    /// <summary>Billing period of the plan.</summary>
    public BillingPeriod BillingPeriod { get; set; }

    /// <summary>Price in Rupees (rental or advance charge).</summary>
    public decimal Price { get; set; }

    /// <summary>Local call rate per minute (landline plans).</summary>
    public decimal? LocalRate { get; set; }

    /// <summary>STD call rate per minute (landline plans).</summary>
    public decimal? StdRate { get; set; }

    /// <summary>Mobile messaging rate per minute (landline plans).</summary>
    public decimal? MobileMsgRate { get; set; }

    /// <summary>Detailed description shown on the plan details page.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Comma separated list of features.</summary>
    [MaxLength(2000)]
    public string? Features { get; set; }

    /// <summary>Icon class used on the plan card.</summary>
    [MaxLength(80)]
    public string? Icon { get; set; }

    /// <summary>Inactive plans are hidden from the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Admin approval state for vendor submitted plans.</summary>
    public PlanApprovalStatus ApprovalStatus { get; set; } = PlanApprovalStatus.Pending;

    /// <summary>Reason recorded by the admin when a plan is rejected.</summary>
    [MaxLength(500)]
    public string? ApprovalRemarks { get; set; }

    /// <summary>Owning vendor; null means an official Nexus plan.</summary>
    public int? VendorId { get; set; }

    /// <summary>Navigation to the owning vendor.</summary>
    public Vendor? Vendor { get; set; }

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the plan was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Date the plan was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Bulk / corporate discount tier based on the number of connections in one order.</summary>
public class DiscountRule
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Minimum number of connections (inclusive).</summary>
    public int MinConnections { get; set; }

    /// <summary>Maximum number of connections (inclusive). Null means "and above".</summary>
    public int? MaxConnections { get; set; }

    /// <summary>Discount percentage applied to advance payment and security deposit.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Human readable description of the tier.</summary>
    [MaxLength(300)]
    public string? Description { get; set; }

    /// <summary>Only active rules are evaluated.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Key/value application setting maintained by the administrator.</summary>
public class Setting
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Unique setting key, for example "Tax.Percentage".</summary>
    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Setting value stored as text.</summary>
    [MaxLength(400)]
    public string Value { get; set; } = string.Empty;

    /// <summary>Description shown on the settings screen.</summary>
    [MaxLength(400)]
    public string? Description { get; set; }

    /// <summary>Grouping used to arrange the settings screen (Billing, General...).</summary>
    [MaxLength(60)]
    public string? Group { get; set; }
}

/// <summary>
/// Sequence counter used to build the Order ID and the Account ID.
/// The row is locked inside a transaction so that two concurrent requests can never
/// receive the same serial number (requirement section 4).
/// </summary>
public class SequenceCounter
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Unique counter name, for example "ORDER-D" or "ACCOUNT-B".</summary>
    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Letter prefix used when the identifier is formatted.</summary>
    [MaxLength(4)]
    public string? Prefix { get; set; }

    /// <summary>Last serial number handed out by this counter.</summary>
    public long CurrentValue { get; set; }

    /// <summary>Date the counter was last increased.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
