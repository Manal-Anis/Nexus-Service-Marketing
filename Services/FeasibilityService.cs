using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Information about a single feasibility check that has to be performed.</summary>
public class FeasibilityRequirement
{
    /// <summary>Type of the check (landline or internet).</summary>
    public FeasibilityCheckType CheckType { get; init; }

    /// <summary>True when the check is mandatory before a connection can be created.</summary>
    public bool IsMandatory { get; init; } = true;

    /// <summary>Explanation shown to the technical user on the feasibility screen.</summary>
    public string Instructions { get; init; } = string.Empty;
}

/// <summary>
/// Feasibility rules (requirement section 5.2).
/// A Dial-Up order for a customer that does not own a Nexus landline needs two checks
/// (landline and internet). Every other order needs exactly one check.
/// </summary>
public class FeasibilityService
{
    /// <summary>
    /// Returns the list of feasibility checks that have to be completed for an order.
    /// </summary>
    /// <param name="typeLetter">D = Dial-Up, T = Telephone/Landline, B = Broadband.</param>
    /// <param name="requiresNewLandline">
    /// True for a Dial-Up order of a customer that does not own an active Nexus landline.
    /// </param>
    public IReadOnlyList<FeasibilityRequirement> GetRequirements(string typeLetter, bool requiresNewLandline)
    {
        var letter = (typeLetter ?? string.Empty).Trim().ToUpperInvariant();

        switch (letter)
        {
            case "D":
                // Dial-Up needs the landline first, therefore both checks are required.
                if (requiresNewLandline)
                {
                    return new[]
                    {
                        new FeasibilityRequirement
                        {
                            CheckType = FeasibilityCheckType.Landline,
                            Instructions = "Check the availability of the Nexus landline exchange at the installation address."
                        },
                        new FeasibilityRequirement
                        {
                            CheckType = FeasibilityCheckType.Internet,
                            Instructions = "Check the dial-up server availability and the distance parameters."
                        }
                    };
                }

                // The customer already owns an active landline, only the internet is checked.
                return new[]
                {
                    new FeasibilityRequirement
                    {
                        CheckType = FeasibilityCheckType.Internet,
                        Instructions = "Check the dial-up server availability against the existing landline connection."
                    }
                };

            case "T":
                // Landline only order - one check on the telephone exchange.
                return new[]
                {
                    new FeasibilityRequirement
                    {
                        CheckType = FeasibilityCheckType.Landline,
                        Instructions = "Check the availability of the Nexus telephone exchange at the installation address."
                    }
                };

            default:
                // Broadband (and every other category) needs a single internet check.
                return new[]
                {
                    new FeasibilityRequirement
                    {
                        CheckType = FeasibilityCheckType.Internet,
                        Instructions = "Check the DSLAM port availability and the distance parameters."
                    }
                };
        }
    }

    /// <summary>
    /// Works out the aggregate result of an order from the individual checks.
    /// The order is only feasible when every mandatory check came back as feasible.
    /// </summary>
    public FeasibilityResult ResolveResult(IEnumerable<OrderFeasibilityCheck> checks)
    {
        var list = checks.ToList();

        if (list.Count == 0 || list.Any(c => c.Result == FeasibilityResult.Pending))
        {
            return FeasibilityResult.Pending;
        }

        return list.Any(c => c.Result == FeasibilityResult.NotFeasible)
            ? FeasibilityResult.NotFeasible
            : FeasibilityResult.Feasible;
    }

    /// <summary>
    /// Maps the aggregate feasibility result to the order lifecycle state.
    /// </summary>
    public OrderStatus MapToOrderStatus(FeasibilityResult result) => result switch
    {
        FeasibilityResult.Pending => OrderStatus.FeasibilityPending,
        FeasibilityResult.Feasible => OrderStatus.Feasible,
        FeasibilityResult.NotFeasible => OrderStatus.NotFeasible,
        _ => OrderStatus.Placed
    };
}
