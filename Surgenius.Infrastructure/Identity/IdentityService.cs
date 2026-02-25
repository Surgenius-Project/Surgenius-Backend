using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Surgenius.Application.Models.DTOs;
using Surgenius.Application.Interfaces;
using Surgenius.Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Surgenius.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public IdentityService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UserType = request.UserType,
            DoctorId = request.UserType == Domain.Enums.UserType.Student ? request.DoctorId : null
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return null;

        await _userManager.AddToRoleAsync(user, request.UserType.ToString());

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return null;

        return await GenerateAuthResponse(user);
    }

    public async Task<IEnumerable<AuthResponse>> GetDoctorsAsync()
    {
        var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
        var result = new List<AuthResponse>();

        foreach (var doctor in doctors)
        {
            result.Add(new AuthResponse
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Email = doctor.Email!,
                Role = "Doctor"
            });
        }

        return result;
    }

    private async Task<AuthResponse?> GenerateAuthResponse(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "No Role";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role)
        };

        var keyString = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString)) return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new AuthResponse
        {
            Id = user.Id,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = user.FullName,
            Email = user.Email!,
            Role = role
        };
    }
}
