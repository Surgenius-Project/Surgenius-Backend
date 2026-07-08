using System.Threading.Tasks;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Auth.Login;
using Surgenius.Application.DTOs.Auth.Password;
using Surgenius.Application.DTOs.Auth.Register;
using Surgenius.Application.DTOs.Auth.Roles;
using Surgenius.Application.DTOs.Auth.Responses;

namespace Surgenius.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponse<string>> VerifyCodeAsync(VerifyCodeRequestDto request);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<ApiResponse<string>> AssignRoleAsync(AssignRoleRequestDto request);
    Task<ApiResponse<string>> LogoutAsync();
}
