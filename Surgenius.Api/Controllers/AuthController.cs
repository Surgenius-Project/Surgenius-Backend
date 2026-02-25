using Microsoft.AspNetCore.Mvc;
using Surgenius.Application.Models.DTOs;
using Surgenius.Application.Interfaces;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _identityService.RegisterAsync(request);
        if (response == null) return BadRequest("Registration failed.");

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _identityService.LoginAsync(request);
        if (response == null) return Unauthorized("Invalid email or password.");

        return Ok(response);
    }
}
