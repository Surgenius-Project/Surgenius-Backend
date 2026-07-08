using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Api.Extensions;
using Surgenius.Application.DTOs.Auth.Login;
using Surgenius.Application.DTOs.Auth.Register;
using Surgenius.Application.DTOs.Auth.Password;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Application.DTOs.Auth.Roles;
using Surgenius.Application.DTOs.Auth.Responses;

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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequestDto request)
    {
        var response = await _authService.VerifyCodeAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var response = await _authService.ResetPasswordAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [Authorize]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
    {
        if (request.UserId == System.Guid.Empty)
        {
            try
            {
                request.UserId = User.GetUserId();
            }
            catch
            {
                // Let service handle validation if claim is absent
            }
        }

        var response = await _authService.AssignRoleAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var response = await _authService.LogoutAsync();
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
