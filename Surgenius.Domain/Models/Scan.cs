using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;


public class Scan
{
    public Guid Id { get; set;}
    public Guid CaseId { get; set; }
    public required string ScanPath { get; set; }
    public required string ScanType { get; set; }

    public DateTime UploadDate { get; set; }

    public Case Case { get; set; } = null!;
    public ThreeDModel? ThreeDModel { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
}
