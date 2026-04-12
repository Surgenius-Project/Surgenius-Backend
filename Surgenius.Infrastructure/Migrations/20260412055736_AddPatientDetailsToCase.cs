using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surgenius.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDetailsToCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientAge",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PatientGender",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientPhone",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PatientAge",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PatientGender",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PatientPhone",
                table: "Cases");
        }
    }
}
