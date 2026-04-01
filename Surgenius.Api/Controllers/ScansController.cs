using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Api.Extensions;
using Surgenius.Application.DTOs.Scans;
using Surgenius.Application.Interfaces.Scans;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/scans")]
public class ScansController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScansController(IScanService scanService)
    {
        _scanService = scanService;
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST api/scans
    // Doctor uploads a scan for one of their cases.
    // ──────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadScan(
        [FromForm] IFormFile file,
        [FromForm] Guid caseId,
        [FromForm] string? scanType)
    {
        // Basic file validation at the controller boundary
        if (file == null || file.Length == 0)
            return BadRequest(new { IsSuccess = false, Message = "Please provide a valid file." });

        var doctorId = User.GetUserId();

        // Adapt IFormFile → framework-agnostic UploadScanDto
        var dto = new UploadScanDto
        {
            FileStream = file.OpenReadStream(),
            FileName   = file.FileName,
            CaseId     = caseId,
            ScanType   = scanType
        };

        var response = await _scanService.UploadScanAsync(doctorId, dto);

        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET api/scans/case/{caseId}
    // Accessible by both Doctors and Students (with their own access rules).
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("case/{caseId:guid}")]
    [Authorize(Roles = "Doctor,Student")]
    public async Task<IActionResult> GetScansByCase(Guid caseId)
    {
        var userId   = User.GetUserId();
        var isDoctor = User.IsInRole("Doctor");

        var response = await _scanService.GetScansByCaseAsync(userId, isDoctor, caseId);

        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
