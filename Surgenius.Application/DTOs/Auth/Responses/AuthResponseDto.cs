using System;
using System.Collections.Generic;

namespace Surgenius.Application.DTOs.Auth.Responses;

public class AuthResponseDto
{
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public string? InviteCode { get; set; }
}
