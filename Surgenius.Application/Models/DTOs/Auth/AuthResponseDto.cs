namespace Surgenius.Application.Models.DTOs.Auth;

public class AuthResponseDto
{
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? InviteCode { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpiry { get; set; }
}
