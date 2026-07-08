using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Domain.Models;
using Surgenius.Domain.Enums;
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
using Microsoft.AspNetCore.Authentication;

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

        var otpCode = new Random().Next(100000, 999999).ToString();
        user.OtpCode = otpCode;
        user.OtpExpiry = System.DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        var htmlBody = $@"
            <div style=""font-family: 'Segoe UI', Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 32px; background-color: #ffffff; border-radius: 12px; border: 1px solid #e5e7eb;"">
                <div style=""text-align: center; margin-bottom: 24px;"">
                    <h1 style=""color: #1e293b; font-size: 24px; margin: 0;"">Surgenius</h1>
                    <p style=""color: #64748b; font-size: 14px; margin-top: 4px;"">Password Reset Request</p>
                </div>
                <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 16px 0;"" />
                <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">
                    Hello <strong>{user.FullName}</strong>,
                </p>
                <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">
                    We received a request to reset your password. Use the verification code below to proceed:
                </p>
                <div style=""text-align: center; margin: 28px 0;"">
                    <span style=""display: inline-block; font-size: 32px; font-weight: 700; letter-spacing: 8px; color: #0f172a; background-color: #f1f5f9; padding: 16px 32px; border-radius: 8px; border: 2px dashed #94a3b8;"">
                        {otpCode}
                    </span>
                </div>
                <p style=""color: #64748b; font-size: 13px; line-height: 1.6; text-align: center;"">
                    This code will expire in <strong>15 minutes</strong>.<br />
                    If you didn't request a password reset, please ignore this email.
                </p>
                <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;"" />
                <p style=""color: #94a3b8; font-size: 12px; text-align: center; margin: 0;"">
                    &copy; {System.DateTime.UtcNow.Year} Surgenius. All rights reserved.
                </p>
            </div>";

        await _emailService.SendEmailAsync(user.Email!, "Surgenius — Password Reset Verification Code", htmlBody);

        return ApiResponse<string>.Success(null!, "Verification code has been sent to your email.");
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

        var roleName = request.RoleName;
        if (string.IsNullOrWhiteSpace(roleName))
            return ApiResponse<string>.Failure("Role name is required.");

        if (!roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase) && 
            !roleName.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
            !roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<string>.Failure("Invalid role name. Must be 'Doctor', 'Student', or 'Admin'.");
        }

        if (roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase)) roleName = "Doctor";
        else if (roleName.Equals("Student", StringComparison.OrdinalIgnoreCase)) roleName = "Student";
        else if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)) roleName = "Admin";

        if (roleName == "Doctor")
        {
            user.UserType = UserType.Doctor;
            if (string.IsNullOrEmpty(user.InviteCode))
            {
                user.InviteCode = GenerateUniqueInviteCode(user.FullName ?? user.Email ?? "DOC");
            }
        }
        else if (roleName == "Student")
        {
            user.UserType = UserType.Student;
            if (!string.IsNullOrEmpty(request.InviteCode))
            {
                var doctor = await _userManager.Users.FirstOrDefaultAsync(u => u.InviteCode == request.InviteCode && u.UserType == UserType.Doctor);
                if (doctor == null)
                {
                    return ApiResponse<string>.Failure("Invalid doctor invite code.");
                }
                user.DoctorId = doctor.Id;
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.Failure("Failed to update user information.", errors);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(roleName))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.Failure("Failed to assign role.", errors);
            }
        }

        return ApiResponse<string>.Success($"Role '{roleName}' assigned successfully.");
    }

    public async Task<ApiResponse<string>> LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        }
        return ApiResponse<string>.Success(null!, "Logged out successfully.");
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
