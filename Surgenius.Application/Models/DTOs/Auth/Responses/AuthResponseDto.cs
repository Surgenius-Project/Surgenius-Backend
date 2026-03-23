namespace Surgenius.Application.Models.DTOs.Auth.Responses;

public class AuthResponseDto
{
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? InviteCode { get; set; }
}
