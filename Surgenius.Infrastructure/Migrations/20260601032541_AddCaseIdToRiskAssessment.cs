using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surgenius.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseIdToRiskAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CaseId",
                table: "RiskAssessments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_CaseId",
                table: "RiskAssessments",
                column: "CaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_RiskAssessments_Cases_CaseId",
                table: "RiskAssessments",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RiskAssessments_Cases_CaseId",
                table: "RiskAssessments");

            migrationBuilder.DropIndex(
                name: "IX_RiskAssessments_CaseId",
                table: "RiskAssessments");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "RiskAssessments");
        }
    }
}
