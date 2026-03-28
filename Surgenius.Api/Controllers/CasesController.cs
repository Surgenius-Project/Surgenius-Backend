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
    public async Task<IActionResult> GetUserCases()
    {
        var userId = User.GetUserId();
        
        var response = await _caseService.GetUserCasesAsync(userId);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCase(Guid id)
    {
        var userId = User.GetUserId();
        
        var response = await _caseService.GetCaseByIdAsync(userId, id);
        
        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }
}
