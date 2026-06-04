using System.Text.Json.Serialization;

namespace Surgenius.Application.DTOs.Analysis;

public class ScanAnalysisResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tumor_detected")]
    public bool TumorDetected { get; set; }

    [JsonPropertyName("diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("tumor_pixels")]
    public int TumorPixels { get; set; }

    [JsonPropertyName("inference_time_sec")]
    public double InferenceTimeSec { get; set; }

    [JsonPropertyName("original_image")]
    public string? OriginalImage { get; set; }
}
