using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using Xunit;

namespace NexusServiceMarketingSystem.Tests;

/// <summary>
/// Tests of the bill calculation (requirement section 5.7).
/// Formula: charges + previous dues - discount = sub total, then the service tax of
/// 12.24% on the whole bill, then total payable = sub total + tax.
/// </summary>
public class BillingServiceTests
{
    private readonly BillingService _service = new();

    /// <summary>The service tax that the settings hold by default.</summary>
    private const decimal TaxPercent = 12.24m;

    [Fact]
    public void Calculate_Adds_Every_Charge_Line_And_The_Previous_Due_To_The_Gross_Amount()
    {
        var result = _service.Calculate(new BillCalculationInput
        {
            PlanCharge = 1000m,
            LocalChargeAmount = 500m,
            StdChargeAmount = 200m,
            MobileChargeAmount = 100m,
            ReplacementChargeAmount = 50m,
            EquipmentChargeAmount = 150m,
            PreviousDue = 200m,
            TaxPercent = TaxPercent
        });

        Assert.Equal(2200m, result.GrossCharges);
    }

    [Fact]
    public void Calculate_Removes_The_Discount_Before_The_Tax_Is_Calculated()
    {
        var result = _service.Calculate(new BillCalculationInput
        {
            PlanCharge = 1000m,
            LocalChargeAmount = 500m,
            StdChargeAmount = 200m,
            MobileChargeAmount = 100m,
            PreviousDue = 200m,
            DiscountPercent = 10m,
            TaxPercent = TaxPercent
        });

        // Gross 2000 - 10% discount = 1800
        Assert.Equal(2000m, result.GrossCharges);
        Assert.Equal(200m, result.DiscountAmount);
        Assert.Equal(1800m, result.SubTotal);

        // The tax is taken from the sub total, not from the gross amount.
        Assert.Equal(220.32m, result.TaxAmount);
        Assert.Equal(2020.32m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_Applies_The_Service_Tax_Of_Twelve_Point_Two_Four_Percent()
    {
        var result = _service.Calculate(new BillCalculationInput
        {
            PlanCharge = 2000m,
            DiscountPercent = 25m,
            TaxPercent = TaxPercent
        });

        Assert.Equal(2000m, result.GrossCharges);
        Assert.Equal(500m, result.DiscountAmount);
        Assert.Equal(1500m, result.SubTotal);
        Assert.Equal(183.60m, result.TaxAmount);
        Assert.Equal(1683.60m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_Without_A_Discount_Keeps_The_Gross_And_The_Sub_Total_Equal()
    {
        var result = _service.Calculate(new BillCalculationInput
        {
            PlanCharge = 1000m,
            TaxPercent = TaxPercent
        });

        Assert.Equal(1000m, result.GrossCharges);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(1000m, result.SubTotal);
        Assert.Equal(122.40m, result.TaxAmount);
        Assert.Equal(1122.40m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_Rounds_Every_Amount_To_Two_Decimal_Places()
    {
        var result = _service.Calculate(new BillCalculationInput
        {
            PlanCharge = 100.05m,
            TaxPercent = TaxPercent
        });

        Assert.Equal(100.05m, result.SubTotal);
        Assert.Equal(12.25m, result.TaxAmount);   // 100.05 * 0.1224 = 12.24612
        Assert.Equal(112.30m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_Of_An_Empty_Bill_Is_Zero()
    {
        var result = _service.Calculate(new BillCalculationInput { TaxPercent = TaxPercent });

        Assert.Equal(0m, result.GrossCharges);
        Assert.Equal(0m, result.SubTotal);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public void CalculateActivationInvoice_Adds_The_Deposit_The_Advance_And_The_Equipment()
    {
        var result = _service.CalculateActivationInvoice(2500m, 1500m, 3000m, TaxPercent);

        Assert.Equal(7000m, result.GrossCharges);
        Assert.Equal(0m, result.DiscountAmount);      // the bulk discount is already applied
        Assert.Equal(7000m, result.SubTotal);
        Assert.Equal(856.80m, result.TaxAmount);
        Assert.Equal(7856.80m, result.TotalAmount);
    }

    [Fact]
    public void ResolvePaymentState_Marks_A_Bill_Paid_When_Nothing_Is_Left_Outstanding()
    {
        var (status, paid, due) = _service.ResolvePaymentState(1000m, 1000m, 0m);

        Assert.Equal(BillStatus.Paid, status);
        Assert.Equal(1000m, paid);
        Assert.Equal(0m, due);
    }

    [Fact]
    public void ResolvePaymentState_Keeps_A_Bill_Due_While_Money_Is_Missing()
    {
        var (status, paid, due) = _service.ResolvePaymentState(1000m, 400m, 600m);

        Assert.Equal(BillStatus.Due, status);
        Assert.Equal(400m, paid);
        Assert.Equal(600m, due);
    }

    [Fact]
    public void ResolvePaymentState_Tolerates_A_Rounding_Remainder_Of_One_Paisa()
    {
        var (status, paid, due) = _service.ResolvePaymentState(1000m, 999.995m, 0m);

        Assert.Equal(BillStatus.Paid, status);
        Assert.Equal(1000m, paid);
        Assert.Equal(0m, due);
    }
}
