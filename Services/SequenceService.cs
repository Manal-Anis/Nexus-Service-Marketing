using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;

namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// Hands out the serial numbers used inside the Order ID and the Account ID.
/// The counter row is locked with an UPDATE / ROW lock inside an explicit transaction so two
/// concurrent requests can never receive the same serial number.
/// </summary>
public class SequenceService
{
    /// <summary>Database context used to reach the SequenceCounters table.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>Creates the service with the injected database context.</summary>
    public SequenceService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Name of the counter used for the order identifiers of one type letter.</summary>
    public static string OrderCounterName(char typeLetter) => $"ORDER-{char.ToUpperInvariant(typeLetter)}";

    /// <summary>Name of the counter used for the account identifiers of one type letter.</summary>
    public static string AccountCounterName(char typeLetter) => $"ACCOUNT-{char.ToUpperInvariant(typeLetter)}";

    /// <summary>
    /// Reads the next free serial number of the given counter and stores it immediately.
    /// The whole operation runs inside one transaction with a row lock.
    /// </summary>
    public async Task<long> NextSerialAsync(string counterName, CancellationToken cancellationToken = default)
    {
        // The transaction keeps the row lock until the counter has been written back.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // UPDLOCK + ROWLOCK blocks any other session that tries to increase the same counter.
        var values = await _db.Database
            .SqlQueryRaw<long>(
                "SELECT CurrentValue AS Value FROM SequenceCounters WITH (UPDLOCK, ROWLOCK) WHERE Name = {0}",
                counterName)
            .ToListAsync(cancellationToken);

        long nextValue;

        if (values.Count == 0)
        {
            // The counter does not exist yet - create it starting from the first number.
            nextValue = 1;
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO SequenceCounters (Name, CurrentValue, UpdatedAt) VALUES ({0}, {1}, SYSUTCDATETIME())",
                counterName, nextValue);
        }
        else
        {
            nextValue = values[0] + 1;
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE SequenceCounters SET CurrentValue = {0}, UpdatedAt = SYSUTCDATETIME() WHERE Name = {1}",
                nextValue, counterName);
        }

        await transaction.CommitAsync(cancellationToken);
        return nextValue;
    }

    /// <summary>Returns the next order serial number for the given connection type letter.</summary>
    public Task<long> NextOrderSerialAsync(char typeLetter, CancellationToken cancellationToken = default)
        => NextSerialAsync(OrderCounterName(typeLetter), cancellationToken);

    /// <summary>Returns the next account serial number for the given connection type letter.</summary>
    public Task<long> NextAccountSerialAsync(char typeLetter, CancellationToken cancellationToken = default)
        => NextSerialAsync(AccountCounterName(typeLetter), cancellationToken);
}
