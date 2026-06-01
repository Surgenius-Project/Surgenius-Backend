using System.Text.Json.Serialization;

namespace Surgenius.Application.DTOs.Analysis;

/// <summary>
/// Request DTO containing the 11 clinical lab metrics
/// sent to the Hugging Face risk-assessment model.
/// Properties use snake_case JSON names to match the AI API contract.
/// </summary>
public class RiskAssessmentRequestDto
{
    [JsonIgnore]
    public Guid CaseId { get; set; }

    [JsonPropertyName("Age")]
    public int Age { get; set; }

    [JsonPropertyName("Gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("Total_Bilirubin")]
    public double TotalBilirubin { get; set; }

    [JsonPropertyName("Direct_Bilirubin")]
    public double DirectBilirubin { get; set; }

    [JsonPropertyName("Alkaline_Phosphotase")]
    public int AlkalinePhosphotase { get; set; }

    [JsonPropertyName("Alamine_Aminotransferase")]
    public int AlamineAminotransferase { get; set; }

    [JsonPropertyName("Aspartate_Aminotransferase")]
    public int AspartateAminotransferase { get; set; }

    [JsonPropertyName("Total_Protiens")]
    public double TotalProtiens { get; set; }

    [JsonPropertyName("Albumin")]
    public double Albumin { get; set; }

    [JsonPropertyName("Albumin_and_Globulin_Ratio")]
    public double AlbuminAndGlobulinRatio { get; set; }
}

/// <summary>
/// Response DTO returned by the Hugging Face risk-assessment model.
/// Properties use snake_case JSON names to match the AI API response.
/// </summary>
public class RiskAssessmentResponseDto
{
    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("need_scan")]
    public bool NeedScan { get; set; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}
