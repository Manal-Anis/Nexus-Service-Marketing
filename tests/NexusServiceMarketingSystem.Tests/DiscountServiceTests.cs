using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using Xunit;

namespace NexusServiceMarketingSystem.Tests;

/// <summary>
/// Tests of the bulk / corporate discount tiers (requirement section 5.6).
/// </summary>
public class DiscountServiceTests
{
    private readonly DiscountService _service = new();

    /// <summary>The tiers exactly as they are maintained by the administrator.</summary>
    private static List<DiscountRule> DefaultRules() =>
    [
        new DiscountRule { MinConnections = 1, MaxConnections = 9, DiscountPercent = 0m, IsActive = true },
        new DiscountRule { MinConnections = 10, MaxConnections = 15, DiscountPercent = 25m, IsActive = true },
        new DiscountRule { MinConnections = 16, MaxConnections = 25, DiscountPercent = 50m, IsActive = true },
        new DiscountRule { MinConnections = 26, MaxConnections = 50, DiscountPercent = 75m, IsActive = true },
        new DiscountRule { MinConnections = 51, MaxConnections = null, DiscountPercent = 100m, IsActive = true }
    ];

    [Theory]
    [InlineData(1, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 25)]
    [InlineData(15, 25)]
    [InlineData(16, 50)]
    [InlineData(25, 50)]
    [InlineData(26, 75)]
    [InlineData(50, 75)]
    [InlineData(51, 100)]
    [InlineData(500, 100)]
    public void GetDiscountPercent_Returns_The_Percentage_Of_The_Matching_Tier(int connectionCount, double expected)
    {
        var percent = _service.GetDiscountPercent(DefaultRules(), connectionCount);

        Assert.Equal((decimal)expected, percent);
    }

    [Fact]
    public void GetDiscountPercent_Returns_Zero_When_No_Rule_Matches()
    {
        var percent = _service.GetDiscountPercent(DefaultRules(), 0);

        Assert.Equal(0m, percent);
    }

    [Fact]
    public void GetDiscountPercent_Ignores_Inactive_Rules()
    {
        var rules = new List<DiscountRule>
        {
            new() { MinConnections = 10, MaxConnections = 15, DiscountPercent = 25m, IsActive = false },
            new() { MinConnections = 16, MaxConnections = 25, DiscountPercent = 50m, IsActive = true }
        };

        Assert.Equal(0m, _service.GetDiscountPercent(rules, 12));
        Assert.Equal(50m, _service.GetDiscountPercent(rules, 20));
    }

    [Fact]
    public void Calculate_Discounts_Both_The_Deposit_And_The_Advance()
    {
        var result = _service.Calculate(10000m, 4000m, 20, DefaultRules());

        Assert.Equal(50m, result.DiscountPercent);
        Assert.Equal(10000m, result.GrossSecurityDeposit);
        Assert.Equal(5000m, result.NetSecurityDeposit);
        Assert.Equal(4000m, result.GrossAdvance);
        Assert.Equal(2000m, result.NetAdvance);
        Assert.Equal(7000m, result.TotalSaved);
    }

    [Fact]
    public void Calculate_Without_A_Tier_Keeps_The_Gross_Amounts()
    {
        var result = _service.Calculate(3250m, 1500m, 5, DefaultRules());

        Assert.Equal(0m, result.DiscountPercent);
        Assert.Equal(3250m, result.NetSecurityDeposit);
        Assert.Equal(1500m, result.NetAdvance);
        Assert.Equal(0m, result.TotalSaved);
    }

    [Fact]
    public void Calculate_At_Full_Discount_Clears_Both_Amounts()
    {
        var result = _service.Calculate(3250m, 1500m, 60, DefaultRules());

        Assert.Equal(100m, result.DiscountPercent);
        Assert.Equal(0m, result.NetSecurityDeposit);
        Assert.Equal(0m, result.NetAdvance);
        Assert.Equal(4750m, result.TotalSaved);
    }

    [Fact]
    public void Calculate_Rounds_The_Net_Amounts_To_Two_Decimal_Places()
    {
        var result = _service.Calculate(3250.55m, 1500.55m, 10, DefaultRules());

        Assert.Equal(2437.91m, result.NetSecurityDeposit);   // 3250.55 * 0.75
        Assert.Equal(1125.41m, result.NetAdvance);           // 1500.55 * 0.75
    }
}
