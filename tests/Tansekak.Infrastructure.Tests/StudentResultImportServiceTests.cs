using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tansekak.Domain.Entities;
using Tansekak.Infrastructure.Import;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure.Tests;

public class StudentResultImportServiceTests
{
    [Fact]
    public async Task ImportAsync_replaces_existing_results_using_streaming_batches()
    {
        var (db, connection) = TestDbFactory.Create();
        await using (connection)
        await using (db)
        {
            db.AdmissionYears.Add(new AdmissionYear
            {
                Id = 1,
                Year = 2026,
                MaximumScore = 410,
                IsCurrent = true
            });
            db.StudentResults.Add(new StudentResult
            {
                Id = 1,
                AdmissionYearId = 1,
                SeatingNo = "9999999",
                ArabicName = "قديم",
                TotalDegree = 100,
                StudentCaseDesc = "ادبي"
            });
            await db.SaveChangesAsync();

            using var stream = CreateWorkbook([
                ("2410001", "طالب أ", "390", "علمي علوم"),
                ("2410002", "طالب ب", "380", "علمي علوم"),
                ("2410003", "طالب ج", "370", "علمي علوم"),
            ]);

            var service = new StudentResultImportService(
                db,
                new EntityIdAllocator(db),
                NullLogger<StudentResultImportService>.Instance);

            var result = await service.ImportAsync(1, stream, "results.xlsx");

            Assert.True(result.Success);
            Assert.Equal(3, result.ImportedCount);

            var rows = await db.StudentResults
                .Where(x => x.AdmissionYearId == 1)
                .OrderBy(x => x.SeatingNo)
                .ToListAsync();

            Assert.Equal(3, rows.Count);
            Assert.DoesNotContain(rows, x => x.SeatingNo == "9999999");
            Assert.Equal("2410001", rows[0].SeatingNo);
        }
    }

    private static MemoryStream CreateWorkbook(
        IEnumerable<(string SeatingNo, string Name, string Total, string CaseDesc)> rows)
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
}
