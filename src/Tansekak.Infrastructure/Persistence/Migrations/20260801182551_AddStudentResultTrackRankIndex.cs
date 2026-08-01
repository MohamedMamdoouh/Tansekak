using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentResultTrackRankIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_AdmissionYearId_Track_TotalDegree_SeatingNo",
                table: "StudentResults",
                columns: new[] { "AdmissionYearId", "Track", "TotalDegree", "SeatingNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentResults_AdmissionYearId_Track_TotalDegree_SeatingNo",
                table: "StudentResults");
        }
    }
}
