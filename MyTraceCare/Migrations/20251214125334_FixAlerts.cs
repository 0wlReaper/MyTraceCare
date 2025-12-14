using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTraceCare.Migrations
{
    /// <inheritdoc />
    public partial class FixAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeverityRank",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeverityRank",
                table: "Alerts");
        }
    }
}
