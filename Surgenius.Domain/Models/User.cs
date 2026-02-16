using System.ComponentModel.DataAnnotations;

namespace Surgenius.Domain.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
