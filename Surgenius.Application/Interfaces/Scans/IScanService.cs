using Surgenius.Application.DTOs.Scans;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces.Scans;

/// <summary>
/// Defines the contract for scan-related business operations.
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Uploads a scan file.
    /// Verifies that the Case exists and belongs to the calling user (if not Admin).
    /// </summary>
    Task<ApiResponse<ScanReadDto>> UploadScanAsync(Guid userId, bool isAdmin, UploadScanDto dto);

    
    /// Returns all scans for a case.
    /// - Admins: all cases.
    /// - Doctors: must own the case.
    /// - Students: must be linked to the Doctor who owns the case.
    /// </summary>
    Task<ApiResponse<IEnumerable<ScanReadDto>>> GetScansByCaseAsync(Guid userId, bool isDoctor, bool isAdmin, Guid caseId);
}
