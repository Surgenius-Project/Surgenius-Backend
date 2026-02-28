using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Models.DTOs.Auth;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
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
                return ApiResponse<AuthResponseDto>.Failure("Student registration requires an invite code.");

            var doctor = await _userManager.Users
                .FirstOrDefaultAsync(u => u.InviteCode == request.InviteCode && u.UserType == Domain.Enums.UserType.Doctor);

            if (doctor == null)
                return ApiResponse<AuthResponseDto>.Failure("Invalid invite code.");

            user.DoctorId = doctor.Id;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<AuthResponseDto>.Failure("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(user, request.UserType.ToString());

        return await GenerateAuthResponse(user);
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return ApiResponse<AuthResponseDto>.Failure("Invalid email or password.");

        return await GenerateAuthResponse(user);
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
