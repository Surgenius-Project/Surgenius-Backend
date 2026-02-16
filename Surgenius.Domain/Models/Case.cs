using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Domain.Models;

public class Case
{
    public Guid Id { get; set; }
    public int UserID { get; set; }
    public required string CaseType { get; set; }
    public DateTime CreationDate { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Scan> Scans { get; set; } = new List<Scan>();

}
