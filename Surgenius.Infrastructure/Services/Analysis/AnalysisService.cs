using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Surgenius.Application.DTOs.Analysis;
using Surgenius.Application.Interfaces.Analysis;
using Surgenius.Application.Interfaces.Storage;
using Surgenius.Application.Models.Responses;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Services.Analysis;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IFileStorageService _fileStorage;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IFileStorageService fileStorage,
        IWebHostEnvironment env,
        ILogger<AnalysisService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _fileStorage = fileStorage;
        _env = env;
        _logger = logger;
    }

    public async Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId, bool isAdmin)
    {
        // 1. Verify the scan exists
        var scan = await _context.Scans.Include(s => s.Case).FirstOrDefaultAsync(s => s.Id == scanId);
        if (scan == null)
            throw new Exception("Scan not found.");

        if (!isAdmin && scan.Case.UserId != userId)
            throw new Exception("Unauthorized to analyze this scan.");

        // 2. Prepare File for AI processing
        var cleanPath = scan.ScanPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var absolutePath = Path.Combine(_env.WebRootPath, cleanPath);
        
        if (!File.Exists(absolutePath))
            throw new Exception("Scan file not found on the server.");

        // 3. Make HTTP Call to AI API
        var client = _httpClientFactory.CreateClient("AiApiClient");
        client.Timeout = TimeSpan.FromSeconds(60); // 60s timeout for cold starts

        var baseUrl = _configuration["AiApi:BaseUrl"] ?? "https://render-free-tier-placeholder.com";
        var requestUrl = $"{baseUrl.TrimEnd('/')}/api/v1/analyze-scan";

        using var form = new MultipartFormDataContent();
        await using var fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        
        var fileName = Path.GetFileName(absolutePath);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(scan.CaseId.ToString()), "patient_id");

        _logger.LogInformation("Sending scan {ScanId} to AI API at {Url}", scanId, requestUrl);

        var response = await client.PostAsync(requestUrl, form);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("AI API Error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new Exception("Failed to process scan through the AI API.");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var aiResult = JsonSerializer.Deserialize<AiApiResponseDto>(jsonResponse, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (aiResult == null || aiResult.Status != "success")
            throw new Exception("Invalid response received from the AI API.");

        // 4. Save Base64 Visuals to Disk
        string? maskUrlPath = null;
        string? highlightedUrlPath = null;

        if (!string.IsNullOrEmpty(aiResult.Data.Visuals.MaskBase64))
        {
            var maskBytes = Convert.FromBase64String(aiResult.Data.Visuals.MaskBase64);
            using var maskStream = new MemoryStream(maskBytes);
            maskUrlPath = await _fileStorage.SaveAnalysisImageAsync(maskStream, $"mask_{scanId}.png");
        }

        if (!string.IsNullOrEmpty(aiResult.Data.Visuals.HighlightedBase64))
        {
            var highlightedBytes = Convert.FromBase64String(aiResult.Data.Visuals.HighlightedBase64);
            using var highlightedStream = new MemoryStream(highlightedBytes);
            highlightedUrlPath = await _fileStorage.SaveAnalysisImageAsync(highlightedStream, $"highlighted_{scanId}.png");
        }

        // 5. Save Result to Database
        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            StageNumeric = aiResult.Data.Predictions.StageNumeric,
            StageLabel = aiResult.Data.Predictions.StageLabel,
            Confidence = Math.Round(aiResult.Data.Predictions.Confidence / 100.0, 4), // Convert percentage to 0.0 - 1.0, or keep it depending on DB schema.
            // Documentation says: `max(rf_model.predict_proba(features)[0]) * 100 -> Returns a float percentage (e.g., 87.5)`
            // Wait, previously Confidence was set to Math.Round(random.NextDouble() * 0.5 + 0.5, 2) which is between 0 and 1.
            // If AI returns 87.5, it's out of 100. Previous mock used 0-1 scale. So I'll divide by 100.
            TumorAreaPixels = aiResult.Data.Metrics.TumorAreaPixels,
            MaskPath = maskUrlPath,
            HighlightedPath = highlightedUrlPath,
            Model3DPath = "/uploads/models/liver_placeholder.obj" // Placeholder for 3D
        };

        var existing = await _context.AnalysisResults.FirstOrDefaultAsync(a => a.ScanId == scanId);
        if (existing != null)
        {
            _context.AnalysisResults.Remove(existing);
        }

        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ApiResponse<AnalysisReadDto>> GetAnalysisByScanAsync(Guid userId, bool isDoctor, bool isAdmin, Guid scanId)
    {
        var analysis = await _context.AnalysisResults
            .AsNoTracking()
            .Include(a => a.Scan)
                .ThenInclude(s => s.Case)
            .FirstOrDefaultAsync(a => a.ScanId == scanId);

        if (analysis == null)
            return ApiResponse<AnalysisReadDto>.Failure("Analysis result not found.");

        var @case = analysis.Scan?.Case;
        if (@case == null)
            return ApiResponse<AnalysisReadDto>.Failure("Associated case not found.");

        if (isAdmin)
        {
            // Admin has access to all analysis results
        }
        else if (isDoctor)
        {
            if (@case.UserId != userId)
                return ApiResponse<AnalysisReadDto>.Failure("Access denied. You do not own this case.");
        }
        else
        {
            var student = await _context.Users
                .AsNoTracking()
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<AnalysisReadDto>.Failure("Student account not found.");

            if (student.DoctorId == null || student.DoctorId != @case.UserId)
                return ApiResponse<AnalysisReadDto>.Failure(
                    "Access denied. You are not linked to the Doctor who owns this case.");

            if (student.Doctor != null && !student.Doctor.IsInviteCodeActive)
                return ApiResponse<AnalysisReadDto>.Failure("Access denied by Doctor.");
        }

        var dto = MapToDto(analysis);
        return ApiResponse<AnalysisReadDto>.Success(dto);
    }

    private static AnalysisReadDto MapToDto(AnalysisResult a) => new()
    {
        Id = a.Id,
        ScanId = a.ScanId,
        StageNumeric = a.StageNumeric,
        StageLabel = a.StageLabel,
        Confidence = a.Confidence,
        TumorAreaPixels = a.TumorAreaPixels,
        MaskPath = a.MaskPath,
        HighlightedPath = a.HighlightedPath,
        Model3DPath = a.Model3DPath
    };

    // ══════════════════════════════════════════════════════════════════════
    // CLINICAL RISK ASSESSMENT  (independent from CT Scan pipeline)
    // Calls the Hugging Face ILPD model to evaluate liver disease risk.
    // ══════════════════════════════════════════════════════════════════════
    public async Task<RiskAssessmentResponseDto> AssessRiskAsync(RiskAssessmentRequestDto dto)
    {
        var client = _httpClientFactory.CreateClient("RiskApiClient");
        var requestUrl = client.BaseAddress != null 
            ? "predict-risk" 
            : "https://moutel258-ilpd.hf.space/predict-risk";

        var displayUrl = client.BaseAddress != null 
            ? new Uri(client.BaseAddress, "predict-risk").ToString() 
            : requestUrl;

        _logger.LogInformation("Sending clinical risk assessment request to {Url}", displayUrl);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(requestUrl, dto);
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Risk assessment request to {Url} timed out", displayUrl);
            throw new Exception("The risk assessment service is currently unavailable. Please try again later.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to risk assessment service at {Url}", displayUrl);
            throw new Exception("Unable to reach the risk assessment service. Please check your connection and try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Risk API Error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new Exception("The risk assessment model returned an error. Please try again.");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RiskAssessmentResponseDto>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
        {
            _logger.LogError("Risk API returned null or unparseable response: {Body}", jsonResponse);
            throw new Exception("Invalid response received from the risk assessment model.");
        }

        _logger.LogInformation(
            "Risk assessment completed — RiskLevel: {RiskLevel}, Confidence: {Confidence}, NeedScan: {NeedScan}",
            result.RiskLevel, result.Confidence, result.NeedScan);

        try
        {
            var riskAssessment = new PatientRiskAssessment
            {
                Id = Guid.NewGuid(),
                CaseId = dto.CaseId,
                CreatedAt = DateTime.UtcNow,
                Age = dto.Age,
                Gender = dto.Gender,
                TotalBilirubin = dto.TotalBilirubin,
                DirectBilirubin = dto.DirectBilirubin,
                AlkalinePhosphotase = dto.AlkalinePhosphotase,
                AlamineAminotransferase = dto.AlamineAminotransferase,
                AspartateAminotransferase = dto.AspartateAminotransferase,
                TotalProtiens = dto.TotalProtiens,
                Albumin = dto.Albumin,
                AlbuminAndGlobulinRatio = dto.AlbuminAndGlobulinRatio,
                RiskLevel = result.RiskLevel,
                Confidence = result.Confidence,
                NeedScan = result.NeedScan,
                Recommendation = result.Recommendation
            };

            _context.RiskAssessments.Add(riskAssessment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved PatientRiskAssessment with ID {RiskId} for Case {CaseId} to database.", riskAssessment.Id, dto.CaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PatientRiskAssessment to database for Case {CaseId}", dto.CaseId);
        }

        return result;
    }
}

