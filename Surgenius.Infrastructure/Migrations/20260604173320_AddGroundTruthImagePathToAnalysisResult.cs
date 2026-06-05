using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surgenius.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroundTruthImagePathToAnalysisResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroundTruthImagePath",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroundTruthImagePath",
                table: "AnalysisResults");
        }
    }
}
