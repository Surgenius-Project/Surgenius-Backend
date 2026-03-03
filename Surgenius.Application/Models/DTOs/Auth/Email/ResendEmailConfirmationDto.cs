using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.Models.DTOs.Auth.Email;

public class ResendEmailConfirmationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
