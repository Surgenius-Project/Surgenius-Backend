using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Cases;

namespace Surgenius.Application.Interfaces.Cases;

public interface ICaseService
{
    Task<ApiResponse<CaseDto>> CreateCaseAsync(Guid userId, CreateCaseDto request);

    /// <summary>
    /// Returns cases for the calling user.
    /// Admins see ALL cases; Doctors/Students see only their own.
    /// </summary>
    Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId, bool isDoctor, bool isAdmin, string? searchTerm = null, string? stage = null);

    /// <summary>
    /// Toggles the IsInviteCodeActive flag for a Doctor.
    /// </summary>
    Task<ApiResponse<bool>> ToggleStudentAccessAsync(Guid doctorId);

    /// <summary>
    /// Returns full case details including scans.
    /// Admins: unrestricted access.
    /// Doctors: must own the case.
    /// Students: must be linked to the Doctor who owns it.
    /// </summary>
    Task<ApiResponse<CaseDetailDto>> GetCaseByIdAsync(Guid userId, bool isDoctor, bool isAdmin, Guid caseId);

    /// <summary>
    /// Deletes a case and all its associated data (scans, analysis, files).
    /// </summary>
    Task<ApiResponse<bool>> DeleteCaseAsync(Guid userId, Guid caseId);
}
