namespace Surgenius.Application.DTOs.Analysis;

/// <summary>
/// DTO returned when reading analysis result data for a specific scan.
/// </summary>
public class AnalysisReadDto
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }

    // AI Classification
    public int StageNumeric { get; set; }
    public string StageLabel { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public long TumorAreaPixels { get; set; }

    // Visuals
    public string? OriginalImagePath { get; set; }
    public string? MaskPath { get; set; }
    public string? GroundTruthImagePath { get; set; }
    public string? HighlightedPath { get; set; }
    public string? Model3DPath { get; set; }

    // Aliases for frontend consistency as requested
    public double ConfidenceScore => Confidence;
    public string Stage => StageLabel;
    public string? Model3DUrl => Model3DPath;
}
