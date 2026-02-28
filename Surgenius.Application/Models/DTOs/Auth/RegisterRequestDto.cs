using Surgenius.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.Models.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    [Required]
    public UserType UserType { get; set; }

    public string? InviteCode { get; set; }
}
