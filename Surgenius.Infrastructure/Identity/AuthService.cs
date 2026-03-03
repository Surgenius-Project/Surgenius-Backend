using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Models.DTOs.Auth;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.Interfaces;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        ITokenService tokenService,
        AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
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
