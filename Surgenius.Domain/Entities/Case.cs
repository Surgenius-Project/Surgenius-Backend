using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;

public class Case
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string CaseType { get; set; }
    public DateTime CreationDate { get; set; }

    // Patient details
    public required string PatientName { get; set; }
    public int PatientAge { get; set; }
    public required string PatientGender { get; set; }   // e.g. "Male" | "Female" | "Other"
    public required string PatientPhone { get; set; }
    public string? Description { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Scan> Scans { get; set; } = new List<Scan>();
}
