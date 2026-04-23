using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Surgenius.Application.Interfaces.Email;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Surgenius.Application.DTOs.Auth.Login;
using Surgenius.Application.DTOs.Auth.Password;
using Surgenius.Application.DTOs.Auth.Register;
using Surgenius.Application.DTOs.Auth.Responses;
using Surgenius.Application.DTOs.Auth.Roles;

namespace Surgenius.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        ITokenService tokenService,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
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
                return ApiResponse<RegisterResponseDto>.Failure("The student access code is incorrect.");

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

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        var otpCode = new Random().Next(1000, 9999).ToString();
        user.OtpCode = otpCode;
        user.OtpExpiry = System.DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        await _emailService.SendEmailAsync(user.Email, "Reset Password Verification Code", $"Your password reset verification code is: {otpCode}");

        // TODO: Remove OTP from response body before production — included here for testing only.
        return ApiResponse<string>.Success(otpCode, "Verification code has been sent to your email.");
    }

    public async Task<ApiResponse<string>> VerifyCodeAsync(VerifyCodeRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        if (user.OtpCode != request.Code || user.OtpExpiry < System.DateTime.UtcNow)
        {
            return ApiResponse<string>.Failure("Invalid or expired verification code.");
        }

        user.OtpCode = null;
        user.OtpExpiry = null;
        await _userManager.UpdateAsync(user);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return ApiResponse<string>.Success(encodedToken, "Code verified successfully.");
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return ApiResponse<string>.Failure("Invalid token format.");
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.Failure("Password reset failed.", errors);
        }

        return ApiResponse<string>.Success("Password reset successfully.");
    }

    public async Task<ApiResponse<string>> AssignRoleAsync(AssignRoleRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.Failure("Failed to assign role.", errors);
        }

        return ApiResponse<string>.Success($"Role '{request.RoleName}' assigned successfully.");
    }

    

    private async Task<ApiResponse<AuthResponseDto>> GenerateAuthResponse(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "No Role";

        var token = _tokenService.GenerateToken(user, primaryRole);

        var response = new AuthResponseDto
        {
            Id = user.Id,
            Token = token,
            FullName = user.FullName,
            Email = user.Email!,
            Roles = roles.ToList(),
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
