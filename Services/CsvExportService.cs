using System.Text;

namespace NexusServiceMarketingSystem.Services;

/// <summary>
/// Builds CSV exports for the reports and the advanced search screens.
/// Every value is escaped so a comma or a quote inside the data cannot break the file.
/// </summary>
public class CsvExportService
{
    /// <summary>Creates the CSV header line.</summary>
    public string BuildHeader(IEnumerable<string> columns) => string.Join(",", columns.Select(Escape));

    /// <summary>Creates one CSV row.</summary>
    public string BuildRow(IEnumerable<object?> values) => string.Join(",", values.Select(v => Escape(v?.ToString() ?? string.Empty)));

    /// <summary>Combines a header and the rows into one downloadable document.</summary>
    public string Build(IEnumerable<string> columns, IEnumerable<IEnumerable<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(BuildHeader(columns));

        foreach (var row in rows)
        {
            builder.AppendLine(BuildRow(row));
        }

        return builder.ToString();
    }

    /// <summary>Escapes a single CSV value.</summary>
    private static string Escape(string value)
    {
        var sanitized = value.Replace("\"", "\"\"");

        // Values containing a separator, a quote or a line break have to be quoted.
        return sanitized.Contains(',') || sanitized.Contains('"') || sanitized.Contains('\n') || sanitized.Contains('\r')
            ? $"\"{sanitized}\""
            : sanitized;
    }
}
