namespace Surgenius.Application.Models.DTOs.Auth.Register;

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string? InviteCode { get; set; }
}
