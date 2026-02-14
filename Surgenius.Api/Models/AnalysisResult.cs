using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Api.Models;

public class AnalysisResult
{
    [Key]
    public int ResultID { get; set; }

    [Required]
    [ForeignKey(nameof(Scan))]
    public int ScanID { get; set; }

    [MaxLength(50)]
    public string? TumorStage { get; set; }

    [MaxLength(500)]
    public string? MaskPath { get; set; }

    [MaxLength(100)]
    public string? TumorSize { get; set; }

    [Range(0, 1)]
    public decimal? ConfidenceScore { get; set; }


    public Scan Scan { get; set; } = null!;
}
