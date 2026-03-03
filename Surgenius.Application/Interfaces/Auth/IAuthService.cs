using Surgenius.Application.Models.DTOs.Auth.Email;
using Surgenius.Application.Models.DTOs.Auth.Responses;
using Surgenius.Application.Models.DTOs.Auth.Refresh;
using Surgenius.Application.Models.DTOs.Auth.Login;
using Surgenius.Application.Models.DTOs.Auth.Register;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<ApiResponse<string>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ApiResponse<bool>> RevokeTokenAsync(string userId);
    Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailRequestDto request);
    Task<ApiResponse<string>> ResendConfirmationEmailAsync(ResendEmailConfirmationDto request);
}
