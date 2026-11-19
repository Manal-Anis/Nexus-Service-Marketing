using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

// ---------------------------------------------------------------------------------------------
// ORDERING: order header, status history, feasibility checks and uploaded documents.
// ---------------------------------------------------------------------------------------------

/// <summary>Order header. The public Order ID is 1 letter + 10 digits (for example D0000000001).</summary>
public class Order
{
    /// <summary>Surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Public facing order number - 11 characters, unique.</summary>
    [Required, MaxLength(11)]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Connection type letter: D = Dial-Up, T = Telephone/Landline, B = Broadband.</summary>
    [Required, MaxLength(1)]
    public string TypeLetter { get; set; } = string.Empty;

    /// <summary>Customer who placed the order.</summary>
    public int CustomerId { get; set; }

    /// <summary>Navigation to the customer.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Vendor that will fulfil the order.</summary>
    public int? VendorId { get; set; }

    /// <summary>Navigation to the vendor.</summary>
    public Vendor? Vendor { get; set; }

    /// <summary>Retail shop that captured the order (walk-in orders).</summary>
    public int? RetailShopId { get; set; }

    /// <summary>Navigation to the retail shop.</summary>
    public RetailShop? RetailShop { get; set; }

    /// <summary>Plan that was ordered.</summary>
    public int PlanId { get; set; }

    /// <summary>Navigation to the plan.</summary>
    public Plan Plan { get; set; } = null!;

    /// <summary>Number of connections requested (corporate bulk orders).</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Installation city.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Installation address where the connection is required.</summary>
    [MaxLength(400)]
    public string? InstallationAddress { get; set; }

    /// <summary>Contact number given at the time of applying.</summary>
    [MaxLength(25)]
    public string? ContactNumber { get; set; }

    /// <summary>
    /// Existing Nexus landline Account ID entered by a Dial-Up customer who already owns a
    /// landline. When this value is empty the Dial-Up order also requires a landline check.
    /// </summary>
    [MaxLength(16)]
    public string? ExistingLandlineAccountId { get; set; }

    /// <summary>True when the Dial-Up customer also needs a new landline connection.</summary>
    public bool RequiresLandline { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    /// <summary>Aggregate feasibility result of the order.</summary>
    public FeasibilityResult FeasibilityStatus { get; set; } = FeasibilityResult.Pending;

    /// <summary>Summary remark recorded by the technical department.</summary>
    [MaxLength(2000)]
    public string? FeasibilityRemarks { get; set; }

    /// <summary>Discount percentage applied because of the connection quantity.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Undiscounted security deposit total for the whole order.</summary>
    public decimal SecurityDepositAmount { get; set; }

    /// <summary>Discounted security deposit actually payable.</summary>
    public decimal SecurityDepositPayable { get; set; }

    /// <summary>Undiscounted advance (first period plan charge).</summary>
    public decimal AdvanceAmount { get; set; }

    /// <summary>Discounted advance actually payable.</summary>
    public decimal AdvancePayable { get; set; }

    /// <summary>Equipment charge included in the activation invoice.</summary>
    public decimal EquipmentAmount { get; set; }

    /// <summary>Service tax percentage used when the order was placed.</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Tax amount calculated on the activation invoice.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Grand total payable on delivery for this order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Payment method - only "Cash on Delivery / Pay at counter" is supported.</summary>
    [MaxLength(60)]
    public string PaymentMethod { get; set; } = "Cash on Delivery / Pay at counter";

    /// <summary>Equipment selected by the customer (product master id).</summary>
    public int? EquipmentProductId { get; set; }

    /// <summary>Navigation to the selected equipment.</summary>
    public Product? EquipmentProduct { get; set; }

    /// <summary>User account that placed the order (customer or retail staff).</summary>
    [MaxLength(450)]
    public string? PlacedByUserId { get; set; }

    /// <summary>Date and time the order was placed.</summary>
    public DateTime PlacedAt { get; set; } = DateTime.Now;

    /// <summary>Date the order reached the "Connection Provided" state.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Reason recorded when the order was cancelled.</summary>
    [MaxLength(500)]
    public string? CancelReason { get; set; }

    /// <summary>Free form notes visible to the vendor staff.</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Date the record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Date the record was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Status timeline shown to the customer.</summary>
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    /// <summary>Individual feasibility checks (a Dial-Up order can have two).</summary>
    public ICollection<OrderFeasibilityCheck> FeasibilityChecks { get; set; } = new List<OrderFeasibilityCheck>();

    /// <summary>Documents uploaded against the order.</summary>
    public ICollection<OrderDocument> Documents { get; set; } = new List<OrderDocument>();

    /// <summary>Connections created from this order.</summary>
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
}

/// <summary>One entry of the order status timeline (who changed the status, when and why).</summary>
public class OrderStatusHistory
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Order the timeline entry belongs to.</summary>
    public int OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Status before the change.</summary>
    public OrderStatus? FromStatus { get; set; }

