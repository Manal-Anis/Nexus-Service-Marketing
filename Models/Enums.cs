using System.ComponentModel.DataAnnotations;

namespace NexusServiceMarketingSystem.Models;

/// <summary>Order lifecycle states defined in requirement section 5.1.</summary>
public enum OrderStatus
{
    /// <summary>Order received but not yet processed by the vendor.</summary>
    Placed = 0,
    /// <summary>Waiting for the Technical department to record the feasibility result.</summary>
    FeasibilityPending = 1,
    /// <summary>Both required feasibility checks passed.</summary>
    Feasible = 2,
    /// <summary>At least one required feasibility check failed.</summary>
    NotFeasible = 3,
    /// <summary>One or more connections have been provisioned for the order.</summary>
    ConnectionProvided = 4,
    /// <summary>Order was cancelled by the customer or by the vendor manager.</summary>
    Cancelled = 5
}

/// <summary>Result of a single feasibility check.</summary>
public enum FeasibilityResult
{
    Pending = 0,
    Feasible = 1,
    NotFeasible = 2
}

/// <summary>Which part of the service a feasibility check refers to.</summary>
public enum FeasibilityCheckType
{
    /// <summary>Availability of the Nexus landline exchange at the installation address.</summary>
    Landline = 0,
    /// <summary>Availability of the internet server / DSLAM port at the address.</summary>
    Internet = 1
}

/// <summary>
/// Employee type decides which tabs of the Vendor dashboard a staff member may open.
/// (Requirement section 3.2.)
/// </summary>
public enum EmployeeType
{
    /// <summary>Vendor owner / manager - full access inside the vendor dashboard.</summary>
    Manager = 0,
    /// <summary>Retail counter staff - places orders and records counter payments.</summary>
    Retail = 1,
    /// <summary>Technical staff - feasibility, connections and equipment.</summary>
    Technical = 2,
    /// <summary>Accounts staff - billing and payments only.</summary>
    Accounts = 3
}

/// <summary>Vendor registration / approval state.</summary>
public enum VendorStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Blocked = 3
}

/// <summary>Connection service state (requirement section 5.9).</summary>
public enum ConnectionStatus
{
    Active = 0,
    TemporarilyInactive = 1,
    PermanentlyInactive = 2
}

/// <summary>Shape of the service being sold.</summary>
public enum PlanKind
{
    /// <summary>Internet sold as a fixed number of hours.</summary>
    Hourly = 0,
    /// <summary>Unlimited internet / calls for a period.</summary>
    Unlimited = 1,
    /// <summary>Landline rental with per minute call charges.</summary>
    Rental = 2
}

/// <summary>Billing / validity period of a plan.</summary>
public enum BillingPeriod
{
    OneTime = 0,
    Hourly = 1,
    Monthly = 2,
    Quarterly = 3,
    HalfYearly = 4,
    Yearly = 5
}

/// <summary>The three service categories handled by Nexus Communication System.</summary>
public enum ServiceCategoryCode
{
    /// <summary>Dial-Up internet - requires a Nexus landline.</summary>
    DialUp = 0,
    /// <summary>Broadband internet.</summary>
    Broadband = 1,
    /// <summary>Landline (telephone) connection.</summary>
    Landline = 2
}

/// <summary>Bill lifecycle state.</summary>
public enum BillStatus
{
    Paid = 0,
    Due = 1
}

/// <summary>Distinguishes the activation invoice from a periodic bill.</summary>
public enum BillType
{
    /// <summary>Generated once when a connection is created.</summary>
    Activation = 0,
    /// <summary>Generated per connection for every billing period.</summary>
    Periodic = 1
}

/// <summary>Only cash on delivery / pay at counter is supported (requirement section 5.8).</summary>
public enum PaymentMethod
{
    Cash = 0
}

/// <summary>Security deposit ledger state.</summary>
public enum SecurityDepositStatus
{
    Held = 0,
    Refunded = 1
}

/// <summary>Purchase order workflow state.</summary>
public enum PurchaseOrderStatus
{
    Draft = 0,
    Placed = 1,
    Received = 2
}

/// <summary>Payment state used by purchase orders (money owed to the supplier).</summary>
public enum SupplierPaymentStatus
{
    Unpaid = 0,
    Partial = 1,
    Paid = 2
}

/// <summary>State of a customer requested plan change.</summary>
public enum PlanChangeStatus
{
    Pending = 0,
    Applied = 1,
    Rejected = 2
}

/// <summary>State of a submitted vendor plan.</summary>
public enum PlanApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>Simple validation helper used by view models.</summary>
public static class EnumDisplay
{
    /// <summary>Turns "TemporarilyInactive" into "Temporarily Inactive" for display.</summary>
    public static string ToDisplayName(this Enum value)
    {
        var text = value.ToString();
        var result = string.Empty;
        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
            {
                result += ' ';
            }
            result += text[i];
        }
        return result;
    }
}

/// <summary>Result of a form submission that carries validation messages back to the view.</summary>
public class OperationResult
{
    /// <summary>True when the operation completed without errors.</summary>
    public bool Success { get; init; }

    /// <summary>Human readable message shown in a toast notification.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Field level validation errors.</summary>
    public Dictionary<string, string[]> Errors { get; init; } = new();

    /// <summary>Creates a successful result.</summary>
    public static OperationResult Ok(string message) => new() { Success = true, Message = message };

    /// <summary>Creates a failed result carrying validation errors.</summary>
    public static OperationResult Fail(string message, Dictionary<string, string[]>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new Dictionary<string, string[]>() };
}
