using Microsoft.AspNetCore.Mvc;
using Surgenius.Application.Models.DTOs.Auth.Login;
using Surgenius.Application.Models.DTOs.Auth.Register;
using Surgenius.Application.Models.DTOs.Auth.Password;
using Surgenius.Application.Interfaces.Auth;

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

    [HttpGet("reset-password-page")]
    public ContentResult GetResetPasswordPage([FromQuery] string email, [FromQuery] string token)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Reset Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; background-color: #f4f4f9; margin: 0; }}
        .container {{ background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); width: 300px; }}
        h2 {{ text-align: center; color: #333; }}
        input {{ width: 100%; padding: 10px; margin: 10px 0; border: 1px solid #ccc; border-radius: 4px; box-sizing: border-box; }}
        button {{ width: 100%; padding: 10px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; }}
        button:hover {{ background: #0056b3; }}
        #message {{ margin-top: 10px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>Reset Password</h2>
        <input type='hidden' id='email' value='{email}' />
        <input type='hidden' id='token' value='{token}' />
        <input type='password' id='newPassword' placeholder='New Password' required />
        <button onclick='resetPassword()'>Submit</button>
        <div id='message'></div>
    </div>
    <script>
        async function resetPassword() {{
            const email = document.getElementById('email').value;
            const token = document.getElementById('token').value;
            const newPassword = document.getElementById('newPassword').value;
            const msgObj = document.getElementById('message');
            
            if (!newPassword) {{
                msgObj.innerHTML = '<span style=""color:red;"">Password is required.</span>';
                return;
            }}

            const response = await fetch('/api/auth/reset-password', {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{ email: email, token: token, newPassword: newPassword }})
            }});

            const result = await response.json();
            if (response.ok) {{
                msgObj.innerHTML = '<span style=""color:green;"">Password reset successfully! You can now login.</span>';
            }} else {{
                msgObj.innerHTML = '<span style=""color:red;"">' + (result.message || 'Error occurred.') + '</span>';
            }}
        }}
    </script>
</body>
</html>";
        return new ContentResult
        {
            Content = html,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var response = await _authService.ResetPasswordAsync(request);
        if (!response.IsSuccess)
            return BadRequest(response);

        return Ok(response);
    }
}
