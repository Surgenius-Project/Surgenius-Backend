using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Models.DTOs.Auth.Login;
using Surgenius.Application.Models.DTOs.Auth.Register;
using Surgenius.Application.Models.DTOs.Auth.Refresh;
using Surgenius.Application.Models.DTOs.Auth.Responses;
using Surgenius.Application.Models.DTOs.Auth.Email;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Application.Interfaces.Email;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        ITokenService tokenService,
        AppDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _emailService = emailService;
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterRequestDto request)
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
                return ApiResponse<string>.Failure("Student registration requires an invite code.");

            var doctor = await _userManager.Users
                .FirstOrDefaultAsync(u => u.InviteCode == request.InviteCode && u.UserType == Domain.Enums.UserType.Doctor);

            if (doctor == null)
                return ApiResponse<string>.Failure("Invalid invite code.");

            user.DoctorId = doctor.Id;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<string>.Failure("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(user, request.UserType.ToString());

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        
        // Construct the frontend confirmation URL
        var confirmationUrl = $"https://localhost:3000/confirm-email?userId={user.Id}&token={encodedToken}";
        
        var confirmationMessage = $"Please confirm your email by clicking here: {confirmationUrl}";
        await _emailService.SendEmailAsync(user.Email!, "Confirm your email", confirmationMessage);

        return ApiResponse<string>.Success("Registration successful. Please check your email to confirm your account.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return ApiResponse<AuthResponseDto>.Failure("Invalid email or password.");

        if (!user.EmailConfirmed)
            return ApiResponse<AuthResponseDto>.Failure("Email not confirmed. Please check your inbox.");

        return await GenerateAuthResponse(user);
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return ApiResponse<AuthResponseDto>.Failure("Invalid access token");

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<AuthResponseDto>.Failure("Invalid access token claims");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ApiResponse<AuthResponseDto>.Failure("User not found");

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == user.Id);

        if (storedToken == null || !storedToken.IsActive)
            return ApiResponse<AuthResponseDto>.Failure("Invalid or expired refresh token");

        // Token Rotation: Remove the old one
        _context.RefreshTokens.Remove(storedToken);
        await _context.SaveChangesAsync();

        return await GenerateAuthResponse(user);
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string userId)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == Guid.Parse(userId)).ToListAsync();
        if (tokens.Count != 0)
        {
            _context.RefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
        return ApiResponse<bool>.Success(true);
    }

    public async Task<ApiResponse<string>> ConfirmEmailAsync(ConfirmEmailRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        try 
        {
            var decodedTokenBytes = WebEncoders.Base64UrlDecode(request.Token);
            var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
            
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return ApiResponse<string>.Failure("Email confirmation failed.");

            return ApiResponse<string>.Success("Email confirmed successfully.");
        }
        catch (FormatException)
        {
            return ApiResponse<string>.Failure("Invalid confirmation token format.");
        }
    }

    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(ResendEmailConfirmationDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ApiResponse<string>.Failure("User not found.");

        if (user.EmailConfirmed)
            return ApiResponse<string>.Failure("Email is already confirmed.");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        
        var confirmationUrl = $"https://localhost:3000/confirm-email?userId={user.Id}&token={encodedToken}";
        var confirmationMessage = $"Please confirm your email by clicking here: {confirmationUrl}";
        await _emailService.SendEmailAsync(user.Email!, "Confirm your email", confirmationMessage);

        return ApiResponse<string>.Success("Confirmation email resent successfully.");
    }

    private async Task<ApiResponse<AuthResponseDto>> GenerateAuthResponse(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "No Role";

        var token = _tokenService.GenerateToken(user, role);
        var refreshToken = GenerateRefreshToken(user.Id);

        // Save refresh token to database
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Id = user.Id,
            Token = token,
            FullName = user.FullName,
            Email = user.Email!,
            Role = role,
            InviteCode = user.InviteCode,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiresAt
        };

        return ApiResponse<AuthResponseDto>.Success(response);
    }

    private RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(randomNumber),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };
    }

    private string GenerateUniqueInviteCode(string fullName)
    {
        var prefix = fullName.Length >= 3 ? fullName.Substring(0, 3).ToUpper() : "DOC";
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{random}";
    }
}
