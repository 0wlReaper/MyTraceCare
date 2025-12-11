using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTraceCare.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPageIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataDate",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "FrameNumber",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Alerts");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "FrameIndex",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrameIndex",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Alerts");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Alerts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataDate",
                table: "Alerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameNumber",
                table: "Alerts",
                type: "int",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Alerts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
