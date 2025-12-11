using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTraceCare.Migrations
{
    /// <inheritdoc />
    public partial class ClinicianEnters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Alerts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Alerts");
        }
    }
}
