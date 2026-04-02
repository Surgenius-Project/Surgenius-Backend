using System.Text.Json.Serialization;

namespace Surgenius.Application.DTOs.Scans;

/// <summary>
/// DTO for uploading a scan file.
/// The controller adapts IFormFile into these primitives before calling the service,
/// keeping the Application layer free of ASP.NET Core dependencies.
/// </summary>
public class UploadScanDto
{
    /// <summary>The raw file content stream.</summary>
    [JsonIgnore]
    public required Stream FileStream { get; set; }

    /// <summary>Original filename (used to preserve the extension).</summary>
    public required string FileName { get; set; }

    public Guid CaseId { get; set; }

    /// <summary>
    /// Optional scan type (e.g. "CT", "MRI", "X-Ray"). Defaults to "General" if not provided.
    /// </summary>
    public string? ScanType { get; set; }
}

/// <summary>
/// DTO returned when reading scan data.
/// </summary>
public class ScanReadDto
{
    public Guid Id { get; set; }
    public string ScanPath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string ScanType { get; set; } = string.Empty;
}
