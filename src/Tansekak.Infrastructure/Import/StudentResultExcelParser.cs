using System.Globalization;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Import;

public sealed record ParsedStudentResultRow(
    int RowNumber,
    string SeatingNo,
    string ArabicName,
    decimal TotalDegree,
    string StudentCaseDesc);

public sealed record StudentResultExcelValidationResult(
    IReadOnlyList<ImportValidationErrorDto> Errors,
    int ValidRowCount);

public static class StudentResultExcelParser
{
    public const int MaxSeatingNoLength = 20;
    public const int MaxArabicNameLength = 300;
    public const int MaxStudentCaseDescLength = 100;

    private static readonly string[] RequiredHeaders =
    [
        "seating_no",
        "arabic_name",
        "total_degree",
        "student_case_desc"
    ];

    public static (List<ParsedStudentResultRow> Rows, List<ImportValidationErrorDto> Errors) Parse(Stream stream)
    {
        var validation = Validate(stream);
        if (validation.Errors.Count > 0)
            return ([], validation.Errors.ToList());

        stream.Position = 0;
        var rows = EnumerateRows(stream).ToList();
        return (rows, []);
    }

    public static StudentResultExcelValidationResult Validate(Stream stream)
    {
        var errors = new List<ImportValidationErrorDto>();

        try
        {
            using var document = OpenSpreadsheet(stream);
            if (document.WorkbookPart?.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault() is not { } sheet)
            {
                errors.Add(Err(0, "File", ApiErrorCodes.WorkbookEmpty));
                return new StudentResultExcelValidationResult(errors, 0);
            }

            if (document.WorkbookPart.GetPartById(sheet.Id!) is not WorksheetPart worksheetPart)
            {
                errors.Add(Err(0, "File", ApiErrorCodes.WorksheetEmpty));
                return new StudentResultExcelValidationResult(errors, 0);
            }

            var sharedStrings = document.WorkbookPart.SharedStringTablePart?.SharedStringTable;
            Dictionary<string, int>? columnMap = null;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var validRowCount = 0;
            var hasHeader = false;

            using (var reader = OpenXmlReader.Create(worksheetPart))
            {
                while (reader.Read())
                {
                    if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                        continue;

                    var row = (Row)reader.LoadCurrentElement()!;
                    if (!hasHeader)
                    {
                        columnMap = BuildColumnMap(row, sharedStrings, errors);
                        hasHeader = true;
                        if (errors.Count > 0)
                            return new StudentResultExcelValidationResult(errors, 0);
                        continue;
                    }

                    var rowNumber = row.RowIndex is not null
                        ? (int)row.RowIndex.Value
                        : validRowCount + 2;

                    if (IsEmptyRow(row, columnMap!, sharedStrings))
                        continue;

                    var rowErrors = ValidateDataRow(rowNumber, row, columnMap!, sharedStrings, seen);
                    if (rowErrors.Count > 0)
                    {
                        errors.AddRange(rowErrors);
                        continue;
                    }

                    validRowCount++;
                }
            }

            if (!hasHeader)
            {
                errors.Add(Err(0, "File", ApiErrorCodes.WorksheetEmpty));
                return new StudentResultExcelValidationResult(errors, 0);
            }

            if (validRowCount == 0 && errors.Count == 0)
                errors.Add(Err(0, "File", ApiErrorCodes.FileNoDataRows));

            return new StudentResultExcelValidationResult(errors, validRowCount);
        }
        catch (Exception ex) when (IsInvalidWorkbookException(ex))
        {
            errors.Add(Err(0, "File", ApiErrorCodes.ImportFileInvalid));
            return new StudentResultExcelValidationResult(errors, 0);
        }
    }

    public static IEnumerable<ParsedStudentResultRow> EnumerateRows(Stream stream)
    {
        using var document = OpenSpreadsheet(stream);
        var sheet = document.WorkbookPart!.Workbook!.Sheets!.Elements<Sheet>().First();
        var worksheetPart = (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id!);
        var sharedStrings = document.WorkbookPart.SharedStringTablePart?.SharedStringTable;
        Dictionary<string, int>? columnMap = null;
        var hasHeader = false;

        using var reader = OpenXmlReader.Create(worksheetPart);
        while (reader.Read())
        {
            if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                continue;

            var row = (Row)reader.LoadCurrentElement()!;
            if (!hasHeader)
            {
                columnMap = BuildColumnMap(row, sharedStrings, []);
                hasHeader = true;
                continue;
            }

            var rowNumber = row.RowIndex is not null
                ? (int)row.RowIndex.Value
                : 0;

            if (IsEmptyRow(row, columnMap!, sharedStrings))
                continue;

            var seatingNo = GetCellString(row, columnMap!["seating_no"], sharedStrings).Trim();
            var arabicName = GetCellString(row, columnMap!["arabic_name"], sharedStrings).Trim();
            var totalDegreeText = GetCellString(row, columnMap!["total_degree"], sharedStrings).Trim();
            var studentCaseDesc = GetCellString(row, columnMap!["student_case_desc"], sharedStrings).Trim();

            if (string.IsNullOrWhiteSpace(seatingNo)
                && string.IsNullOrWhiteSpace(arabicName)
                && string.IsNullOrWhiteSpace(totalDegreeText)
                && string.IsNullOrWhiteSpace(studentCaseDesc))
                continue;

            if (!TryParseDecimal(totalDegreeText, out var totalDegree))
                continue;

            yield return new ParsedStudentResultRow(
                rowNumber,
                seatingNo,
                arabicName,
                totalDegree,
                studentCaseDesc);
        }
    }

    private static SpreadsheetDocument OpenSpreadsheet(Stream stream) =>
        SpreadsheetDocument.Open(stream, false);

