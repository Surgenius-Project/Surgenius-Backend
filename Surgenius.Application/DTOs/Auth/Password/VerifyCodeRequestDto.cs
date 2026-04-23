using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.DTOs.Auth.Password;

public class VerifyCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
}
