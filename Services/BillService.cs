using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Values entered by the accounts department while a periodic bill is generated.</summary>
public class GenerateBillInput
{
    /// <summary>Connection the bill is raised against.</summary>
    public int ConnectionId { get; init; }

    /// <summary>First day covered by the bill.</summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>Last day covered by the bill.</summary>
    public DateTime PeriodEnd { get; init; }

    /// <summary>Local call minutes used during the period.</summary>
    public decimal LocalMinutes { get; init; }

    /// <summary>STD call minutes used during the period.</summary>
    public decimal StdMinutes { get; init; }

    /// <summary>Mobile messaging minutes used during the period.</summary>
    public decimal MobileMinutes { get; init; }

    /// <summary>Hours used by an hourly internet pack during the period.</summary>
    public decimal HoursUsed { get; init; }

    /// <summary>Replacement charge for damaged equipment.</summary>
    public decimal ReplacementCharge { get; init; }

    /// <summary>Reason recorded for the replacement charge.</summary>
    public string? ReplacementReason { get; init; }

    /// <summary>Discount percentage granted on the bill.</summary>
    public decimal DiscountPercent { get; init; }

    /// <summary>Remarks printed on the invoice.</summary>
    public string? Remarks { get; init; }

    /// <summary>User account that generated the bill.</summary>
    public string? UserId { get; init; }

    /// <summary>Display name of the user that generated the bill.</summary>
    public string? UserName { get; init; }
}

/// <summary>Outcome of a bill generation.</summary>
public class GenerateBillResult
{
    /// <summary>True when the bill was stored.</summary>
    public bool Success { get; init; }

    /// <summary>Error message shown to the user when the bill could not be generated.</summary>
    public string? Error { get; init; }

    /// <summary>Number of the generated bill.</summary>
    public string? BillNumber { get; init; }

    /// <summary>Total payable amount of the bill.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Identifier of the created bill.</summary>
    public int BillId { get; init; }
}

/// <summary>
/// Generates the periodic bills of the accounts department (requirement section 5.7).
/// Only postpaid connections exist, therefore the bill is always raised after the period has ended.
/// </summary>
public class BillService
{
    /// <summary>Database context used to store the bills.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Service that hands out the serial numbers of the bill numbers.</summary>
    private readonly SequenceService _sequence;

    /// <summary>Billing calculator.</summary>
    private readonly BillingService _billing;

    /// <summary>Service that reads the editable application settings.</summary>
    private readonly SettingsService _settings;

    /// <summary>Creates the service with the injected dependencies.</summary>
    public BillService(ApplicationDbContext db, SequenceService sequence, BillingService billing, SettingsService settings)
    {
        _db = db;
        _sequence = sequence;
        _billing = billing;
        _settings = settings;
    }

    /// <summary>
    /// Calculates and stores the periodic bill of one connection.
    /// </summary>
    public async Task<GenerateBillResult> GeneratePeriodicBillAsync(GenerateBillInput input, CancellationToken cancellationToken = default)
    {
        var connection = await _db.Connections
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.Id == input.ConnectionId, cancellationToken);

        if (connection == null)
        {
            return new GenerateBillResult { Success = false, Error = "The connection could not be found." };
        }

        var plan = connection.Plan;

        // ------------------------------------------------------------- base plan / rental charge
        var planCharge = plan.Price;

        // ------------------------------------------------------------------- call charge calculation
        var localCharge = plan.LocalRate.HasValue ? Math.Round(input.LocalMinutes * plan.LocalRate.Value, 2) : 0m;
        var stdCharge = plan.StdRate.HasValue ? Math.Round(input.StdMinutes * plan.StdRate.Value, 2) : 0m;
        var mobileCharge = plan.MobileMsgRate.HasValue ? Math.Round(input.MobileMinutes * plan.MobileMsgRate.Value, 2) : 0m;

        // ------------------------------------------------------------------ outstanding previous dues
        var previousDue = await _db.Bills
            .Where(b => b.ConnectionId == connection.Id && b.Status == BillStatus.Due)
            .SumAsync(b => (decimal?)(b.TotalAmount - b.AmountPaid), cancellationToken) ?? 0m;

        var taxPercent = await _settings.GetTaxPercentageAsync(cancellationToken);

