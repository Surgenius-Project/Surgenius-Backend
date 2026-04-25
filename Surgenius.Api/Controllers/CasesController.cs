using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Api.Extensions;
using Surgenius.Application.Interfaces.Cases;
using Surgenius.Application.DTOs.Cases;

namespace Surgenius.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;

    public CasesController(ICaseService caseService)
    {
        _caseService = caseService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseDto request)
    {
        var userId = User.GetUserId();
        
        // Assuming ICaseService.CreateCaseAsync takes userId and the DTO
        var response = await _caseService.CreateCaseAsync(userId, request);
        
        if (!response.IsSuccess)
            return BadRequest(response);
            
        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Student")]
    public async Task<IActionResult> GetUserCases([FromQuery] string? searchTerm, [FromQuery] string? stage)
    {
        var userId   = User.GetUserId();
        var isDoctor = User.IsInRole("Doctor");
        var isAdmin  = User.IsInRole("Admin");

        var response = await _caseService.GetUserCasesAsync(userId, isDoctor, isAdmin, searchTerm, stage);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("toggle-access")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> ToggleStudentAccess()
    {
        var userId = User.GetUserId();
        var response = await _caseService.ToggleStudentAccessAsync(userId);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor,Student")]
    public async Task<IActionResult> GetCase(Guid id)
    {
        var userId   = User.GetUserId();
        var isDoctor = User.IsInRole("Doctor");
        var isAdmin  = User.IsInRole("Admin");

        var response = await _caseService.GetCaseByIdAsync(userId, isDoctor, isAdmin, id);

        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> DeleteCase(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var response = await _caseService.DeleteCaseAsync(userId, isAdmin, id);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
