using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IFileStorageService fileStorage,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AnalysisService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _fileStorage = fileStorage;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId, bool isAdmin)
    {
        // 1. Verify the scan exists
        var scan = await _context.Scans.Include(s => s.Case).FirstOrDefaultAsync(s => s.Id == scanId);
        if (scan == null)
            throw new Exception(await BuildScanNotFoundMessageAsync(scanId, userId, isAdmin));

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

        const string requestUrl = "https://moutel258-surgenius-ai.hf.space/api/v1/analyze-scan";

        using var form = new MultipartFormDataContent();
        await using var fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        
        var ext = Path.GetExtension(absolutePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/png"
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        
        var fileName = Path.GetFileName(absolutePath);
        form.Add(fileContent, "file", fileName);

        _logger.LogInformation("Sending scan {ScanId} to AI API at {Url}", scanId, requestUrl);

        var response = await client.PostAsync(requestUrl, form);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("AI API Error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new Exception($"Failed to process scan through the AI API. Status: {response.StatusCode}, Error: {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var aiResult = JsonSerializer.Deserialize<ScanAnalysisResponseDto>(jsonResponse, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (aiResult == null || aiResult.Status != "success")
            throw new Exception("Invalid response received from the AI API.");

        // Download the 4 augmented images locally and return Monster Server URLs
        // to Flutter. Doing this in parallel avoids the slow/timeout-prone external
        // links (Hugging Face / Render) being fetched by the client directly.
        var downloadTasks = new[]
        {
            DownloadAndSaveImageAsync(aiResult.OriginalImage, "original_image"),
            DownloadAndSaveImageAsync(aiResult.UnetImage, "unet_image"),
            DownloadAndSaveImageAsync(aiResult.GroundTruthImage, "ground_truth_image"),
            DownloadAndSaveImageAsync(aiResult.DiagnosisImage, "diagnosis_image")
        };

        var paths = await Task.WhenAll(downloadTasks);

        var originalImagePath = paths[0];
        var maskPath = paths[1];
        var groundTruthImagePath = paths[2];
        var highlightedPath = paths[3];

        // 5. Save Result to Database
        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            StageNumeric = aiResult.TumorDetected ? 1 : 0,
            StageLabel = aiResult.Diagnosis,
            Confidence = aiResult.Confidence > 1.0
                ? Math.Round(aiResult.Confidence / 100.0, 4) 
                : Math.Round(aiResult.Confidence, 4),
            TumorAreaPixels = aiResult.TumorPixels,
            OriginalImagePath = originalImagePath,
            MaskPath = maskPath,
            GroundTruthImagePath = groundTruthImagePath,
            HighlightedPath = highlightedPath,
            Model3DPath = "/uploads/models/liver_placeholder.glb" // Placeholder for 3D
        };

        var oldGeneratedPaths = new List<string?>();
        var existing = await _context.AnalysisResults.FirstOrDefaultAsync(a => a.ScanId == scanId);
        if (existing != null)
        {
            oldGeneratedPaths.Add(existing.OriginalImagePath);
            oldGeneratedPaths.Add(existing.MaskPath);
            oldGeneratedPaths.Add(existing.GroundTruthImagePath);
            oldGeneratedPaths.Add(existing.HighlightedPath);
            _context.AnalysisResults.Remove(existing);
        }

        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync();

        foreach (var oldGeneratedPath in oldGeneratedPaths)
        {
            if (!string.IsNullOrWhiteSpace(oldGeneratedPath))
            {
                await _fileStorage.DeleteFileAsync(oldGeneratedPath);
            }
        }

        return result;
    }

    private async Task<string> BuildScanNotFoundMessageAsync(Guid suppliedId, Guid userId, bool isAdmin)
    {
        var matchingCase = await _context.Cases
            .AsNoTracking()
            .Include(c => c.Scans)
            .FirstOrDefaultAsync(c => c.Id == suppliedId);

        if (matchingCase == null)
        {
            _logger.LogWarning("Analysis requested for missing scan id {ScanId}", suppliedId);
            return $"Scan not found. The analysis endpoint expects a Scan Id, but no scan exists with id '{suppliedId}'.";
        }

        if (!isAdmin && matchingCase.UserId != userId)
        {
            _logger.LogWarning(
                "Analysis requested with case id {CaseId}, but user {UserId} does not own that case",
                suppliedId, userId);

            return "Scan not found. The analysis endpoint expects a Scan Id.";
        }

        var latestScan = matchingCase.Scans
            .OrderByDescending(s => s.UploadDate)
            .FirstOrDefault();

        if (latestScan == null)
        {
            return $"Scan not found. The supplied id '{suppliedId}' is a Case Id, and this case does not have any uploaded scans yet.";
        }

        return $"Scan not found. The supplied id '{suppliedId}' is a Case Id. Use the Scan Id '{latestScan.Id}' for this case.";
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
        OriginalImagePath = a.OriginalImagePath,
        MaskPath = a.MaskPath,
        GroundTruthImagePath = a.GroundTruthImagePath,
        HighlightedPath = a.HighlightedPath,
        Model3DPath = a.Model3DPath
    };

    // ══════════════════════════════════════════════════════════════════════
    // LOCAL IMAGE PROXY
    // Downloads augmented scan images from external AI model links into
    // wwwroot/uploads/augmented-scans and returns local Monster Server URLs.
    // ══════════════════════════════════════════════════════════════════════
    private async Task<string> DownloadAndSaveImageAsync(string? imageUrl, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            _logger.LogError("AI API response is missing '{FieldName}' image URL/data", fieldName);
            throw new Exception($"AI API response is missing '{fieldName}' image URL/data.");
        }

        // 1. Ensure the augmented-scans folder exists
        var augmentedScansFolder = Path.Combine(_env.WebRootPath, "uploads", "augmented-scans");
        if (!Directory.Exists(augmentedScansFolder))
            Directory.CreateDirectory(augmentedScansFolder);

        // 2. Generate a unique filename and resolve the absolute disk path
        var fileName = $"{Guid.NewGuid()}.jpg";
        var absolutePath = Path.Combine(augmentedScansFolder, fileName);

        // Check if string contains "base64," or does NOT start with "http"
        bool isBase64 = imageUrl.Contains("base64,", StringComparison.OrdinalIgnoreCase) || 
                         !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        if (isBase64)
        {
            _logger.LogInformation("Processing Base64 image data for '{FieldName}'", fieldName);

            // Clean the header prefix if present (e.g., "data:image/jpeg;base64,")
            var base64Data = imageUrl;
            var base64Index = base64Data.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (base64Index >= 0)
            {
                base64Data = base64Data.Substring(base64Index + "base64, ".Trim().Length);
            }
            base64Data = base64Data.Trim();

            try
            {
                var imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(absolutePath, imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse or save Base64 image for '{FieldName}'", fieldName);
                throw new Exception($"Failed to process Base64 image for '{fieldName}'.", ex);
            }
        }
        else
        {
            // 3. Download the image bytes asynchronously via HttpClient
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            _logger.LogInformation("Downloading {FieldName} from {Url}", fieldName, imageUrl);

            try
            {
                using var response = await client.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(absolutePath, imageBytes);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Download of '{FieldName}' from {Url} timed out", fieldName, imageUrl);
                throw new Exception($"Download of '{fieldName}' image timed out. Please try again later.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to download '{FieldName}' from {Url}", fieldName, imageUrl);
                throw new Exception($"Failed to download '{fieldName}' image from the AI model.");
            }
        }

        // 4. Build the local response URL from the current request scheme + host
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            _logger.LogWarning("HttpContext is not available; returning relative path for {FieldName}", fieldName);
            return $"/uploads/augmented-scans/{fileName}";
        }

        var localUrl = $"{request.Scheme}://{request.Host}/uploads/augmented-scans/{fileName}";
        return localUrl;
    }

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
