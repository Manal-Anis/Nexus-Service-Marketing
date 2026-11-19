using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

// ---------------------------------------------------------------------------------------------
// CONNECTIONS, EQUIPMENT ASSIGNMENT AND PLAN CHANGE HISTORY.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// A provisioned service. The public Account ID is 1 letter + 3 digit city code +
/// 12 digit serial = 16 characters (for example B001000000000001).
/// </summary>
public class Connection
{
    /// <summary>Surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Public facing account number - 16 characters, unique.</summary>
    [Required, MaxLength(16)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>Order that produced this connection.</summary>
    public int OrderId { get; set; }

    /// <summary>Navigation to the order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Customer who owns the connection.</summary>
    public int CustomerId { get; set; }

    /// <summary>Navigation to the customer.</summary>
    public Customer Customer { get; set; } = null!;

    /// <summary>Vendor responsible for the connection.</summary>
    public int? VendorId { get; set; }

    /// <summary>Navigation to the vendor.</summary>
    public Vendor? Vendor { get; set; }

    /// <summary>Plan currently active on the connection.</summary>
    public int PlanId { get; set; }

    /// <summary>Navigation to the plan.</summary>
    public Plan Plan { get; set; } = null!;

    /// <summary>City in which the connection exists.</summary>
    public int? CityId { get; set; }

    /// <summary>Navigation to the city.</summary>
    public City? City { get; set; }

    /// <summary>Current service state.</summary>
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Active;

    /// <summary>Reason recorded whenever the status was changed.</summary>
    [MaxLength(1000)]
    public string? StatusReason { get; set; }

    /// <summary>Date the status was last changed.</summary>
    public DateTime? StatusChangedAt { get; set; }

    /// <summary>User account that changed the status.</summary>
    [MaxLength(450)]
    public string? StatusChangedByUserId { get; set; }

    /// <summary>Date the service was activated.</summary>
    public DateTime ActivationDate { get; set; } = DateTime.Now;

    /// <summary>Monthly rental / plan charge carried on the connection.</summary>
    public decimal MonthlyCharge { get; set; }

    /// <summary>Security deposit held against the connection.</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Installation address copied from the order.</summary>
    [MaxLength(400)]
    public string? InstallationAddress { get; set; }

    /// <summary>Contact number of the connection owner.</summary>
    [MaxLength(25)]
    public string? ContactNumber { get; set; }

    /// <summary>Date the record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Equipment assigned to this connection.</summary>
    public ICollection<ConnectionEquipment> Equipment { get; set; } = new List<ConnectionEquipment>();

    /// <summary>Bills raised on this connection.</summary>
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();

    /// <summary>Plan change requests raised against this connection.</summary>
    public ICollection<PlanChangeHistory> PlanChanges { get; set; } = new List<PlanChangeHistory>();
}

/// <summary>Equipment issued to a connection (router, modem...).</summary>
public class ConnectionEquipment
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Connection the equipment was issued to.</summary>
    public int ConnectionId { get; set; }

    /// <summary>Navigation to the connection.</summary>
    public Connection Connection { get; set; } = null!;

    /// <summary>Product issued from the master.</summary>
    public int ProductId { get; set; }

    /// <summary>Navigation to the product.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Serial number printed on the device.</summary>
    [MaxLength(80)]
    public string? SerialNumber { get; set; }

    /// <summary>Date the equipment was issued.</summary>
    public DateTime AssignedAt { get; set; } = DateTime.Now;

    /// <summary>User account that issued the equipment.</summary>
    [MaxLength(450)]
    public string? AssignedByUserId { get; set; }

    /// <summary>True when the device was replaced after a damage report.</summary>
    public bool IsReplaced { get; set; }

    /// <summary>Date the device was replaced.</summary>
    public DateTime? ReplacedAt { get; set; }

    /// <summary>Reason recorded for the replacement.</summary>
    [MaxLength(1000)]
    public string? ReplacementReason { get; set; }

    /// <summary>Charge billed for the damaged device.</summary>
    public decimal ReplacementChargeAmount { get; set; }
}

/// <summary>History of plan change requests raised by customers and applied by the technical team.</summary>
public class PlanChangeHistory
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Connection the request belongs to.</summary>
    public int ConnectionId { get; set; }

    /// <summary>Navigation to the connection.</summary>
    public Connection Connection { get; set; } = null!;

    /// <summary>Plan currently active when the request was raised.</summary>
    public int? OldPlanId { get; set; }

    /// <summary>Navigation to the previous plan.</summary>
    public Plan? OldPlan { get; set; }

    /// <summary>Plan the customer wants to move to.</summary>
    public int? NewPlanId { get; set; }

    /// <summary>Navigation to the requested plan.</summary>
    public Plan? NewPlan { get; set; }

    /// <summary>Reason given by the customer.</summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>State of the request.</summary>
    public PlanChangeStatus Status { get; set; } = PlanChangeStatus.Pending;

    /// <summary>User account that raised the request.</summary>
    [MaxLength(450)]
    public string? RequestedByUserId { get; set; }

    /// <summary>Display name of the requester.</summary>
    [MaxLength(150)]
    public string? RequestedByName { get; set; }

    /// <summary>Date the request was raised.</summary>
    public DateTime RequestedAt { get; set; } = DateTime.Now;

    /// <summary>User account that applied or rejected the request.</summary>
    [MaxLength(450)]
    public string? AppliedByUserId { get; set; }

    /// <summary>Date the request was applied or rejected.</summary>
    public DateTime? AppliedAt { get; set; }
}
