using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmissionYearId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportedCount = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_AdmissionYears_AdmissionYearId",
                        column: x => x.AdmissionYearId,
                        principalTable: "AdmissionYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_AdmissionYearId",
                table: "ImportJobs",
                column: "AdmissionYearId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedAtUtc",
                table: "ImportJobs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");
        }
    }
}
