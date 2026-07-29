using ClosedXML.Excel;
using Tansekak.Infrastructure.Import;

namespace Tansekak.UnitTests;

public class StudentResultExcelParserTests
{
    [Fact]
    public void Parse_EnglishHeaders_ReturnsRows()
    {
        using var stream = CreateWorkbook(
            ["seating_no", "arabic_name", "total_degree", "student_case_desc"],
            [["1234567", "محمد احمد", "285.5", "ناجح"]]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Equal("1234567", rows[0].SeatingNo);
        Assert.Equal(285.5m, rows[0].TotalDegree);
    }

    [Fact]
    public void Parse_ArabicHeaders_ReturnsRows()
    {
        using var stream = CreateWorkbook(
            ["رقم الجلوس", "اسم الطالب", "المجموع", "حالة الطالب"],
            [["7654321", "فاطمة حسن", "390", "ناجح"]]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Equal("7654321", rows[0].SeatingNo);
        Assert.Equal(390m, rows[0].TotalDegree);
    }

    [Fact]
    public void Parse_ScoreAbove320_IsAccepted()
    {
        using var stream = CreateWorkbook(
            ["seating_no", "arabic_name", "total_degree", "student_case_desc"],
            [["1111111", "طالب تجريبي", "405", "ناجح"]]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(errors);
        Assert.Equal(405m, rows.Single().TotalDegree);
    }

    [Fact]
    public void Parse_MissingHeaders_ReturnsHeaderError()
    {
        using var stream = CreateWorkbook(
            ["name", "score"],
            [["test", "100"]]);

        var (rows, errors) = StudentResultExcelParser.Parse(stream);

        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("INVALID", errors[0].ErrorCode);
    }

    private static MemoryStream CreateWorkbook(string[] headers, string[][] dataRows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Results");

        for (var col = 0; col < headers.Length; col++)
            sheet.Cell(1, col + 1).Value = headers[col];

        for (var row = 0; row < dataRows.Length; row++)
        {
            for (var col = 0; col < dataRows[row].Length; col++)
                sheet.Cell(row + 2, col + 1).Value = dataRows[row][col];
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