        // -------------------------------------------------------------------- calculation of the totals
        var calculation = _billing.Calculate(new BillCalculationInput
        {
            PlanCharge = planCharge,
            LocalChargeAmount = localCharge,
            StdChargeAmount = stdCharge,
            MobileChargeAmount = mobileCharge,
            ReplacementChargeAmount = input.ReplacementCharge,
            PreviousDue = previousDue,
            DiscountPercent = input.DiscountPercent,
            TaxPercent = taxPercent
        });

        var billSerial = await _sequence.NextSerialAsync("BILL", cancellationToken);
        var billNumber = $"INV-{billSerial.ToString("D6")}";

        var bill = new Bill
        {
            BillNumber = billNumber,
            ConnectionId = connection.Id,
            BillType = BillType.Periodic,
            PeriodStart = input.PeriodStart,
            PeriodEnd = input.PeriodEnd,
            PlanCharge = planCharge,
            LocalChargeAmount = localCharge,
            StdChargeAmount = stdCharge,
            MobileChargeAmount = mobileCharge,
            ReplacementChargeAmount = input.ReplacementCharge,
            PreviousDue = previousDue,
            DiscountPercent = input.DiscountPercent,
            DiscountAmount = calculation.DiscountAmount,
            SubTotal = calculation.SubTotal,
            TaxPercent = taxPercent,
            TaxAmount = calculation.TaxAmount,
            TotalAmount = calculation.TotalAmount,
            AmountPaid = 0m,
            AmountDue = calculation.TotalAmount,
            Status = BillStatus.Due,
            DueDate = input.PeriodEnd,
            GeneratedByUserId = input.UserId,
            GeneratedByName = input.UserName,
            GeneratedOn = DateTime.Now,
            Remarks = input.Remarks
        };

        // ------------------------------------------------------------------- individual charge lines
        var order = 1;

        bill.Lines.Add(new BillLine
        {
            Description = $"{plan.Name} - plan charge ({input.PeriodStart:dd MMM yyyy} to {input.PeriodEnd:dd MMM yyyy})",
            LineType = "Plan",
            Quantity = 1m,
            UnitPrice = planCharge,
            Amount = planCharge,
            SortOrder = order++
        });

        if (input.HoursUsed > 0m && plan.Kind == PlanKind.Hourly)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Internet hours used ({input.HoursUsed:0.##} hrs)",
                LineType = "Usage",
                Quantity = input.HoursUsed,
                UnitPrice = plan.Hours.HasValue && plan.Hours.Value > 0 ? plan.Price / plan.Hours.Value : 0m,
                Amount = Math.Round(input.HoursUsed * (plan.Hours.HasValue && plan.Hours.Value > 0 ? plan.Price / plan.Hours.Value : 0m), 2),
                SortOrder = order++
            });
        }

        if (localCharge > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Local calls ({input.LocalMinutes:0.##} mins @ {plan.LocalRate:N2})",
                LineType = "Local",
                Quantity = input.LocalMinutes,
                UnitPrice = plan.LocalRate ?? 0m,
                Amount = localCharge,
                SortOrder = order++
            });
        }

        if (stdCharge > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"STD calls ({input.StdMinutes:0.##} mins @ {plan.StdRate:N2})",
                LineType = "STD",
                Quantity = input.StdMinutes,
                UnitPrice = plan.StdRate ?? 0m,
                Amount = stdCharge,
                SortOrder = order++
            });
        }

        if (mobileCharge > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Mobile messaging ({input.MobileMinutes:0.##} mins @ {plan.MobileMsgRate:N2})",
                LineType = "Mobile",
                Quantity = input.MobileMinutes,
                UnitPrice = plan.MobileMsgRate ?? 0m,
                Amount = mobileCharge,
                SortOrder = order++
            });
        }

        if (input.ReplacementCharge > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = $"Equipment replacement charge{(string.IsNullOrWhiteSpace(input.ReplacementReason) ? string.Empty : $" - {input.ReplacementReason}")}",
                LineType = "Replacement",
                Quantity = 1m,
                UnitPrice = input.ReplacementCharge,
                Amount = input.ReplacementCharge,
                SortOrder = order++
            });
        }

        if (previousDue > 0m)
        {
            bill.Lines.Add(new BillLine
            {
                Description = "Previous unpaid balance carried forward",
                LineType = "Previous Due",
                Quantity = 1m,
                UnitPrice = previousDue,
                Amount = previousDue,
                SortOrder = order++
            });
        }

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(cancellationToken);

        return new GenerateBillResult
        {
            Success = true,
            BillNumber = billNumber,
            TotalAmount = bill.TotalAmount,
            BillId = bill.Id
        };
    }
}
