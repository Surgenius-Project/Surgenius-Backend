using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Cases;

namespace Surgenius.Application.Interfaces.Cases;

public interface ICaseService
{
    Task<ApiResponse<CaseDto>> CreateCaseAsync(Guid userId, CreateCaseDto request);
    Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId);
    Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid userId, Guid caseId);
}
