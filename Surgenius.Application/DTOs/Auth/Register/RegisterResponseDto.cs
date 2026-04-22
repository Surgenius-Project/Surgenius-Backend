using System;

namespace Surgenius.Application.DTOs.Auth.Register;

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string? InviteCode { get; set; }
}
