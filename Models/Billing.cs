using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

// ---------------------------------------------------------------------------------------------
// BILLING: bills, bill lines, cash payments and the security deposit ledger.
// ---------------------------------------------------------------------------------------------

/// <summary>A bill raised against a connection (activation invoice or periodic bill).</summary>
public class Bill
{
    /// <summary>Surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Human readable bill number, for example "INV-000123".</summary>
    [Required, MaxLength(30)]
    public string BillNumber { get; set; } = string.Empty;

    /// <summary>Connection the bill belongs to.</summary>
    public int ConnectionId { get; set; }

    /// <summary>Navigation to the connection.</summary>
    public Connection Connection { get; set; } = null!;

    /// <summary>Order that created the connection (useful for activation invoices).</summary>
    public int? OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order? Order { get; set; }

    /// <summary>Activation invoice or periodic bill.</summary>
    public BillType BillType { get; set; } = BillType.Periodic;

    /// <summary>First day covered by the bill.</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Last day covered by the bill.</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>Base plan charge (rental or plan price) for the period.</summary>
    public decimal PlanCharge { get; set; }

    /// <summary>Total local call charges for the period.</summary>
    public decimal LocalChargeAmount { get; set; }

    /// <summary>Total STD call charges for the period.</summary>
    public decimal StdChargeAmount { get; set; }

    /// <summary>Total mobile messaging charges for the period.</summary>
    public decimal MobileChargeAmount { get; set; }

    /// <summary>Replacement charges for damaged equipment.</summary>
    public decimal ReplacementChargeAmount { get; set; }

    /// <summary>Equipment charge billed on the activation invoice.</summary>
    public decimal EquipmentChargeAmount { get; set; }

    /// <summary>Unpaid amount carried forward from the previous bill.</summary>
    public decimal PreviousDue { get; set; }

    /// <summary>Discount percentage applied to the bill.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Discount amount in Rupees.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Sub total before tax.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Service tax percentage applied.</summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Service tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Grand total payable.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Amount already received against this bill.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Amount still outstanding.</summary>
    public decimal AmountDue { get; set; }

    /// <summary>Payment state of the bill.</summary>
    public BillStatus Status { get; set; } = BillStatus.Due;

    /// <summary>Date the payment should be made by.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>User account that generated the bill.</summary>
    [MaxLength(450)]
    public string? GeneratedByUserId { get; set; }

    /// <summary>Display name of the user that generated the bill.</summary>
    [MaxLength(150)]
    public string? GeneratedByName { get; set; }

    /// <summary>Date and time the bill was generated.</summary>
    public DateTime GeneratedOn { get; set; } = DateTime.Now;

    /// <summary>Remarks printed on the invoice.</summary>
    [MaxLength(1000)]
    public string? Remarks { get; set; }

    /// <summary>Individual charge lines of the bill.</summary>
    public ICollection<BillLine> Lines { get; set; } = new List<BillLine>();

    /// <summary>Payments received against this bill.</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

/// <summary>A single charge line printed on a bill.</summary>
public class BillLine
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Bill the line belongs to.</summary>
    public int BillId { get; set; }

    /// <summary>Navigation to the bill.</summary>
    public Bill Bill { get; set; } = null!;

    /// <summary>Description printed on the invoice.</summary>
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Category of the line: Plan, Local, STD, Mobile, Equipment, Replacement.</summary>
    [MaxLength(60)]
    public string? LineType { get; set; }

    /// <summary>Quantity (hours or minutes) the line represents.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit price applied to the quantity.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Line total.</summary>
    public decimal Amount { get; set; }

    /// <summary>Display order of the line on the invoice.</summary>
    public int SortOrder { get; set; }
}

/// <summary>A cash receipt recorded by the vendor against a bill (cash on delivery only).</summary>
public class Payment
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Bill the payment settles.</summary>
    public int BillId { get; set; }

    /// <summary>Navigation to the bill.</summary>
    public Bill Bill { get; set; } = null!;

    /// <summary>Connection the payment belongs to.</summary>
    public int? ConnectionId { get; set; }

    /// <summary>Navigation to the connection.</summary>
    public Connection? Connection { get; set; }

    /// <summary>Unique receipt number printed on the counter receipt.</summary>
    [Required, MaxLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>Amount received in Rupees.</summary>
    public decimal Amount { get; set; }

    /// <summary>Payment method - always cash.</summary>
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    /// <summary>Date the cash was received.</summary>
    public DateTime PaidOn { get; set; } = DateTime.Now;

    /// <summary>User account that received the cash.</summary>
    [MaxLength(450)]
    public string? ReceivedByUserId { get; set; }

    /// <summary>Display name of the person who received the cash.</summary>
    [MaxLength(150)]
    public string? ReceivedByName { get; set; }

    /// <summary>Remarks entered while recording the receipt.</summary>
    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

/// <summary>Security deposit ledger - holds and refunds the refundable deposit.</summary>
public class SecurityDepositEntry
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Order the deposit was collected against.</summary>
    public int? OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order? Order { get; set; }

    /// <summary>Connection the deposit belongs to.</summary>
    public int? ConnectionId { get; set; }

    /// <summary>Navigation to the connection.</summary>
    public Connection? Connection { get; set; }

    /// <summary>Gross deposit amount before the bulk discount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Discount applied on the deposit.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Net deposit actually held.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Held or Refunded.</summary>
    public SecurityDepositStatus Status { get; set; } = SecurityDepositStatus.Held;

    /// <summary>Date the deposit was collected.</summary>
    public DateTime HeldOn { get; set; } = DateTime.Now;

    /// <summary>Date the deposit was refunded.</summary>
    public DateTime? RefundedOn { get; set; }

    /// <summary>User account that processed the refund.</summary>
    [MaxLength(450)]
    public string? RefundedByUserId { get; set; }

    /// <summary>Remarks recorded on the ledger entry.</summary>
    [MaxLength(1000)]
    public string? Remarks { get; set; }
}
