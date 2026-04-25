using Microsoft.AspNetCore.Identity;
using Surgenius.Domain.Enums;

namespace Surgenius.Domain.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }
    public UserType UserType { get; set; }
    public string? InviteCode { get; set; }

    public Guid? DoctorId { get; set; }
    public ApplicationUser? Doctor { get; set; }
    public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public bool IsInviteCodeActive { get; set; } = true;

    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
