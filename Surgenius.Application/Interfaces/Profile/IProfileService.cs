using Surgenius.Application.DTOs.Profile;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces.Profile;

public interface IProfileService
{
    Task<ApiResponse<ProfileReadDto>> GetProfileAsync(Guid userId);
    Task<ApiResponse<ProfileReadDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
    Task<ApiResponse<IEnumerable<StudentDto>>> GetLinkedStudentsAsync(Guid doctorId);
    Task<ApiResponse<bool>> RemoveStudentAsync(Guid doctorId, Guid studentId);
    Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    Task<ApiResponse<string>> GetOrGenerateInviteCodeAsync(Guid doctorId);
}
