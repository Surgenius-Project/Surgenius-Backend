using System;
using System.Text.Json.Serialization;

namespace Surgenius.Application.DTOs.Auth.Roles;

public class AssignRoleRequestDto
{
    public Guid UserId { get; set; }

    private string? _roleName;

    [JsonPropertyName("roleName")]
    public string RoleName
    {
        get => !string.IsNullOrWhiteSpace(_roleName) ? _roleName : (_role ?? string.Empty);
        set => _roleName = value;
    }

    private string? _role;

    [JsonPropertyName("role")]
    public string? Role
    {
        get => _role;
        set => _role = value;
    }

    [JsonPropertyName("inviteCode")]
    public string? InviteCode { get; set; }
}
