using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surgenius.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRiskAssessmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalBilirubin = table.Column<double>(type: "float", nullable: false),
                    DirectBilirubin = table.Column<double>(type: "float", nullable: false),
                    AlkalinePhosphotase = table.Column<int>(type: "int", nullable: false),
                    AlamineAminotransferase = table.Column<int>(type: "int", nullable: false),
                    AspartateAminotransferase = table.Column<int>(type: "int", nullable: false),
                    TotalProtiens = table.Column<double>(type: "float", nullable: false),
                    Albumin = table.Column<double>(type: "float", nullable: false),
                    AlbuminAndGlobulinRatio = table.Column<double>(type: "float", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    NeedScan = table.Column<bool>(type: "bit", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAssessments");
        }
    }
}
