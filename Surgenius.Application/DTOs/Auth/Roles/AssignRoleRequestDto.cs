using System;

namespace Surgenius.Application.DTOs.Auth.Roles;

public class AssignRoleRequestDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!;
}
