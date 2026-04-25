using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.DTOs.Cases;

/// <summary>
/// Payload for creating a new case. All patient fields except Description are required.
/// </summary>
public class CreateCaseDto
{
    [Required]
    public required string CaseType { get; set; }

    // ── Patient details ──────────────────────────────────────────────────────
    [Required]
    public required string PatientName { get; set; }

    [Required, Range(0, 150)]
    public int PatientAge { get; set; }

    /// <summary>Accepted values: "Male", "Female", "Other"</summary>
    [Required]
    public required string PatientGender { get; set; }

    [Required, Phone]
    public required string PatientPhone { get; set; }

    /// <summary>Optional free-text description / chief complaint.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Lightweight case summary returned when listing all cases for a user.
/// </summary>
public class CaseDto
{
    public Guid Id { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }

    // Patient details
    public string PatientName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LatestStage { get; set; }
}

/// <summary>
/// Lightweight scan summary embedded inside a case detail response.
/// </summary>
public class ScanSummaryDto
{
    public Guid Id { get; set; }
    public string ScanPath { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}

/// <summary>
/// Full case details including the list of associated scans.
/// Returned by GetCaseByIdAsync for both Doctors and linked Students.
/// </summary>
public class CaseDetailDto
{
    public Guid Id { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }

    // Patient details
    public string PatientName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<ScanSummaryDto> Scans { get; set; } = new();
}
