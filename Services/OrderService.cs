using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Values needed to place an order.</summary>
public class PlaceOrderRequest
{
    /// <summary>Customer that owns the order.</summary>
    public int CustomerId { get; init; }

    /// <summary>Vendor that will fulfil the order.</summary>
    public int? VendorId { get; init; }

    /// <summary>Retail shop that captured the order.</summary>
    public int? RetailShopId { get; init; }

    /// <summary>Plan being ordered.</summary>
    public int PlanId { get; init; }

    /// <summary>Number of connections requested (bulk orders).</summary>
    public int Quantity { get; init; } = 1;

    /// <summary>Installation city.</summary>
    public int? CityId { get; init; }

    /// <summary>Installation address.</summary>
    public string? InstallationAddress { get; init; }

    /// <summary>Contact number of the customer.</summary>
    public string? ContactNumber { get; init; }

    /// <summary>Existing Nexus landline account id entered by a Dial-Up customer.</summary>
    public string? ExistingLandlineAccountId { get; init; }

    /// <summary>Equipment the customer selected.</summary>
    public int? EquipmentProductId { get; init; }

    /// <summary>User account that placed the order.</summary>
    public string? PlacedByUserId { get; init; }

    /// <summary>Free form notes.</summary>
    public string? Notes { get; init; }
}

/// <summary>Outcome of a placed order.</summary>
public class PlaceOrderResult
{
    /// <summary>True when the order was stored.</summary>
    public bool Success { get; init; }

    /// <summary>Error message shown to the user when the order could not be stored.</summary>
    public string? Error { get; init; }

    /// <summary>The generated 11 character order number.</summary>
    public string? OrderId { get; init; }

    /// <summary>Total payable amount of the order.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Security deposit after the bulk discount.</summary>
    public decimal SecurityDepositPayable { get; init; }

    /// <summary>Advance payment after the bulk discount.</summary>
    public decimal AdvancePayable { get; init; }

    /// <summary>Service tax calculated on the order.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Discount percentage that was applied.</summary>
    public decimal DiscountPercent { get; init; }
}

/// <summary>
/// Places the orders of the customer dashboard and of the vendor walk-in counter and keeps the
/// order lifecycle (status history and feasibility checks) up to date.
/// </summary>
public class OrderService
{
    /// <summary>Database context used to store the orders.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Service that hands out the serial numbers of the order identifiers.</summary>
    private readonly SequenceService _sequence;

    /// <summary>Discount calculator.</summary>
    private readonly DiscountService _discount;

    /// <summary>Service that reads the editable application settings.</summary>
    private readonly SettingsService _settings;

    /// <summary>Feasibility rule helper.</summary>
    private readonly FeasibilityService _feasibility;

    /// <summary>Creates the service with the injected dependencies.</summary>
    public OrderService(ApplicationDbContext db, SequenceService sequence, DiscountService discount,
        SettingsService settings, FeasibilityService feasibility)
    {
        _db = db;
        _sequence = sequence;
        _discount = discount;
        _settings = settings;
        _feasibility = feasibility;
    }

