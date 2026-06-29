namespace Surgenius.Application.DTOs.Cases;

public class CaseResponseDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
