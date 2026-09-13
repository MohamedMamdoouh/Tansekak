using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionYears_IsCurrent",
                table: "AdmissionYears");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "StudentResults",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "ImportJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntityIdSequences",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NextId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityIdSequences", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionYears_IsCurrent",
                table: "AdmissionYears",
                column: "IsCurrent",
                unique: true,
                filter: "\"IsCurrent\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityIdSequences");

            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionYears_IsCurrent",
                table: "AdmissionYears");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "ImportJobs");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "StudentResults",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionYears_IsCurrent",
                table: "AdmissionYears",
                column: "IsCurrent");
        }
    }
}
