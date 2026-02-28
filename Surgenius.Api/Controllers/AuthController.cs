using Microsoft.AspNetCore.Mvc;
using Surgenius.Application.Models.DTOs.Auth;
using Surgenius.Application.Interfaces;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
