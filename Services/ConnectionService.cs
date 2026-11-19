using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Options used while a connection is created.</summary>
public class CreateConnectionOptions
{
    /// <summary>Serial number of the issued equipment.</summary>
    public string? EquipmentSerialNumber { get; init; }

    /// <summary>User account that creates the connection.</summary>
    public string? UserId { get; init; }

    /// <summary>Display name of the user that creates the connection.</summary>
    public string? UserName { get; init; }
}

/// <summary>Outcome of a connection creation.</summary>
public class CreateConnectionResult
{
    /// <summary>True when the connection was created.</summary>
    public bool Success { get; init; }

    /// <summary>Error message shown to the user when the connection could not be created.</summary>
    public string? Error { get; init; }

    /// <summary>The generated 16 character account number.</summary>
    public string? AccountId { get; init; }

    /// <summary>Number of the generated activation invoice.</summary>
    public string? BillNumber { get; init; }

    /// <summary>Total amount of the activation invoice.</summary>
    public decimal TotalAmount { get; init; }
}

/// <summary>
/// Creates the connections of a feasible order: assigns the equipment from the stock, generates
/// the 16 character account number and raises the activation invoice.
/// </summary>
public class ConnectionService
{
    /// <summary>Database context used to store the connections.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Service that hands out the serial numbers of the identifiers.</summary>
    private readonly SequenceService _sequence;

    /// <summary>Billing calculator.</summary>
    private readonly BillingService _billing;

    /// <summary>Service that reads the editable application settings.</summary>
    private readonly SettingsService _settings;

    /// <summary>Creates the service with the injected dependencies.</summary>
    public ConnectionService(ApplicationDbContext db, SequenceService sequence, BillingService billing, SettingsService settings)
    {
        _db = db;
        _sequence = sequence;
        _billing = billing;
        _settings = settings;
    }

    /// <summary>
    /// Creates one connection for the given order.
    /// </summary>
    /// <param name="order">Order the connection belongs to. It has to be in the "Feasible" state.</param>
    /// <param name="options">Serial number of the equipment and the user that performs the action.</param>
    public async Task<CreateConnectionResult> CreateConnectionAsync(Order order, CreateConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        // Business rule - a connection can only be created for a feasible order.
        if (order.Status != OrderStatus.Feasible)
        {
            return new CreateConnectionResult
            {
                Success = false,
                Error = "A connection can only be created when the order is feasible."
            };
        }

        var plan = await _db.Plans
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == order.PlanId, cancellationToken);

        if (plan == null)
        {
            return new CreateConnectionResult { Success = false, Error = "The plan of the order could not be found." };
        }

