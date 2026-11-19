using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.ViewModels;

/// <summary>Small badge model that is rendered by the shared status badge partial.</summary>
public class StatusBadgeViewModel
{
    public string Text { get; set; } = string.Empty;
    public string Kind { get; set; } = "muted";
}

/// <summary>Maps the domain enums to the badge colours of the theme.</summary>
public static class BadgeMapper
{
    public static StatusBadgeViewModel For(OrderStatus status) => status switch
    {
        OrderStatus.Placed => new StatusBadgeViewModel { Text = "Placed", Kind = "info" },
        OrderStatus.FeasibilityPending => new StatusBadgeViewModel { Text = "Feasibility pending", Kind = "warning" },
        OrderStatus.Feasible => new StatusBadgeViewModel { Text = "Feasible", Kind = "success" },
        OrderStatus.NotFeasible => new StatusBadgeViewModel { Text = "Not feasible", Kind = "danger" },
        OrderStatus.ConnectionProvided => new StatusBadgeViewModel { Text = "Connection provided", Kind = "success" },
        OrderStatus.Cancelled => new StatusBadgeViewModel { Text = "Cancelled", Kind = "muted" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(ConnectionStatus status) => status switch
    {
        ConnectionStatus.Active => new StatusBadgeViewModel { Text = "Active", Kind = "success" },
        ConnectionStatus.TemporarilyInactive => new StatusBadgeViewModel { Text = "Temporarily inactive", Kind = "warning" },
        ConnectionStatus.PermanentlyInactive => new StatusBadgeViewModel { Text = "Permanently inactive", Kind = "danger" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(BillStatus status) => status switch
    {
        BillStatus.Paid => new StatusBadgeViewModel { Text = "Paid", Kind = "success" },
        BillStatus.Due => new StatusBadgeViewModel { Text = "Due", Kind = "danger" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(VendorStatus status) => status switch
    {
        VendorStatus.Approved => new StatusBadgeViewModel { Text = "Approved", Kind = "success" },
        VendorStatus.Pending => new StatusBadgeViewModel { Text = "Pending approval", Kind = "warning" },
        VendorStatus.Rejected => new StatusBadgeViewModel { Text = "Rejected", Kind = "danger" },
        VendorStatus.Blocked => new StatusBadgeViewModel { Text = "Blocked", Kind = "danger" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(PlanApprovalStatus status) => status switch
    {
        PlanApprovalStatus.Approved => new StatusBadgeViewModel { Text = "Approved", Kind = "success" },
        PlanApprovalStatus.Pending => new StatusBadgeViewModel { Text = "Pending review", Kind = "warning" },
        PlanApprovalStatus.Rejected => new StatusBadgeViewModel { Text = "Rejected", Kind = "danger" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(SecurityDepositStatus status) => status switch
    {
        SecurityDepositStatus.Held => new StatusBadgeViewModel { Text = "Held", Kind = "info" },
        SecurityDepositStatus.Refunded => new StatusBadgeViewModel { Text = "Refunded", Kind = "success" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => new StatusBadgeViewModel { Text = "Draft", Kind = "muted" },
        PurchaseOrderStatus.Placed => new StatusBadgeViewModel { Text = "Placed", Kind = "info" },
        PurchaseOrderStatus.Received => new StatusBadgeViewModel { Text = "Received", Kind = "success" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(SupplierPaymentStatus status) => status switch
    {
        SupplierPaymentStatus.Unpaid => new StatusBadgeViewModel { Text = "Unpaid", Kind = "danger" },
        SupplierPaymentStatus.Partial => new StatusBadgeViewModel { Text = "Partially paid", Kind = "warning" },
        SupplierPaymentStatus.Paid => new StatusBadgeViewModel { Text = "Paid", Kind = "success" },
        _ => new StatusBadgeViewModel { Text = status.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(FeasibilityResult result) => result switch
    {
        FeasibilityResult.Pending => new StatusBadgeViewModel { Text = "Pending", Kind = "warning" },
        FeasibilityResult.Feasible => new StatusBadgeViewModel { Text = "Feasible", Kind = "success" },
        FeasibilityResult.NotFeasible => new StatusBadgeViewModel { Text = "Not feasible", Kind = "danger" },
        _ => new StatusBadgeViewModel { Text = result.ToString(), Kind = "muted" }
    };

    public static StatusBadgeViewModel For(EmployeeType type) => type switch
    {
        EmployeeType.Manager => new StatusBadgeViewModel { Text = "Manager", Kind = "gold" },
        EmployeeType.Retail => new StatusBadgeViewModel { Text = "Retail", Kind = "info" },
        EmployeeType.Technical => new StatusBadgeViewModel { Text = "Technical", Kind = "success" },
        EmployeeType.Accounts => new StatusBadgeViewModel { Text = "Accounts", Kind = "warning" },
        _ => new StatusBadgeViewModel { Text = type.ToString(), Kind = "muted" }
    };
}
