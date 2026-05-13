using System.Text.Json.Serialization;

namespace Surgenius.Application.DTOs.Analysis;

public class AiApiResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public AiApiDataDto Data { get; set; } = new();
}

public class AiApiDataDto
{
    [JsonPropertyName("patient_id")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("tumor_detected")]
    public bool TumorDetected { get; set; }

    [JsonPropertyName("predictions")]
    public AiApiPredictionsDto Predictions { get; set; } = new();

    [JsonPropertyName("metrics")]
    public AiApiMetricsDto Metrics { get; set; } = new();

    [JsonPropertyName("visuals")]
    public AiApiVisualsDto Visuals { get; set; } = new();
}

public class AiApiPredictionsDto
{
    [JsonPropertyName("stage_numeric")]
    public int StageNumeric { get; set; }

    [JsonPropertyName("stage_label")]
    public string StageLabel { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class AiApiMetricsDto
{
    [JsonPropertyName("tumor_area_pixels")]
    public long TumorAreaPixels { get; set; }
}

public class AiApiVisualsDto
{
    [JsonPropertyName("mask_base64")]
    public string MaskBase64 { get; set; } = string.Empty;

    [JsonPropertyName("highlighted_base64")]
    public string HighlightedBase64 { get; set; } = string.Empty;
}
