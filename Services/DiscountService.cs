using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Result of a discount calculation.</summary>
public class DiscountBreakdown
{
    /// <summary>Discount percentage that matched the connection quantity.</summary>
    public decimal DiscountPercent { get; init; }

    /// <summary>Gross security deposit before the discount.</summary>
    public decimal GrossSecurityDeposit { get; init; }

    /// <summary>Security deposit after the discount.</summary>
    public decimal NetSecurityDeposit { get; init; }

    /// <summary>Gross advance payment before the discount.</summary>
    public decimal GrossAdvance { get; init; }

    /// <summary>Advance payment after the discount.</summary>
    public decimal NetAdvance { get; init; }

    /// <summary>Total amount saved by the customer.</summary>
    public decimal TotalSaved => (GrossSecurityDeposit - NetSecurityDeposit) + (GrossAdvance - NetAdvance);
}

/// <summary>
/// Bulk / corporate discount calculator (requirement section 5.6).
/// The discount applies to the advance payment and to the security deposit of an order and is
/// driven by the number of connections requested inside that single order.
/// </summary>
public class DiscountService
{
    /// <summary>
    /// Returns the discount percentage that matches the given number of connections.
    /// When no tier matches, zero percent is returned.
    /// </summary>
    /// <param name="rules">Discount tiers maintained by the administrator.</param>
    /// <param name="connectionCount">Number of connections requested inside the order.</param>
    public decimal GetDiscountPercent(IEnumerable<DiscountRule> rules, int connectionCount)
    {
        if (connectionCount < 1)
        {
            return 0m;
        }

        // The tiers are stored with non overlapping ranges (10-15, 16-25, 26-50, 51+).
        var rule = rules
            .Where(r => r.IsActive)
            .Where(r => connectionCount >= r.MinConnections &&
                        (r.MaxConnections == null || connectionCount <= r.MaxConnections.Value))
            .OrderBy(r => r.MinConnections)
            .FirstOrDefault();

        return rule?.DiscountPercent ?? 0m;
    }

    /// <summary>
    /// Calculates the discounted security deposit and the discounted advance payment.
    /// </summary>
    public DiscountBreakdown Calculate(decimal securityDeposit, decimal advance, int connectionCount,
        IEnumerable<DiscountRule> rules)
    {
        var percent = GetDiscountPercent(rules, connectionCount);

        // Amounts are rounded to two decimal places because they are currency values.
        var netDeposit = Math.Round(securityDeposit * (1m - (percent / 100m)), 2);
        var netAdvance = Math.Round(advance * (1m - (percent / 100m)), 2);

        return new DiscountBreakdown
        {
            DiscountPercent = percent,
            GrossSecurityDeposit = securityDeposit,
            NetSecurityDeposit = netDeposit,
            GrossAdvance = advance,
            NetAdvance = netAdvance
        };
    }
}