        // ------------------------------------------------------------------ city code of the account id
        var cityCode = "000";
        if (order.CityId.HasValue)
        {
            var city = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == order.CityId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(city?.Code3))
            {
                cityCode = city!.Code3;
            }
        }

        // ------------------------------------------------ generate the 16 character account identifier
        var serial = await _sequence.NextAccountSerialAsync(plan.Category.Code[0], cancellationToken);
        var accountId = CodeGenerator.FormatAccountId(plan.Category.Code[0], cityCode, serial);

        // ------------------------------------------------------------------------- issue the equipment
        Product? equipment = null;
        if (order.EquipmentProductId.HasValue)
        {
            equipment = await _db.Products.FirstOrDefaultAsync(p => p.Id == order.EquipmentProductId.Value, cancellationToken);

            if (equipment == null || equipment.IsDeleted)
            {
                return new CreateConnectionResult { Success = false, Error = "The selected equipment is no longer available." };
            }

            // Business rule - the stock can never become negative.
            if (equipment.StockQuantity <= 0)
            {
                return new CreateConnectionResult
                {
                    Success = false,
                    Error = $"There is no {equipment.Name} left in the stock. Please order more equipment."
                };
            }
        }

        // ---------------------------------------------------------------------- create the connection
        var quantity = Math.Max(1, order.Quantity);
        var connection = new Connection
        {
            AccountId = accountId,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            VendorId = order.VendorId,
            PlanId = plan.Id,
            CityId = order.CityId,
            Status = ConnectionStatus.Active,
            ActivationDate = DateTime.Now,
            MonthlyCharge = plan.Price,
            InstallationAddress = order.InstallationAddress,
            ContactNumber = order.ContactNumber
        };

        _db.Connections.Add(connection);

        if (equipment != null)
        {
            // The stock is reduced as soon as the equipment leaves the store.
            equipment.StockQuantity -= 1;

            connection.Equipment.Add(new ConnectionEquipment
            {
                ProductId = equipment.Id,
                SerialNumber = options.EquipmentSerialNumber,
                AssignedAt = DateTime.Now,
                AssignedByUserId = options.UserId
            });
        }

        // ---------------------------------------------------------------- activation invoice of the connection
        var taxPercent = await _settings.GetTaxPercentageAsync(cancellationToken);

        // The order totals are split over the requested number of connections.
        var depositPerConnection = Math.Round(order.SecurityDepositPayable / quantity, 2);
        var advancePerConnection = Math.Round(order.AdvancePayable / quantity, 2);
        var equipmentPerConnection = Math.Round(order.EquipmentAmount / quantity, 2);

        var calculation = _billing.CalculateActivationInvoice(depositPerConnection, advancePerConnection, equipmentPerConnection, taxPercent);

        var billSerial = await _sequence.NextSerialAsync("BILL", cancellationToken);
        var billNumber = $"INV-{billSerial.ToString("D6")}";

        var bill = new Bill
        {
            BillNumber = billNumber,
            OrderId = order.Id,
            BillType = BillType.Activation,
            PeriodStart = DateTime.Now,
            PeriodEnd = DateTime.Now.AddDays(30),
            PlanCharge = advancePerConnection,
            EquipmentChargeAmount = equipmentPerConnection,
            DiscountPercent = 0m,
            TaxPercent = taxPercent,
            GeneratedByUserId = options.UserId,
            GeneratedByName = options.UserName,
            GeneratedOn = DateTime.Now,
            DueDate = DateTime.Now.AddDays(7),
            Remarks = "Activation invoice - payable at the counter (cash on delivery)."
        };

        // The tax is calculated on the deposit, the advance and the equipment together.
        bill.SubTotal = calculation.SubTotal;
        bill.TaxAmount = calculation.TaxAmount;
        bill.TotalAmount = calculation.TotalAmount;
        bill.AmountPaid = 0m;
        bill.AmountDue = bill.TotalAmount;
        bill.Status = BillStatus.Due;

        // The individual charge lines printed on the invoice.
        bill.Lines.Add(new BillLine
        {
            Description = $"Security deposit ({plan.Category.Name})",
            LineType = "Security Deposit",
            Quantity = 1m,
            UnitPrice = depositPerConnection,
            Amount = depositPerConnection,
            SortOrder = 1
        });
        bill.Lines.Add(new BillLine
        {
            Description = $"Advance payment - {plan.Name}",
            LineType = "Plan",
            Quantity = 1m,
            UnitPrice = advancePerConnection,
            Amount = advancePerConnection,
            SortOrder = 2
        });

        if (equipmentPerConnection > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Equipment - {equipment!.Name}",
                LineType = "Equipment",
                Quantity = 1m,
                UnitPrice = equipmentPerConnection,
                Amount = equipmentPerConnection,
                SortOrder = 3
            });
        }

        // The invoice is attached to the connection through the navigation property so that
        // Entity Framework resolves the foreign key while it saves the graph.
        connection.Bills.Add(bill);

        // ------------------------------------------------------ security deposit ledger (Held)
        var depositEntry = new SecurityDepositEntry
        {
            OrderId = order.Id,
            Amount = Math.Round(order.SecurityDepositAmount / quantity, 2),
            DiscountAmount = Math.Round((order.SecurityDepositAmount - order.SecurityDepositPayable) / quantity, 2),
            NetAmount = depositPerConnection,
            Status = SecurityDepositStatus.Held,
            HeldOn = DateTime.Now,
            Remarks = "Refundable security deposit held against the connection."
        };

        depositEntry.Connection = connection;

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateConnectionResult
        {
            Success = true,
            AccountId = accountId,
            BillNumber = billNumber,
            TotalAmount = bill.TotalAmount
        };
    }

    /// <summary>
    /// Prefix of the reason that marks a connection as suspended by the overdue rule. It allows the
    /// service to restore the connection automatically once the bill is paid.
    /// </summary>
    public const string OverdueReasonPrefix = "Overdue:";

    /// <summary>
    /// Applies the overdue rule: a bill that stays unpaid for longer than the configured number of
    /// days suspends the connection, and paying the bill restores it.
    /// </summary>
    /// <param name="vendorId">Limits the sweep to one partner. Null sweeps every connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of connections whose state was changed.</returns>
    public async Task<int> ApplyOverdueRuleAsync(int? vendorId = null, CancellationToken cancellationToken = default)
    {
        var overdueDays = await _settings.GetOverdueDaysAsync(cancellationToken);
        var today = DateTime.Today;
        var changed = 0;

        var bills = await _db.Bills
            .Include(b => b.Connection)
            .Where(b => b.Status == BillStatus.Due && b.AmountDue > 0)
            .Where(b => vendorId == null || (b.Connection != null && b.Connection.VendorId == vendorId))
            .ToListAsync(cancellationToken);

        var overdueConnectionIds = new HashSet<int>();

        foreach (var bill in bills)
        {
            var connection = bill.Connection;
            if (connection is null || connection.Status == ConnectionStatus.PermanentlyInactive)
            {
                continue;
            }

            var overdueOn = bill.DueDate.Date.AddDays(overdueDays);
            if (overdueOn > today)
            {
                continue;
            }

            overdueConnectionIds.Add(connection.Id);

            if (connection.Status == ConnectionStatus.Active)
            {
                connection.Status = ConnectionStatus.TemporarilyInactive;
                connection.StatusReason =
                    $"{OverdueReasonPrefix} the bill {bill.BillNumber} was not paid within {overdueDays} days.";
                connection.StatusChangedAt = DateTime.Now;
                connection.StatusChangedByUserId = "system";
                changed++;
            }
        }

        // A connection that was suspended by this rule is restored as soon as no bill is overdue anymore.
        var suspended = await _db.Connections
            .Where(c => c.Status == ConnectionStatus.TemporarilyInactive)
            .Where(c => c.StatusReason != null && c.StatusReason.StartsWith(OverdueReasonPrefix))
            .Where(c => vendorId == null || c.VendorId == vendorId)
            .ToListAsync(cancellationToken);

        foreach (var connection in suspended)
        {
            if (overdueConnectionIds.Contains(connection.Id))
            {
                continue;
            }

            connection.Status = ConnectionStatus.Active;
            connection.StatusReason = "The overdue bill was paid, the service was restored.";
            connection.StatusChangedAt = DateTime.Now;
            connection.StatusChangedByUserId = "system";
            changed++;
        }

        if (changed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
