using Microsoft.AspNetCore.Identity;
using Surgenius.Application.Models.DTOs.Auth.Login;
using Surgenius.Application.Models.DTOs.Auth.Register;
using Surgenius.Application.Models.DTOs.Auth.Password;
using Surgenius.Application.Models.DTOs.Auth.Responses;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Surgenius.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UserType = request.UserType
        };

        if (request.UserType == Domain.Enums.UserType.Doctor)
        {
            user.InviteCode = GenerateUniqueInviteCode(request.FullName);
        }
        else if (request.UserType == Domain.Enums.UserType.Student)
        {
            if (string.IsNullOrEmpty(request.InviteCode))
                return ApiResponse<RegisterResponseDto>.Failure("Student registration requires an invite code.");

            var doctor = await _userManager.Users
                .FirstOrDefaultAsync(u => u.InviteCode == request.InviteCode && u.UserType == Domain.Enums.UserType.Doctor);

            if (doctor == null)
                return ApiResponse<RegisterResponseDto>.Failure("Invalid invite code.");

            user.DoctorId = doctor.Id;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<RegisterResponseDto>.Failure("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(user, request.UserType.ToString());

        // Auto-confirm email on registration
        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, confirmToken);

        var response = new RegisterResponseDto
        {
            UserId = user.Id,
            InviteCode = user.InviteCode
        };

        return ApiResponse<RegisterResponseDto>.Success(response, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return ApiResponse<AuthResponseDto>.Failure("Invalid email or password.");


        return await GenerateAuthResponse(user);
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        // We use GeneratePasswordResetTokenAsync instead of requiring their current password
        // because the user requested ONLY "reset password" directly. Wait, the user prompt was:
        // "make only reset i think i reset only i dont need emailService"
        // "write only new passwoed" - so changing it to use Remove/Add instead of token reset, OR simple reset.
        // Actually, if they are resetting password directly, we can just remove the old one and add the new one.
        
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.Failure("Password reset failed.", errors);
        }

        return ApiResponse<string>.Success("Password reset successfully.");
    }

    

    private async Task<ApiResponse<AuthResponseDto>> GenerateAuthResponse(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "No Role";

        var token = _tokenService.GenerateToken(user, role);

        var response = new AuthResponseDto
        {
            Id = user.Id,
            Token = token,
            FullName = user.FullName,
            Email = user.Email!,
            Role = role,
            InviteCode = user.InviteCode
        };

        return ApiResponse<AuthResponseDto>.Success(response);
    }

    private string GenerateUniqueInviteCode(string fullName)
    {
        var prefix = fullName.Length >= 3 ? fullName.Substring(0, 3).ToUpper() : "DOC";
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{random}";
    }
}
