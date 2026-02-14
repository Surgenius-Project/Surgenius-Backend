using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Api.Models;

/// <summary>
/// Represents a 3D model generated from a scan (tumor and liver models).
/// </summary>
public class ThreeDModel
{
    [Key]
    public int ModelID { get; set; }

    [Required]
    [ForeignKey(nameof(Scan))]
    public int ScanID { get; set; }

    [MaxLength(500)]
    public string? TumorModelPath { get; set; }

    [MaxLength(500)]
    public string? LiverModelPath { get; set; }

    // Navigation properties
    public Scan Scan { get; set; } = null!;
}
