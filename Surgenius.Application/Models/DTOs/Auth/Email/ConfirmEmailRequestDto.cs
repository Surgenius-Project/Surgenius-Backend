using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.Models.DTOs.Auth.Email;

public class ConfirmEmailRequestDto
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;
}
