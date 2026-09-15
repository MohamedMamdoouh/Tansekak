using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tansekak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearStudentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"StudentResults\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
