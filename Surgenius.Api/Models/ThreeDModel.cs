using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Api.Models;


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

    public Scan Scan { get; set; } = null!;
}
