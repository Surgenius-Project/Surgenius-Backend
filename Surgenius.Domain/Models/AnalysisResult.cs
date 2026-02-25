using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;

public class AnalysisResult
{
  
    public Guid Id { get; set; }

    public Guid ScanId { get; set; }

    public string? TumorStage { get; set; }

    public string? MaskPath { get; set; }

    public string? TumorSize { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public Scan Scan { get; set; } = null!;
}