    /// <summary>Status after the change.</summary>
    public OrderStatus ToStatus { get; set; }

    /// <summary>User account that performed the change.</summary>
    [MaxLength(450)]
    public string? ChangedByUserId { get; set; }

    /// <summary>Display name of the user that performed the change.</summary>
    [MaxLength(150)]
    public string? ChangedByName { get; set; }

    /// <summary>Date and time of the change.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    /// <summary>Remark explaining the change.</summary>
    [MaxLength(1000)]
    public string? Remark { get; set; }
}

/// <summary>A single feasibility check recorded by the technical department.</summary>
public class OrderFeasibilityCheck
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Order the check belongs to.</summary>
    public int OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Whether this check covers the landline or the internet service.</summary>
    public FeasibilityCheckType CheckType { get; set; }

    /// <summary>Result of the check.</summary>
    public FeasibilityResult Result { get; set; } = FeasibilityResult.Pending;

    /// <summary>Distance parameters evaluated (exchange distance, cable length...).</summary>
    [MaxLength(1000)]
    public string? DistanceParameters { get; set; }

    /// <summary>Server / port availability observed at the address.</summary>
    [MaxLength(1000)]
    public string? ServerAvailability { get; set; }

    /// <summary>Company rules applied (documents, pending dues, city coverage).</summary>
    [MaxLength(1000)]
    public string? CompanyRules { get; set; }

    /// <summary>Free form remarks.</summary>
    [MaxLength(2000)]
    public string? Remarks { get; set; }

    /// <summary>User account that performed the check.</summary>
    [MaxLength(450)]
    public string? CheckedByUserId { get; set; }

    /// <summary>Display name of the user that performed the check.</summary>
    [MaxLength(150)]
    public string? CheckedByName { get; set; }

    /// <summary>Date and time the check was performed.</summary>
    public DateTime? CheckedAt { get; set; }
}

/// <summary>Document uploaded by the customer (address proof, application form, receipt).</summary>
public class OrderDocument
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Order the document belongs to.</summary>
    public int? OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order? Order { get; set; }

    /// <summary>Customer who uploaded the document.</summary>
    public int? CustomerId { get; set; }

    /// <summary>Navigation to the customer.</summary>
    public Customer? Customer { get; set; }

    /// <summary>Type of document: Address Proof, Application Form, Receipt, Other.</summary>
    [MaxLength(60)]
    public string DocumentType { get; set; } = "Other";

    /// <summary>Original file name shown to the user.</summary>
    [MaxLength(300)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Randomly generated stored file name (never the original one).</summary>
    [MaxLength(300)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>Relative folder path: Uploads/{Year}/{City}/{OrderId}/</summary>
    [MaxLength(500)]
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>File size in bytes (maximum 2 MB).</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>MIME type detected when the file was uploaded.</summary>
    [MaxLength(120)]
    public string? ContentType { get; set; }

    /// <summary>Date and time of the upload.</summary>
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    /// <summary>User account that uploaded the document.</summary>
    [MaxLength(450)]
    public string? UploadedByUserId { get; set; }
}
