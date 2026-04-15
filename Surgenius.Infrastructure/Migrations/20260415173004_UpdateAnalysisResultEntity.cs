using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surgenius.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnalysisResultEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "AnalysisResults");

            migrationBuilder.RenameColumn(
                name: "TumorStage",
                table: "AnalysisResults",
                newName: "Model3DPath");

            migrationBuilder.RenameColumn(
                name: "TumorSize",
                table: "AnalysisResults",
                newName: "HighlightedPath");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "AnalysisResults",
                type: "float(18)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "StageLabel",
                table: "AnalysisResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StageNumeric",
                table: "AnalysisResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "TumorAreaPixels",
                table: "AnalysisResults",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "StageLabel",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "StageNumeric",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TumorAreaPixels",
                table: "AnalysisResults");

            migrationBuilder.RenameColumn(
                name: "Model3DPath",
                table: "AnalysisResults",
                newName: "TumorStage");

            migrationBuilder.RenameColumn(
                name: "HighlightedPath",
                table: "AnalysisResults",
                newName: "TumorSize");

            migrationBuilder.AddColumn<decimal>(
                name: "ConfidenceScore",
                table: "AnalysisResults",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