    private static bool IsInvalidWorkbookException(Exception ex) =>
        ex is OpenXmlPackageException
            or FileFormatException
            or InvalidDataException
            or XmlException
            or IOException;

    private static Dictionary<string, int> BuildColumnMap(
        Row headerRow,
        SharedStringTable? sharedStrings,
        List<ImportValidationErrorDto> errors)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.Elements<Cell>())
        {
            var columnIndex = GetColumnIndex(cell.CellReference?.Value);
            if (columnIndex <= 0)
                continue;

            var header = NormalizeHeader(GetCellValue(cell, sharedStrings));
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
                map[header] = columnIndex;
        }

        foreach (var required in RequiredHeaders)
        {
            if (!map.ContainsKey(required))
            {
                errors.Add(Err(
                    (int)(headerRow.RowIndex?.Value ?? 1),
                    required,
                    ApiErrorCodes.MissingColumn,
                    ArabicErrorCatalog.GetMessage(ApiErrorCodes.MissingColumn, required)));
            }
        }

        return map;
    }

    private static List<ImportValidationErrorDto> ValidateDataRow(
        int rowNumber,
        Row row,
        IReadOnlyDictionary<string, int> columnMap,
        SharedStringTable? sharedStrings,
        HashSet<string> seen)
    {
        var errors = new List<ImportValidationErrorDto>();

        var seatingNo = GetCellString(row, columnMap["seating_no"], sharedStrings);
        var arabicName = GetCellString(row, columnMap["arabic_name"], sharedStrings);
        var totalDegreeText = GetCellString(row, columnMap["total_degree"], sharedStrings);
        var studentCaseDesc = GetCellString(row, columnMap["student_case_desc"], sharedStrings);

        if (string.IsNullOrWhiteSpace(seatingNo))
            errors.Add(Err(rowNumber, "seating_no", ApiErrorCodes.Required));
        else if (seatingNo.Length > MaxSeatingNoLength)
            errors.Add(FieldTooLong(rowNumber, "seating_no", MaxSeatingNoLength));

        if (string.IsNullOrWhiteSpace(arabicName))
            errors.Add(Err(rowNumber, "arabic_name", ApiErrorCodes.Required));
        else if (arabicName.Length > MaxArabicNameLength)
            errors.Add(FieldTooLong(rowNumber, "arabic_name", MaxArabicNameLength));

        if (string.IsNullOrWhiteSpace(studentCaseDesc))
            errors.Add(Err(rowNumber, "student_case_desc", ApiErrorCodes.Required));
        else if (studentCaseDesc.Length > MaxStudentCaseDescLength)
            errors.Add(FieldTooLong(rowNumber, "student_case_desc", MaxStudentCaseDescLength));

        if (!TryParseDecimal(totalDegreeText, out var totalDegree))
            errors.Add(Err(rowNumber, "total_degree", ApiErrorCodes.TotalDegreeInvalid));
        else if (totalDegree < 0)
            errors.Add(Err(rowNumber, "total_degree", ApiErrorCodes.TotalDegreeNegative));

        if (errors.Count > 0)
            return errors;

        if (!seen.Add(seatingNo.Trim()))
            errors.Add(Err(rowNumber, "seating_no", ApiErrorCodes.Duplicate));

        return errors;
    }

    private static bool IsEmptyRow(
        Row row,
        IReadOnlyDictionary<string, int> columnMap,
        SharedStringTable? sharedStrings)
    {
        var seatingNo = GetCellString(row, columnMap["seating_no"], sharedStrings);
        var arabicName = GetCellString(row, columnMap["arabic_name"], sharedStrings);
        var totalDegreeText = GetCellString(row, columnMap["total_degree"], sharedStrings);
        var studentCaseDesc = GetCellString(row, columnMap["student_case_desc"], sharedStrings);

        return string.IsNullOrWhiteSpace(seatingNo)
            && string.IsNullOrWhiteSpace(arabicName)
            && string.IsNullOrWhiteSpace(totalDegreeText)
            && string.IsNullOrWhiteSpace(studentCaseDesc);
    }

    private static string GetCellString(Row row, int columnNumber, SharedStringTable? sharedStrings)
    {
        var cell = row.Elements<Cell>()
            .FirstOrDefault(c => GetColumnIndex(c.CellReference?.Value) == columnNumber);
        return cell is null ? string.Empty : GetCellValue(cell, sharedStrings).Trim();
    }

    private static string GetCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.CellFormula is not null)
            return cell.CellValue?.InnerText ?? string.Empty;

        var value = cell.CellValue?.InnerText ?? cell.InnerText;
        if (cell.DataType?.Value == CellValues.SharedString && sharedStrings is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0
                && index < sharedStrings.Count())
            {
                return sharedStrings.ElementAt(index).InnerText;
            }
        }

        return value;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
            return -1;

        var columnLetters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (columnLetters.Length == 0)
            return -1;

        var index = 0;
        foreach (var ch in columnLetters)
            index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);

        return index;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().ToLowerInvariant().Replace(' ', '_');

    private static bool TryParseDecimal(string value, out decimal result)
    {
        if (decimal.TryParse(value, out result))
            return true;

        return decimal.TryParse(
            value.Replace(',', '.'),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static ImportValidationErrorDto Err(int row, string col, string code, string? message = null) =>
        new(row, col, code, message ?? ArabicErrorCatalog.GetMessage(code));

    private static ImportValidationErrorDto FieldTooLong(int row, string col, int maxLength) =>
        new(
            row,
            col,
            ApiErrorCodes.FieldTooLong,
            ArabicErrorCatalog.GetMessage(ApiErrorCodes.FieldTooLong, col, maxLength));
}
