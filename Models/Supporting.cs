using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

// ---------------------------------------------------------------------------------------------
// SUPPORTING RECORDS: feedback, contact messages, audit log and purchase orders.
// ---------------------------------------------------------------------------------------------

/// <summary>Customer feedback with a 1 to 5 star rating.</summary>
public class Feedback
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Customer who left the feedback.</summary>
    public int CustomerId { get; set; }

    /// <summary>Navigation to the customer.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Optional order the feedback relates to.</summary>
    public int? OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order? Order { get; set; }

    /// <summary>Star rating from 1 to 5.</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Free form comments.</summary>
    [MaxLength(2000)]
    public string? Comments { get; set; }

    /// <summary>Date the feedback was submitted.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Only approved testimonials are shown on the home page.</summary>
    public bool IsApproved { get; set; }

    /// <summary>Reply published by the administrator.</summary>
    [MaxLength(1000)]
    public string? AdminResponse { get; set; }

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }
}

/// <summary>Message received through the public contact form.</summary>
public class ContactMessage
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Sender name.</summary>
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sender email.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Sender phone number.</summary>
    [MaxLength(25)]
    public string? Phone { get; set; }

    /// <summary>Subject line.</summary>
    [MaxLength(250)]
    public string? Subject { get; set; }

    /// <summary>Message body.</summary>
    [Required, MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Date the message was received.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>True once the administrator has read the message.</summary>
    public bool IsRead { get; set; }
}

/// <summary>Audit trail of the sensitive actions performed inside the system.</summary>
public class AuditLog
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>User account that performed the action.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>Display name of the user.</summary>
    [MaxLength(150)]
    public string? UserName { get; set; }

    /// <summary>Short action name, for example "Bill.Updated".</summary>
    [MaxLength(120)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity type affected, for example "Bill".</summary>
    [MaxLength(80)]
    public string? EntityType { get; set; }

    /// <summary>Identifier of the affected entity.</summary>
    [MaxLength(80)]
    public string? EntityId { get; set; }

    /// <summary>Additional details about the action.</summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>IP address the request came from.</summary>
    [MaxLength(80)]
    public string? IpAddress { get; set; }

    /// <summary>Date and time of the action.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Purchase order raised by the administrator to restock equipment from a supplier.</summary>
public class PurchaseOrder
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Unique purchase order number.</summary>
    [Required, MaxLength(30)]
    public string PurchaseOrderNumber { get; set; } = string.Empty;

    /// <summary>Supplier the order is placed with.</summary>
    public int SupplierId { get; set; }

    /// <summary>Navigation to the supplier.</summary>
    public Supplier Supplier { get; set; } = null!;

    /// <summary>Date the order was raised.</summary>
    public DateTime OrderDate { get; set; } = DateTime.Now;

    /// <summary>Expected delivery date.</summary>
    public DateTime? ExpectedDate { get; set; }

    /// <summary>Current state: Draft, Placed or Received.</summary>
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>Payment state towards the supplier.</summary>
    public SupplierPaymentStatus PaymentStatus { get; set; } = SupplierPaymentStatus.Unpaid;

    /// <summary>Total value of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Amount already paid to the supplier.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Amount still owed to the supplier.</summary>
    public decimal AmountDue { get; set; }

    /// <summary>Notes printed on the purchase order.</summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>User account that created the order.</summary>
    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    /// <summary>Date the order was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Date the goods were received (stock is increased on this date).</summary>
    public DateTime? ReceivedOn { get; set; }

    /// <summary>Soft delete marker.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Items included in the purchase order.</summary>
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

/// <summary>A single product line of a purchase order.</summary>
public class PurchaseOrderItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Purchase order the line belongs to.</summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>Navigation to the purchase order.</summary>
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>Product being ordered.</summary>
    public int ProductId { get; set; }

    /// <summary>Navigation to the product.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Cost of one unit.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Line total (quantity x unit cost).</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>Simple key/value settings helper exposed to the views.</summary>
public class ErrorViewModel
{
    /// <summary>Identifier of the failed request.</summary>
    public string? RequestId { get; set; }

    /// <summary>True when a request identifier is available.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