    /// <summary>
    /// Creates an order: generates the order number, calculates the discount, the tax and the
    /// totals and opens the required feasibility checks.
    /// </summary>
    public async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        // ------------------------------------------------------------------ load the master data
        var plan = await _db.Plans
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted && p.IsActive, cancellationToken);

        if (plan == null)
        {
            return new PlaceOrderResult { Success = false, Error = "The selected plan is not available." };
        }

        if (plan.ApprovalStatus != PlanApprovalStatus.Approved)
        {
            return new PlaceOrderResult { Success = false, Error = "The selected plan has not been approved yet." };
        }

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && !c.IsDeleted, cancellationToken);

        if (customer == null)
        {
            return new PlaceOrderResult { Success = false, Error = "The customer record could not be found." };
        }

        var quantity = Math.Max(1, request.Quantity);
        var typeLetter = plan.Category.Code.ToUpperInvariant();

        // ------------------------------------------------- validate the existing landline account
        var existingLandline = request.ExistingLandlineAccountId?.Trim();
        var requiresNewLandline = false;

        if (typeLetter == "D")
        {
            if (!string.IsNullOrWhiteSpace(existingLandline))
            {
                // The account must exist and must be active, otherwise the customer needs a landline.
                var landline = await _db.Connections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AccountId == existingLandline, cancellationToken);

                if (landline == null || landline.Status != ConnectionStatus.Active)
                {
                    return new PlaceOrderResult
                    {
                        Success = false,
                        Error = "The entered Nexus landline account id is not valid or the connection is not active."
                    };
                }
            }
            else
            {
                // No landline supplied - the order has to carry a landline feasibility check as well.
                requiresNewLandline = true;
            }
        }

        // ------------------------------------------------------- calculate the money of the order
        var rules = await _db.DiscountRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(cancellationToken);
        var taxPercent = await _settings.GetTaxPercentageAsync(cancellationToken);

        var grossDeposit = plan.Category.SecurityDeposit * quantity;
        var grossAdvance = plan.Price * quantity;

        var breakdown = _discount.Calculate(grossDeposit, grossAdvance, quantity, rules);

        var equipmentPrice = 0m;
        if (request.EquipmentProductId.HasValue)
        {
            var equipment = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.EquipmentProductId.Value && !p.IsDeleted, cancellationToken);

            if (equipment != null)
            {
                equipmentPrice = equipment.Price * quantity;
            }
        }

        // The tax is calculated on the discounted deposit, the discounted advance and the equipment.
        var taxable = breakdown.NetSecurityDeposit + breakdown.NetAdvance + equipmentPrice;
        var taxAmount = Math.Round(taxable * (taxPercent / 100m), 2);
        var total = Math.Round(taxable + taxAmount, 2);

        // ------------------------------------------------------ generate the order number (11 chars)
        var serial = await _sequence.NextOrderSerialAsync(typeLetter[0], cancellationToken);
        var orderId = CodeGenerator.FormatOrderId(typeLetter[0], serial);

        // ------------------------------------------------------------------- store the new order
        var order = new Order
        {
            OrderId = orderId,
            TypeLetter = typeLetter,
            CustomerId = customer.Id,
            VendorId = request.VendorId,
            RetailShopId = request.RetailShopId,
            PlanId = plan.Id,
            Quantity = quantity,
            CityId = request.CityId ?? customer.CityId,
            InstallationAddress = request.InstallationAddress ?? customer.Address,
            ContactNumber = request.ContactNumber ?? customer.Phone,
            ExistingLandlineAccountId = string.IsNullOrWhiteSpace(existingLandline) ? null : existingLandline,
            RequiresLandline = requiresNewLandline,
            Status = OrderStatus.Placed,
            FeasibilityStatus = FeasibilityResult.Pending,
            DiscountPercent = breakdown.DiscountPercent,
            SecurityDepositAmount = grossDeposit,
            SecurityDepositPayable = breakdown.NetSecurityDeposit,
            AdvanceAmount = grossAdvance,
            AdvancePayable = breakdown.NetAdvance,
            EquipmentAmount = equipmentPrice,
            TaxPercent = taxPercent,
            TaxAmount = taxAmount,
            TotalAmount = total,
            EquipmentProductId = request.EquipmentProductId,
            PlacedByUserId = request.PlacedByUserId,
            PlacedAt = DateTime.Now,
            Notes = request.Notes
        };

        _db.Orders.Add(order);

        // The timeline always starts with the "Placed" entry.
        order.StatusHistory.Add(new OrderStatusHistory
        {
            ToStatus = OrderStatus.Placed,
            ChangedByUserId = request.PlacedByUserId,
            ChangedAt = DateTime.Now,
            Remark = "Order received by Nexus Communication System."
        });

        // Open one feasibility check per required check (a Dial-Up order can need two).
        foreach (var requirement in _feasibility.GetRequirements(typeLetter, requiresNewLandline))
        {
            order.FeasibilityChecks.Add(new OrderFeasibilityCheck
            {
                CheckType = requirement.CheckType,
                Result = FeasibilityResult.Pending,
                Remarks = requirement.Instructions
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult
        {
            Success = true,
            OrderId = orderId,
            TotalAmount = total,
            SecurityDepositPayable = breakdown.NetSecurityDeposit,
            AdvancePayable = breakdown.NetAdvance,
            TaxAmount = taxAmount,
            DiscountPercent = breakdown.DiscountPercent
        };
    }

    /// <summary>
    /// Changes the status of an order and writes the change into the status timeline.
    /// </summary>
    public async Task ChangeStatusAsync(Order order, OrderStatus newStatus, string? remark, string? userId, string? userName,
        CancellationToken cancellationToken = default)
    {
        var oldStatus = order.Status;

        order.Status = newStatus;
        order.UpdatedAt = DateTime.Now;

        if (newStatus == OrderStatus.ConnectionProvided)
        {
            order.CompletedAt = DateTime.Now;
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = oldStatus,
            ToStatus = newStatus,
            ChangedByUserId = userId,
            ChangedByName = userName,
            ChangedAt = DateTime.Now,
            Remark = remark
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
