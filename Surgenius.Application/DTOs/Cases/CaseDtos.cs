namespace Surgenius.Application.DTOs.Cases;

public class CreateCaseDto
{
    public required string CaseType { get; set; }
}

public class CaseDto
{
    public Guid Id { get; set; }
    public required string CaseType { get; set; }
    public DateTime CreationDate { get; set; }
}
