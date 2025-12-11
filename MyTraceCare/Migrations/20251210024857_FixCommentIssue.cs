using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTraceCare.Migrations
{
    /// <inheritdoc />
    public partial class FixCommentIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientComments_Alerts_AlertId",
                table: "PatientComments");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PatientDevices",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "AlertId",
                table: "PatientComments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataDate",
                table: "PatientComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientComments_Alerts_AlertId",
                table: "PatientComments",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientComments_Alerts_AlertId",
                table: "PatientComments");

            migrationBuilder.DropColumn(
                name: "DataDate",
                table: "PatientComments");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PatientDevices",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AlertId",
                table: "PatientComments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientComments_Alerts_AlertId",
                table: "PatientComments",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
