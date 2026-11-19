using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Charge lines that a bill is built from.</summary>
public class BillLineDraft
{
    /// <summary>Text printed on the invoice.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Category of the line (Plan, Local, STD, Mobile, Equipment, Replacement, Deposit).</summary>
    public string? LineType { get; init; }

    /// <summary>Quantity represented by the line (hours or minutes).</summary>
    public decimal Quantity { get; init; } = 1m;

    /// <summary>Unit price of the line.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Total amount of the line.</summary>
    public decimal Amount { get; init; }
}

/// <summary>All values needed to calculate one bill.</summary>
public class BillCalculationInput
{
    /// <summary>Base plan or rental charge of the period.</summary>
    public decimal PlanCharge { get; set; }

    /// <summary>Total local call charges.</summary>
    public decimal LocalChargeAmount { get; set; }

    /// <summary>Total STD call charges.</summary>
    public decimal StdChargeAmount { get; set; }

    /// <summary>Total mobile messaging charges.</summary>
    public decimal MobileChargeAmount { get; set; }

    /// <summary>Replacement charges for damaged equipment.</summary>
    public decimal ReplacementChargeAmount { get; set; }

    /// <summary>Equipment charge billed on the activation invoice.</summary>
    public decimal EquipmentChargeAmount { get; set; }

    /// <summary>Security deposit billed on the activation invoice.</summary>
    public decimal SecurityDepositAmount { get; set; }

    /// <summary>Outstanding amount carried forward from the previous bill.</summary>
    public decimal PreviousDue { get; set; }

    /// <summary>Discount percentage applied on the bill.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Service tax percentage (12.24% by default).</summary>
    public decimal TaxPercent { get; set; }
}

/// <summary>Calculated totals of one bill.</summary>
public class BillCalculationResult
{
    /// <summary>Sum of every charge line before the discount.</summary>
    public decimal GrossCharges { get; init; }

    /// <summary>Discount amount in Rupees.</summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>Sub total after the discount but before the tax.</summary>
    public decimal SubTotal { get; init; }

    /// <summary>Service tax amount.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Total payable amount.</summary>
    public decimal TotalAmount { get; init; }
}

/// <summary>
/// Billing calculator (requirement section 5.7).
/// Formula: charges + previous dues - discount = sub total; tax 12.24% on the whole bill;
/// total payable = sub total + tax.
/// The class is stateless so it can be covered by unit tests without a database.
/// </summary>
public class BillingService
{
    /// <summary>
    /// Calculates the totals of a bill.
    /// </summary>
    /// <param name="input">Charge values of the bill.</param>
    public BillCalculationResult Calculate(BillCalculationInput input)
    {
        // Step 1 - add every charge line of the bill.
        var gross = input.PlanCharge
                    + input.LocalChargeAmount
                    + input.StdChargeAmount
                    + input.MobileChargeAmount
                    + input.ReplacementChargeAmount
                    + input.EquipmentChargeAmount
                    + input.SecurityDepositAmount
                    + input.PreviousDue;

        // Step 2 - remove the discount that was granted on this bill.
        var discount = Math.Round(gross * (input.DiscountPercent / 100m), 2);
        var subTotal = Math.Round(gross - discount, 2);

        // Step 3 - apply the service tax on the whole bill.
        var tax = Math.Round(subTotal * (input.TaxPercent / 100m), 2);
        var total = Math.Round(subTotal + tax, 2);

        return new BillCalculationResult
        {
            GrossCharges = Math.Round(gross, 2),
            DiscountAmount = discount,
            SubTotal = subTotal,
            TaxAmount = tax,
            TotalAmount = total
        };
    }

    /// <summary>
    /// Calculates the activation invoice of a new connection: the discounted security deposit,
    /// the discounted advance (first period charge), the equipment price and the tax.
    /// </summary>
    /// <param name="discountedSecurityDeposit">Security deposit after the bulk discount.</param>
    /// <param name="discountedAdvance">Advance payment after the bulk discount.</param>
    /// <param name="equipmentAmount">Price of the equipment issued with the connection.</param>
    /// <param name="taxPercent">Service tax percentage taken from the settings.</param>
    public BillCalculationResult CalculateActivationInvoice(decimal discountedSecurityDeposit, decimal discountedAdvance,
        decimal equipmentAmount, decimal taxPercent)
    {
        // The discount has already been applied on the deposit and on the advance, therefore
        // the activation invoice itself is calculated without any further discount.
        return Calculate(new BillCalculationInput
        {
            PlanCharge = discountedAdvance,
            EquipmentChargeAmount = equipmentAmount,
            SecurityDepositAmount = discountedSecurityDeposit,
            DiscountPercent = 0m,
            TaxPercent = taxPercent
        });
    }

    /// <summary>
    /// Recalculates the payment state of a bill after a receipt has been recorded.
    /// A bill is Paid when nothing is left outstanding, otherwise it stays Due.
    /// </summary>
    /// <param name="totalAmount">Total payable amount of the bill.</param>
    /// <param name="amountPaid">Amount received so far.</param>
    /// <param name="amountDue">Amount still outstanding.</param>
    public (BillStatus Status, decimal Paid, decimal Due) ResolvePaymentState(decimal totalAmount, decimal amountPaid, decimal amountDue)
    {
        var paid = Math.Round(amountPaid, 2);
        var due = Math.Round(totalAmount - paid, 2);

        // A small tolerance avoids a rounding remainder of one paisa keeping a bill "Due".
        if (due <= 0.009m)
        {
            due = 0m;
            paid = Math.Round(totalAmount, 2);
        }

        var status = due <= 0m ? BillStatus.Paid : BillStatus.Due;
        return (status, paid, due);
    }
}
