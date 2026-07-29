using System.Globalization;

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
        ["seatingno"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["seating number"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["seat_no"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["seat no"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["رقم الجلوس"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["رقم_الجلوس"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["رقم جلوس"] = nameof(ParsedStudentResultRow.SeatingNo),
        ["الجلوس"] = nameof(ParsedStudentResultRow.SeatingNo),

        ["arabic_name"] = nameof(ParsedStudentResultRow.ArabicName),
        ["arabicname"] = nameof(ParsedStudentResultRow.ArabicName),
        ["name"] = nameof(ParsedStudentResultRow.ArabicName),
        ["student name"] = nameof(ParsedStudentResultRow.ArabicName),
        ["student_name"] = nameof(ParsedStudentResultRow.ArabicName),
        ["اسم الطالب"] = nameof(ParsedStudentResultRow.ArabicName),
        ["اسم_الطالب"] = nameof(ParsedStudentResultRow.ArabicName),
        ["الاسم"] = nameof(ParsedStudentResultRow.ArabicName),
        ["اسم الطالب رباعي"] = nameof(ParsedStudentResultRow.ArabicName),
        ["اسم الطالب رباعى"] = nameof(ParsedStudentResultRow.ArabicName),

        ["total_degree"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["totaldegree"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["total degree"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["degree"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["score"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["total"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["total_score"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["total score"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["المجموع"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["المجموع الكلي"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["مجموع"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["الدرجة"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["درجة"] = nameof(ParsedStudentResultRow.TotalDegree),
        ["المجموع_الكلي"] = nameof(ParsedStudentResultRow.TotalDegree),

        ["student_case_desc"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["studentcasedesc"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["student case"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["student_case"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["case"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["status"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["student status"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["student_status"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["حالة الطالب"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["حالة_الطالب"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["الحالة"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
        ["حالة"] = nameof(ParsedStudentResultRow.StudentCaseDesc),
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
            errors.Add(Err(1, "Header", "INVALID",
                "Missing required columns. Expected: seating_no, arabic_name, total_degree, student_case_desc (or Arabic equivalents like رقم الجلوس, اسم الطالب, المجموع, حالة الطالب)."));
            return (rows, errors);
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
        for (var rowNum = headerRow.RowNumber() + 1; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (IsEmptyRow(row, columnMap)) continue;

            var seatingNo = NormalizeSeatingNo(GetCell(row, columnMap, "SeatingNo"));
            var arabicName = GetCell(row, columnMap, "ArabicName").Trim();
            var totalDegreeRaw = GetNumericCell(row, columnMap, "TotalDegree");
            var studentCaseDesc = GetCell(row, columnMap, "StudentCaseDesc").Trim();

            if (string.IsNullOrWhiteSpace(seatingNo))
                errors.Add(Err(rowNum, "seating_no", "REQUIRED", "Seating number is required."));
            if (string.IsNullOrWhiteSpace(arabicName))
                errors.Add(Err(rowNum, "arabic_name", "REQUIRED", "Arabic name is required."));
            if (string.IsNullOrWhiteSpace(studentCaseDesc))
                errors.Add(Err(rowNum, "student_case_desc", "REQUIRED", "Student case is required."));

            if (!TryParseDegree(totalDegreeRaw, out var totalDegree))
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

    private static bool TryParseDegree(string raw, out decimal totalDegree)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            totalDegree = 0;
            return false;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out totalDegree))
            return true;

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out totalDegree);
    }

    private static Dictionary<string, int> MapHeaders(ClosedXML.Excel.IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(cell.GetString());
            if (string.IsNullOrWhiteSpace(header)) continue;

            if (HeaderAliases.TryGetValue(header, out var field))
            {
                map[field] = cell.Address.ColumnNumber;
                continue;
            }

            var normalized = header.Replace('_', ' ');
            if (HeaderAliases.TryGetValue(normalized, out field))
                map[field] = cell.Address.ColumnNumber;
        }

        return map;
    }

    private static string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return string.Empty;
        return header.Trim().Trim('\uFEFF').Replace("  ", " ");
    }

    private static string GetCell(ClosedXML.Excel.IXLRow row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var col)) return string.Empty;
        var cell = row.Cell(col);
        if (cell.DataType == ClosedXML.Excel.XLDataType.Number && cell.TryGetValue(out double number))
            return number.ToString(CultureInfo.InvariantCulture);
        return cell.GetFormattedString().Trim();
    }

    private static string GetNumericCell(ClosedXML.Excel.IXLRow row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var col)) return string.Empty;
        var cell = row.Cell(col);
        if (cell.DataType == ClosedXML.Excel.XLDataType.Number)
        {
            if (cell.TryGetValue(out decimal decimalValue))
                return decimalValue.ToString(CultureInfo.InvariantCulture);
            if (cell.TryGetValue(out double doubleValue))
                return doubleValue.ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString().Trim();
    }

    private static bool IsEmptyRow(ClosedXML.Excel.IXLRow row, Dictionary<string, int> columnMap)
    {
        return columnMap.Values.All(col => string.IsNullOrWhiteSpace(row.Cell(col).GetFormattedString()));
    }

    private static Application.DTOs.ImportValidationErrorDto Err(int row, string col, string code, string msg) =>
        new(row, col, code, msg);
}
