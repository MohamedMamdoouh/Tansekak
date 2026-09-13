using ClosedXML.Excel;
using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Import;

public sealed record ParsedStudentResultRow(
    int RowNumber,
    string SeatingNo,
    string ArabicName,
    decimal TotalDegree,
    string StudentCaseDesc);

public static class StudentResultExcelParser
{
    private static readonly string[] RequiredHeaders =
    [
        "seating_no",
        "arabic_name",
        "total_degree",
        "student_case_desc"
    ];

    public static (List<ParsedStudentResultRow> Rows, List<ImportValidationErrorDto> Errors) Parse(Stream stream)
    {
        var errors = new List<ImportValidationErrorDto>();
        var rows = new List<ParsedStudentResultRow>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            errors.Add(Err(0, "File", "EMPTY", "Workbook contains no worksheets."));
            return (rows, errors);
        }

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
        {
            errors.Add(Err(0, "File", "EMPTY", "Worksheet contains no data rows."));
            return (rows, errors);
        }

        var columnMap = BuildColumnMap(headerRow, errors);
        if (errors.Count > 0)
            return (rows, errors);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
        for (var rowNumber = headerRow.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
                continue;

            var seatingNo = GetCellString(row, columnMap["seating_no"]);
            var arabicName = GetCellString(row, columnMap["arabic_name"]);
            var totalDegreeText = GetCellString(row, columnMap["total_degree"]);
            var studentCaseDesc = GetCellString(row, columnMap["student_case_desc"]);

            if (string.IsNullOrWhiteSpace(seatingNo)
                && string.IsNullOrWhiteSpace(arabicName)
                && string.IsNullOrWhiteSpace(totalDegreeText)
                && string.IsNullOrWhiteSpace(studentCaseDesc))
                continue;

            if (string.IsNullOrWhiteSpace(seatingNo))
                errors.Add(Err(rowNumber, "seating_no", "REQUIRED", "Seating number is required."));
            if (string.IsNullOrWhiteSpace(arabicName))
                errors.Add(Err(rowNumber, "arabic_name", "REQUIRED", "Arabic name is required."));
            if (string.IsNullOrWhiteSpace(studentCaseDesc))
                errors.Add(Err(rowNumber, "student_case_desc", "REQUIRED", "Student case description is required."));
            if (!TryParseDecimal(totalDegreeText, out var totalDegree))
                errors.Add(Err(rowNumber, "total_degree", "INVALID", "Total degree must be a valid number."));
            else if (totalDegree < 0)
                errors.Add(Err(rowNumber, "total_degree", "INVALID", "Total degree must be zero or greater."));

            if (errors.Any(e => e.RowNumber == rowNumber))
                continue;

            rows.Add(new ParsedStudentResultRow(
                rowNumber,
                seatingNo.Trim(),
                arabicName.Trim(),
                totalDegree,
                studentCaseDesc.Trim()));
        }

        if (rows.Count == 0 && errors.Count == 0)
            errors.Add(Err(0, "File", "EMPTY", "File contains no data rows."));

        return (rows, errors);
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow, List<ImportValidationErrorDto> errors)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
                map[header] = cell.Address.ColumnNumber;
        }

        foreach (var required in RequiredHeaders)
        {
            if (!map.ContainsKey(required))
                errors.Add(Err(headerRow.RowNumber(), required, "MISSING_COLUMN", $"Required column '{required}' was not found."));
        }

        return map;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().ToLowerInvariant().Replace(' ', '_');

    private static string GetCellString(IXLRow row, int columnNumber) =>
        row.Cell(columnNumber).GetFormattedString().Trim();

    private static bool TryParseDecimal(string value, out decimal result)
    {
        if (decimal.TryParse(value, out result))
            return true;

        return decimal.TryParse(
            value.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out result);
    }

    private static ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}
