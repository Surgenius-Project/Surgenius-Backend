using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Api.Extensions;
using Surgenius.Application.DTOs.Profile;
using Surgenius.Application.Interfaces.Profile;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var response = await _profileService.GetProfileAsync(userId);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var userId = User.GetUserId();
        var response = await _profileService.UpdateProfileAsync(userId, request);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("students")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetLinkedStudents()
    {
        var userId = User.GetUserId();
        var response = await _profileService.GetLinkedStudentsAsync(userId);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpDelete("students/{studentId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> RemoveStudent(Guid studentId)
    {
        var userId = User.GetUserId();
        var response = await _profileService.RemoveStudentAsync(userId, studentId);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = User.GetUserId();
        var response = await _profileService.ChangePasswordAsync(userId, request);
        
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
