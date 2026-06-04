using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;

public class AnalysisResult
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }

    // AI Classification
    public int StageNumeric { get; set; } // 0, 1, 2, 3...
    public string StageLabel { get; set; } = string.Empty; // "Stage I", "Stage II"...
    public double Confidence { get; set; } // 0.0 to 1.0
    public long TumorAreaPixels { get; set; }

    // Visuals
    public string? OriginalImagePath { get; set; } // AI generated 4-image grid
    public string? MaskPath { get; set; }      // AI generated mask
    public string? HighlightedPath { get; set; } // Original image with mask overlay
    public string? Model3DPath { get; set; }     // Future 3D model file path

    // Navigation
    public Scan Scan { get; set; } = null!;
}
