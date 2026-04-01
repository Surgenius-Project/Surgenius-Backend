using Surgenius.Application.DTOs.Scans;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces.Scans;

/// <summary>
/// Defines the contract for scan-related business operations.
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Uploads a scan file. Only Doctors may call this.
    /// Verifies that the Case exists and belongs to the calling Doctor.
    /// </summary>
    Task<ApiResponse<ScanReadDto>> UploadScanAsync(Guid doctorId, UploadScanDto dto);

    /// <summary>
    /// Returns all scans for a case.
    /// - Doctors: must own the case.
    /// - Students: must be linked to the Doctor who owns the case.
    /// </summary>
    Task<ApiResponse<IEnumerable<ScanReadDto>>> GetScansByCaseAsync(Guid userId, bool isDoctor, Guid caseId);
}
