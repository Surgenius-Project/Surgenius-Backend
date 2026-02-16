using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;


public class ThreeDModel
{
    public Guid Id { get; set; }

    public int ScanID { get; set; }

    public string? TumorModelPath { get; set; }

    public string? LiverModelPath { get; set; }

    public Scan Scan { get; set; } = null!;
}
