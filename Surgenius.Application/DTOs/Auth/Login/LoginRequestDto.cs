using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.DTOs.Auth.Login;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
