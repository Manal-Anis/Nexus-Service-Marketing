using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// Reads the editable application settings (tax percentage, currency, overdue days...).
/// The values are cached in memory for a short period so the settings screens stay fast.
/// </summary>
public class SettingsService
{
    /// <summary>Database context used to read the settings.</summary>
    private readonly ApplicationDbContext _db;

    /// <summary>In memory cache of the settings.</summary>
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Date and time the cache was filled for the last time.</summary>
    private DateTime _cachedAt = DateTime.MinValue;

    /// <summary>Lock that protects the cache while it is refreshed.</summary>
    private readonly object _cacheLock = new();

    /// <summary>Creates the service with the injected database context.</summary>
    public SettingsService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Setting key that holds the service tax percentage.</summary>
    public const string TaxPercentageKey = "Billing.TaxPercentage";

    /// <summary>Setting key that holds the currency symbol.</summary>
    public const string CurrencyKey = "General.Currency";

    /// <summary>Setting key that holds the number of days after which a bill becomes overdue.</summary>
    public const string OverdueDaysKey = "Billing.OverdueDays";

    /// <summary>Setting key that holds the company support phone number.</summary>
    public const string SupportPhoneKey = "General.SupportPhone";

    /// <summary>Setting key that holds the company support email address.</summary>
    public const string SupportEmailKey = "General.SupportEmail";

    /// <summary>Setting key that holds the company address.</summary>
    public const string CompanyAddressKey = "General.CompanyAddress";

    /// <summary>Setting key that holds the number of days a bill stays payable after its due date.</summary>
    public const string PaymentGraceDaysKey = "Billing.PaymentGraceDays";

    /// <summary>Returns every setting as a dictionary.</summary>
    public async Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheAsync(cancellationToken);
        return new Dictionary<string, string>(_cache, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Returns one setting value or the supplied fallback when the key is unknown.</summary>
    public async Task<string> GetAsync(string key, string fallback = "", CancellationToken cancellationToken = default)
    {
        await EnsureCacheAsync(cancellationToken);
        return _cache.TryGetValue(key, out var value) ? value : fallback;
    }

    /// <summary>Returns the service tax percentage stored in the settings.</summary>
    public async Task<decimal> GetTaxPercentageAsync(CancellationToken cancellationToken = default)
    {
        var raw = await GetAsync(TaxPercentageKey, "12.24", cancellationToken);
        return decimal.TryParse(raw, out var value) ? value : 12.24m;
    }

    /// <summary>Returns the currency symbol stored in the settings.</summary>
    public async Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default)
        => await GetAsync(CurrencyKey, "Rs", cancellationToken);

    /// <summary>Returns the number of days after which an unpaid bill marks the connection overdue.</summary>
    public async Task<int> GetOverdueDaysAsync(CancellationToken cancellationToken = default)
    {
        var raw = await GetAsync(OverdueDaysKey, "30", cancellationToken);
        return int.TryParse(raw, out var value) && value > 0 ? value : 30;
    }

    /// <summary>Saves a setting value and refreshes the cache.</summary>
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null)
        {
            _db.Settings.Add(new Setting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await EnsureCacheAsync(cancellationToken, forceRefresh: true);
    }

    /// <summary>Refreshes the in memory cache when it is older than one minute.</summary>
    private async Task EnsureCacheAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        lock (_cacheLock)
        {
            if (!forceRefresh && (DateTime.Now - _cachedAt).TotalSeconds < 60 && _cache.Count > 0)
            {
                return;
            }
        }

        var rows = await _db.Settings.AsNoTracking().ToListAsync(cancellationToken);

        lock (_cacheLock)
        {
            _cache.Clear();
            foreach (var row in rows)
            {
                _cache[row.Key] = row.Value;
            }

            _cachedAt = DateTime.Now;
        }
    }
}
