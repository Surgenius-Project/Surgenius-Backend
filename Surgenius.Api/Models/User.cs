using System.ComponentModel.DataAnnotations;

namespace Surgenius.Api.Models;

/// <summary>
/// Represents a user in the Surgenius system.
/// </summary>
public class User
{
    [Key]
    public int UserID { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Password { get; set; }

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Role { get; set; }

    // Navigation properties
    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
