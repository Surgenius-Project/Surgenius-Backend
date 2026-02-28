using Surgenius.Application.Models.DTOs.Auth;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
}
