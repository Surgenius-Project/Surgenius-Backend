using System.ComponentModel.DataAnnotations;

namespace Surgenius.Application.DTOs.Auth.Login;

public class GoogleMobileLoginRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
