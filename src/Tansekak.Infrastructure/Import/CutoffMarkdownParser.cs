using System.Globalization;
using System.Text.RegularExpressions;
using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Import;

public static partial class CutoffMarkdownParser
{
    public static (List<ParsedCutoffRow> Rows, List<ImportValidationErrorDto> Errors) Parse(Stream stream)
    {
        var rows = new List<ParsedCutoffRow>();
        var errors = new List<ImportValidationErrorDto>();
        using var reader = new StreamReader(stream, leaveOpen: true);

        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('|'))
                continue;

            if (IsSeparatorRow(trimmed) || IsHeaderRow(trimmed))
                continue;

            var cells = trimmed.Trim('|').Split('|', StringSplitOptions.None)
                .Select(c => c.Trim())
                .ToArray();

            if (cells.Length < 2)
            {
                errors.Add(Error(lineNumber, "الكلية", "INVALID_ROW", "Expected two table columns."));
                continue;
            }

            var label = cells[0];
            var scoreText = cells[1];

            if (string.IsNullOrWhiteSpace(label))
            {
                errors.Add(Error(lineNumber, "الكلية", "REQUIRED", "College name is required."));
                continue;
            }

            if (LeadingScore().IsMatch(label))
            {
                errors.Add(Error(lineNumber, "الكلية", "MALFORMED", "College column contains a score value."));
                continue;
            }

            if (!decimal.TryParse(scoreText, NumberStyles.Any, CultureInfo.InvariantCulture, out var score))
            {
                errors.Add(Error(lineNumber, "الحد الأدنى", "INVALID", "Cutoff score must be a number."));
                continue;
            }

            rows.Add(new ParsedCutoffRow(lineNumber, label, score));
        }

        return (rows, errors);
    }

    private static bool IsSeparatorRow(string line) =>
        line.Contains("---", StringComparison.Ordinal);

    private static bool IsHeaderRow(string line) =>
        line.Contains("الكلية", StringComparison.OrdinalIgnoreCase)
        || line.Contains("الحد", StringComparison.OrdinalIgnoreCase);

    private static ImportValidationErrorDto Error(int row, string column, string code, string message) =>
        new(row, column, code, message);

    [GeneratedRegex(@"^\d+(?:\.\d+)?\s")]
    private static partial Regex LeadingScore();
}
