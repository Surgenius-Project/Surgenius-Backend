using Surgenius.Application.Models.DTOs.Auth.Login;
using Surgenius.Application.Models.DTOs.Auth.Register;
using Surgenius.Application.Models.DTOs.Auth.Responses;
using Surgenius.Application.Models.DTOs.Auth.Password;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request);
}
