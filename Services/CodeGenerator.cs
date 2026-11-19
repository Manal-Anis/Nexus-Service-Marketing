namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// Pure identifier formatting helpers (requirement section 4).
/// The class is static and side effect free so it can be covered by unit tests without a database.
/// </summary>
public static class CodeGenerator
{
    /// <summary>Number of digits used by the order serial part (10).</summary>
    public const int OrderSerialDigits = 10;

    /// <summary>Number of digits used by the account serial part (12).</summary>
    public const int AccountSerialDigits = 12;

    /// <summary>Length of the public order identifier.</summary>
    public const int OrderIdLength = 11;

    /// <summary>Length of the public account identifier.</summary>
    public const int AccountIdLength = 16;

    /// <summary>
    /// Builds an 11 character order identifier: one connection type letter followed by a
    /// 10 digit serial number, for example D0000000001.
    /// </summary>
    /// <param name="typeLetter">D = Dial-Up, T = Telephone/Landline, B = Broadband.</param>
    /// <param name="serial">Serial number of this order inside the type letter series.</param>
    public static string FormatOrderId(char typeLetter, long serial)
    {
        if (!char.IsLetter(typeLetter))
        {
            throw new ArgumentException("The order type letter must be an alphabetic character.", nameof(typeLetter));
        }

        if (serial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serial), "The serial number can never be negative.");
        }

        return $"{char.ToUpperInvariant(typeLetter)}{serial.ToString($"D{OrderSerialDigits}")}";
    }

    /// <summary>
    /// Builds a 16 character account identifier: one connection type letter, the 3 digit city
    /// code and a 12 digit serial number, for example B001000000000001.
    /// </summary>
    /// <param name="typeLetter">D = Dial-Up, T = Telephone/Landline, B = Broadband.</param>
    /// <param name="cityCode">Three digit numeric code of the city.</param>
    /// <param name="serial">Serial number of this connection inside the type letter series.</param>
    public static string FormatAccountId(char typeLetter, string cityCode, long serial)
    {
        if (!char.IsLetter(typeLetter))
        {
            throw new ArgumentException("The account type letter must be an alphabetic character.", nameof(typeLetter));
        }

        if (string.IsNullOrWhiteSpace(cityCode) || cityCode.Length != 3 || !cityCode.All(char.IsDigit))
        {
            throw new ArgumentException("The city code must contain exactly three digits.", nameof(cityCode));
        }

        if (serial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serial), "The serial number can never be negative.");
        }

        return $"{char.ToUpperInvariant(typeLetter)}{cityCode}{serial.ToString($"D{AccountSerialDigits}")}";
    }

    /// <summary>Checks that the supplied value is a well formed 11 character order identifier.</summary>
    public static bool IsValidOrderId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != OrderIdLength)
        {
            return false;
        }

        if (!"DTB".Contains(value[0]))
        {
            return false;
        }

        return value[1..].All(char.IsDigit);
    }

    /// <summary>Checks that the supplied value is a well formed 16 character account identifier.</summary>
    public static bool IsValidAccountId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != AccountIdLength)
        {
            return false;
        }

        if (!"DTB".Contains(value[0]))
        {
            return false;
        }

        return value[1..].All(char.IsDigit);
    }

    /// <summary>
    /// Splits an account identifier into its parts so the system can find the city code
    /// without a database round trip.
    /// </summary>
    public static bool TryParseAccountId(string? value, out char typeLetter, out string cityCode, out long serial)
    {
        typeLetter = '\0';
        cityCode = string.Empty;
        serial = 0;

        if (!IsValidAccountId(value))
        {
            return false;
        }

        typeLetter = value![0];
        cityCode = value.Substring(1, 3);
        serial = long.Parse(value.Substring(4));
        return true;
    }
}
