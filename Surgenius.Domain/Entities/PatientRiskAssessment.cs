using System;

namespace Surgenius.Domain.Models;

/// <summary>
/// Domain model for storing Patient Clinical Risk Assessment details
/// and the corresponding AI evaluation results.
/// </summary>
public class PatientRiskAssessment
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Case? Case { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 11 Clinical Lab Metrics (Inputs)
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public double TotalBilirubin { get; set; }
    public double DirectBilirubin { get; set; }
    public int AlkalinePhosphotase { get; set; }
    public int AlamineAminotransferase { get; set; }
    public int AspartateAminotransferase { get; set; }
    public double TotalProtiens { get; set; }
    public double Albumin { get; set; }
    public double AlbuminAndGlobulinRatio { get; set; }

    // Assessment Results (Outputs)
    public string RiskLevel { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool NeedScan { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}
