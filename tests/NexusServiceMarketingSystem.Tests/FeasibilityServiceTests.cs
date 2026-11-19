using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using Xunit;

namespace NexusServiceMarketingSystem.Tests;

/// <summary>
/// Tests of the feasibility rules (requirement section 5.2).
/// A Dial-Up order of a customer that does not own a Nexus landline needs two checks
/// (landline and internet), every other order needs exactly one check.
/// </summary>
public class FeasibilityServiceTests
{
    private readonly FeasibilityService _service = new();

    [Fact]
    public void DialUp_Of_A_Customer_Without_Landline_Needs_Both_The_Landline_And_The_Internet_Check()
    {
        var requirements = _service.GetRequirements("D", requiresNewLandline: true);

        Assert.Equal(2, requirements.Count);
        Assert.Equal(FeasibilityCheckType.Landline, requirements[0].CheckType);
        Assert.Equal(FeasibilityCheckType.Internet, requirements[1].CheckType);
        Assert.All(requirements, r => Assert.True(r.IsMandatory));
    }

    [Fact]
    public void DialUp_Of_A_Customer_With_An_Existing_Landline_Needs_The_Internet_Check_Only()
    {
        var requirements = _service.GetRequirements("D", requiresNewLandline: false);

        Assert.Single(requirements);
        Assert.Equal(FeasibilityCheckType.Internet, requirements[0].CheckType);
    }

    [Fact]
    public void Landline_Order_Needs_The_Landline_Check_Only()
    {
        var requirements = _service.GetRequirements("T", requiresNewLandline: false);

        Assert.Single(requirements);
        Assert.Equal(FeasibilityCheckType.Landline, requirements[0].CheckType);
    }

    [Fact]
    public void Broadband_Order_Needs_The_Internet_Check_Only()
    {
        var requirements = _service.GetRequirements("B", requiresNewLandline: false);

        Assert.Single(requirements);
        Assert.Equal(FeasibilityCheckType.Internet, requirements[0].CheckType);
    }

    [Fact]
    public void The_Type_Letter_Is_Not_Case_Sensitive()
    {
        var requirements = _service.GetRequirements("d", requiresNewLandline: true);

        Assert.Equal(2, requirements.Count);
        Assert.Equal(FeasibilityCheckType.Landline, requirements[0].CheckType);
    }

    [Fact]
    public void Every_Check_Comes_With_An_Instruction_For_The_Technical_User()
    {
        var requirements = _service.GetRequirements("D", requiresNewLandline: true);

        Assert.All(requirements, r => Assert.False(string.IsNullOrWhiteSpace(r.Instructions)));
    }

    [Fact]
    public void ResolveResult_Returns_Pending_When_There_Is_No_Check_At_All()
    {
        var result = _service.ResolveResult(Array.Empty<OrderFeasibilityCheck>());

        Assert.Equal(FeasibilityResult.Pending, result);
    }

    [Fact]
    public void ResolveResult_Returns_Pending_While_One_Check_Is_Still_Open()
    {
        var result = _service.ResolveResult(new[]
        {
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Landline, Result = FeasibilityResult.Feasible },
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Internet, Result = FeasibilityResult.Pending }
        });

        Assert.Equal(FeasibilityResult.Pending, result);
    }

    [Fact]
    public void ResolveResult_Returns_NotFeasible_When_One_Check_Failed()
    {
        var result = _service.ResolveResult(new[]
        {
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Landline, Result = FeasibilityResult.Feasible },
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Internet, Result = FeasibilityResult.NotFeasible }
        });

        Assert.Equal(FeasibilityResult.NotFeasible, result);
    }

    [Fact]
    public void ResolveResult_Returns_Feasible_When_Every_Check_Passed()
    {
        var result = _service.ResolveResult(new[]
        {
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Landline, Result = FeasibilityResult.Feasible },
            new OrderFeasibilityCheck { CheckType = FeasibilityCheckType.Internet, Result = FeasibilityResult.Feasible }
        });

        Assert.Equal(FeasibilityResult.Feasible, result);
    }

    [Theory]
    [InlineData(FeasibilityResult.Pending, OrderStatus.FeasibilityPending)]
    [InlineData(FeasibilityResult.Feasible, OrderStatus.Feasible)]
    [InlineData(FeasibilityResult.NotFeasible, OrderStatus.NotFeasible)]
    public void MapToOrderStatus_Maps_The_Check_Result_Onto_The_Order_State(FeasibilityResult result, OrderStatus expected)
    {
        Assert.Equal(expected, _service.MapToOrderStatus(result));
    }
}
