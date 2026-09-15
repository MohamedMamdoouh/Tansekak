using ClosedXML.Excel;
using Tansekak.Application.Common;
using Tansekak.Infrastructure.Import;

namespace Tansekak.Infrastructure.Tests;

public class StudentResultExcelParserTests
{
    [Fact]
    public void Parse_valid_workbook_returns_rows()
    {
        using var stream = CreateWorkbook([
            ("2410001", "طالب أ", "390", "علمي علوم"),
            ("2410002", "طالب ب", "380", "علمي علوم"),
        ]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal("2410001", rows[0].SeatingNo);
        Assert.Equal(390, rows[0].TotalDegree);
    }

    [Fact]
    public void Parse_missing_required_column_returns_error()
    {
        using var stream = CreateWorkbookWithHeaders(["seating_no", "arabic_name", "total_degree"]);

        var (_, errors) = StudentResultExcelParser.Parse(stream);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.ErrorCode == ApiErrorCodes.MissingColumn);
    }

    [Fact]
    public void Parse_invalid_total_degree_returns_row_error()
    {
        using var stream = CreateWorkbook([
            ("2410001", "طالب أ", "abc", "علمي علوم"),
        ]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(rows);
        Assert.Contains(errors, e => e.ErrorCode == ApiErrorCodes.TotalDegreeInvalid);
    }

    [Fact]
    public void Validate_rejects_arabic_name_longer_than_max_length()
    {
        using var stream = CreateWorkbook([
            ("2410001", new string('أ', StudentResultExcelParser.MaxArabicNameLength + 1), "390", "علمي علوم"),
        ]);

        var validation = StudentResultExcelParser.Validate(stream);

        Assert.Contains(validation.Errors, e => e.ErrorCode == ApiErrorCodes.FieldTooLong);
        Assert.Equal(0, validation.ValidRowCount);
    }

    [Fact]
    public void Validate_and_enumerate_rows_return_same_data()
    {
        using var stream = CreateWorkbook([
            ("2410001", "طالب أ", "390", "علمي علوم"),
            ("2410002", "طالب ب", "380.5", "علمي علوم"),
        ]);

        var validation = StudentResultExcelParser.Validate(stream);
        Assert.Empty(validation.Errors);
        Assert.Equal(2, validation.ValidRowCount);

        stream.Position = 0;
        var enumerated = StudentResultExcelParser.EnumerateRows(stream).ToList();

        Assert.Equal(2, enumerated.Count);
        Assert.Equal("2410001", enumerated[0].SeatingNo);
        Assert.Equal(380.5m, enumerated[1].TotalDegree);
    }

    [Fact]
    public void Validate_detects_duplicate_seating_numbers()
    {
        using var stream = CreateWorkbook([
            ("2410001", "طالب أ", "390", "علمي علوم"),
            ("2410001", "طالب ب", "380", "علمي علوم"),
        ]);

        var validation = StudentResultExcelParser.Validate(stream);

        Assert.Contains(validation.Errors, e => e.ErrorCode == ApiErrorCodes.Duplicate);
    }

    private static MemoryStream CreateWorkbook(IEnumerable<(string SeatingNo, string Name, string Total, string CaseDesc)> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Results");
        sheet.Cell(1, 1).Value = "seating_no";
        sheet.Cell(1, 2).Value = "arabic_name";
        sheet.Cell(1, 3).Value = "total_degree";
        sheet.Cell(1, 4).Value = "student_case_desc";

        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.SeatingNo;
            sheet.Cell(rowIndex, 2).Value = row.Name;
            sheet.Cell(rowIndex, 3).Value = row.Total;
            sheet.Cell(rowIndex, 4).Value = row.CaseDesc;
            rowIndex++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateWorkbookWithHeaders(IEnumerable<string> headers)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Results");
        var column = 1;
        foreach (var header in headers)
        {
            sheet.Cell(1, column++).Value = header;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
