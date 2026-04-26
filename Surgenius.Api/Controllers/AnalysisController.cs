using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Api.Extensions;
using Surgenius.Application.DTOs.Analysis;
using Surgenius.Application.Interfaces.Analysis;
using Surgenius.Application.Models.Responses;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;

    public AnalysisController(IAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    // --------------------------------------------------------------------------
    // POST api/analysis/process/{scanId}
    // Trigger mock AI analysis for a specific scan.
    // Doctor only - must own the case containing the scan.
    // --------------------------------------------------------------------------
    [HttpPost("process/{scanId:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> ProcessScan(Guid scanId)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        try
        {
            var result = await _analysisService.ProcessScanAsync(scanId, userId, isAdmin);

            // Map to DTO and transform relative paths to absolute URLs
            var dto = new AnalysisReadDto
            {
                Id = result.Id,
                ScanId = result.ScanId,
                StageNumeric = result.StageNumeric,
                StageLabel = result.StageLabel,
                Confidence = result.Confidence,
                TumorAreaPixels = result.TumorAreaPixels,
                MaskPath = TransformToAbsoluteUrl(result.MaskPath),
                HighlightedPath = TransformToAbsoluteUrl(result.HighlightedPath),
                Model3DPath = TransformToAbsoluteUrl("/uploads/models/liver_placeholder.obj") // Final placeholder as requested
            };

            var response = ApiResponse<AnalysisReadDto>.Success(dto, "Analysis completed successfully.");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<AnalysisReadDto>.Failure(ex.Message);
            return BadRequest(errorResponse);
        }
    }

    // --------------------------------------------------------------------------
    // GET api/analysis/scan/{scanId}
    // Retrieve existing analysis result for a specific scan.
    // Accessible by Doctors and Students.
    // --------------------------------------------------------------------------
    [HttpGet("scan/{scanId:guid}")]
    [Authorize(Roles = "Admin,Doctor,Student")]
    public async Task<IActionResult> GetAnalysisByScan(Guid scanId)
    {
        var userId = User.GetUserId();
        var isDoctor = User.IsInRole("Doctor");
        var isAdmin = User.IsInRole("Admin");

        var response = await _analysisService.GetAnalysisByScanAsync(userId, isDoctor, isAdmin, scanId);

        if (!response.IsSuccess)
            return BadRequest(response);

        // Transform relative paths to absolute URLs in the response data
        if (response.Data != null)
        {
            response.Data.MaskPath = TransformToAbsoluteUrl(response.Data.MaskPath);
            response.Data.HighlightedPath = TransformToAbsoluteUrl(response.Data.HighlightedPath);
            response.Data.Model3DPath = TransformToAbsoluteUrl("/uploads/models/liver_placeholder.obj"); // Final placeholder as requested
        }

        return Ok(response);
    }

    // --------------------------------------------------------------------------
    // Helper method: Transform relative paths to absolute URLs using HttpContext
    // --------------------------------------------------------------------------
    private string? TransformToAbsoluteUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return relativePath;

        // If it's already an absolute URL, return as-is
        if (relativePath.StartsWith("http://") || relativePath.StartsWith("https://"))
            return relativePath;

        // Build absolute URL from request context
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // Ensure path starts with /
        var path = relativePath.StartsWith("/") ? relativePath : $"/{relativePath}";

        return $"{baseUrl}{path}";
    }
}

