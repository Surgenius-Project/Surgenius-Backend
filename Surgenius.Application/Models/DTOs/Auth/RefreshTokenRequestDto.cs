using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.Models.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string AccessToken { get; set; } = null!;

    [Required]
    public string RefreshToken { get; set; } = null!;
}
