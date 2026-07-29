using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyAllowedTracksAndStudentTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Track",
                table: "StudentResults",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedTracks",
                table: "Faculties",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE "Faculties" SET "AllowedTracks" = '[1,2]' WHERE "Id" = 1;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 2;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 3;
                UPDATE "Faculties" SET "AllowedTracks" = '[2,1]' WHERE "Id" = 5;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 6;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 8;
                UPDATE "Faculties" SET "AllowedTracks" = '[2,1]' WHERE "Id" = 9;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 10;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 12;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 13;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 14;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 15;
                UPDATE "Faculties" SET "AllowedTracks" = '[2]' WHERE "Id" = 16;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 18;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 20;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 21;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 22;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 23;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 24;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 25;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 26;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 27;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 28;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,2,1]' WHERE "Id" = 29;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 31;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 32;
                UPDATE "Faculties" SET "AllowedTracks" = '[3,1]' WHERE "Id" = 33;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 34;
                UPDATE "Faculties" SET "AllowedTracks" = '[1]' WHERE "Id" = 36;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Track",
                table: "StudentResults");

            migrationBuilder.DropColumn(
                name: "AllowedTracks",
                table: "Faculties");
        }
    }
}
