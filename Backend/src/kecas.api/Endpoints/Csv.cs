namespace kecas.api.Endpoints;

using System.Globalization;
using System.Text;
using kecas.application.reports;

/// <summary>FR-19: renders a ReportTable as RFC 4180-style CSV.</summary>
internal static class Csv
{
    public static string FromTable(ReportTable table)
    {
        var builder = new StringBuilder();
        AppendRow(builder, table.Columns.Cast<object?>());
        foreach (var row in table.Rows)
        {
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<object?> values)
    {
        builder.AppendLine(string.Join(',', values.Select(Escape)));
    }

    private static string Escape(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        return text.Contains('"') || text.Contains(',') || text.Contains('\r') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
