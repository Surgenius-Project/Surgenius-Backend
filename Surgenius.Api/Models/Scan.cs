using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Api.Models;

/// <summary>
/// Represents a medical scan associated with a case.
/// </summary>
public class Scan
{
    [Key]
    public int ScanID { get; set; }

    [Required]
    [ForeignKey(nameof(Case))]
    public int CaseID { get; set; }

    [Required]
    [MaxLength(500)]
    public required string ScanPath { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ScanType { get; set; }

    [Required]
    public DateTime UploadDate { get; set; }

    // Navigation properties
    public Case Case { get; set; } = null!;
    public ThreeDModel? ThreeDModel { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
}
