using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surgenius.Api.Models;

public class Case
{
    [Key]
    public int CaseID { get; set; }

    [Required]
    [ForeignKey(nameof(User))]
    public int UserID { get; set; }

    [Required]
    [MaxLength(100)]
    public required string CaseType { get; set; }

    [Required]
    public DateTime CreationDate { get; set; }


    public User User { get; set; } = null!;
    public ICollection<Scan> Scans { get; set; } = new List<Scan>();
}
