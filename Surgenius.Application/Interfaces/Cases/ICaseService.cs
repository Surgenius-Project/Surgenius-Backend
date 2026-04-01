using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Cases;

namespace Surgenius.Application.Interfaces.Cases;

public interface ICaseService
{
    Task<ApiResponse<CaseDto>> CreateCaseAsync(Guid userId, CreateCaseDto request);
    Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId);

    /// <summary>
    /// Returns full case details including scans.
    /// Doctors must own the case; Students must be linked to the Doctor who owns it.
    /// </summary>
    Task<ApiResponse<CaseDetailDto>> GetCaseByIdAsync(Guid userId, bool isDoctor, Guid caseId);
}
