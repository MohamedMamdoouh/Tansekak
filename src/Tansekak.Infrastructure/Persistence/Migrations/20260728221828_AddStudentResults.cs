using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmissionYearId = table.Column<int>(type: "int", nullable: false),
                    SeatingNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TotalDegree = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    StudentCaseDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentResults_AdmissionYears_AdmissionYearId",
                        column: x => x.AdmissionYearId,
                        principalTable: "AdmissionYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_AdmissionYearId_SeatingNo",
                table: "StudentResults",
                columns: new[] { "AdmissionYearId", "SeatingNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentResults_SeatingNo",
                table: "StudentResults",
                column: "SeatingNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentResults");
        }
    }
}
