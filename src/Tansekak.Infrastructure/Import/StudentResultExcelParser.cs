namespace Tansekak.Infrastructure.Import;

public sealed record ParsedStudentResultRow(
    int RowNumber,
    string SeatingNo,
    string ArabicName,
    decimal TotalDegree,
    string StudentCaseDesc);

public static class StudentResultExcelParser
{
    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["seating_no"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["arabic_name"] = nameof(ParsedStudentResultRow.ArabicName),
        ["total_degree"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["student_case_desc"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
    };

    public static (List<ParsedStudentResultRow> Rows, List<Application.DTOs.ImportValidationErrorDto> Errors) Parse(Stream stream)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var rows = new List<ParsedStudentResultRow>();
        var errors = new List<Application.DTOs.ImportValidationErrorDto>();

        var headerRow = sheet.FirstRowUsed();
        if (headerRow is null)
        {
            errors.Add(Err(0, "File", "EMPTY", "File contains no data."));
            return (rows, errors);
        }

        var columnMap = MapHeaders(headerRow);
        if (columnMap.Count < 4)
        {
            errors.Add(Err(1, "Header", "INVALID", "Missing required columns: seating_no, arabic_name, total_degree, student_case_desc."));
            return (rows, errors);
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
        for (var rowNum = headerRow.RowNumber() + 1; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (IsEmptyRow(row, columnMap)) continue;

            var seatingNo = NormalizeSeatingNo(GetCell(row, columnMap, "SeatingNo"));
            var arabicName = GetCell(row, columnMap, "ArabicName").Trim();
            var totalDegreeRaw = GetCell(row, columnMap, "TotalDegree").Trim();
            var studentCaseDesc = GetCell(row, columnMap, "StudentCaseDesc").Trim();

            if (string.IsNullOrWhiteSpace(seatingNo))
                errors.Add(Err(rowNum, "seating_no", "REQUIRED", "Seating number is required."));
            if (string.IsNullOrWhiteSpace(arabicName))
                errors.Add(Err(rowNum, "arabic_name", "REQUIRED", "Arabic name is required."));
            if (string.IsNullOrWhiteSpace(studentCaseDesc))
                errors.Add(Err(rowNum, "student_case_desc", "REQUIRED", "Student case is required."));

            if (!decimal.TryParse(totalDegreeRaw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var totalDegree) &&
                !decimal.TryParse(totalDegreeRaw, out totalDegree))
            {
                errors.Add(Err(rowNum, "total_degree", "INVALID", "Total degree must be a number."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(seatingNo) || string.IsNullOrWhiteSpace(arabicName) ||
                string.IsNullOrWhiteSpace(studentCaseDesc))
                continue;

            rows.Add(new ParsedStudentResultRow(rowNum, seatingNo, arabicName, totalDegree, studentCaseDesc));
        }

        return (rows, errors);
    }

    public static string NormalizeSeatingNo(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static Dictionary<string, int> MapHeaders(ClosedXML.Excel.IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (HeaderAliases.TryGetValue(header, out var field))
                map[field] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static string GetCell(ClosedXML.Excel.IXLRow row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var col)) return string.Empty;
        return row.Cell(col).GetFormattedString().Trim();
    }

    private static bool IsEmptyRow(ClosedXML.Excel.IXLRow row, Dictionary<string, int> columnMap)
    {
        return columnMap.Values.All(col => string.IsNullOrWhiteSpace(row.Cell(col).GetFormattedString()));
    }

    private static Application.DTOs.ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}
