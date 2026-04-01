namespace Surgenius.Application.DTOs.Cases;

public class CreateCaseDto
{
    public required string CaseType { get; set; }
}

public class CaseDto
{
    public Guid Id { get; set; }
    public required string CaseType { get; set; }
    public DateTime CreationDate { get; set; }
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
    public required string CaseType { get; set; }
    public DateTime CreationDate { get; set; }
    public List<ScanSummaryDto> Scans { get; set; } = new();
}
