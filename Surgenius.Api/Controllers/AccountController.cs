using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Domain.Models;
using Surgenius.Domain.Enums;
using Google.Apis.Auth;
using Surgenius.Application.DTOs.Auth.Login;

namespace Surgenius.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService, IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("signin-google")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
            return BadRequest(new { Message = "Google authentication failed." });

        var emailClaim = result.Principal.FindFirst(ClaimTypes.Email);
        var nameClaim = result.Principal.FindFirst(ClaimTypes.Name);

        if (emailClaim == null || nameClaim == null)
            return BadRequest(new { Message = "Required claims (Email, Name) were not received from Google." });

        var email = emailClaim.Value;
        var name = nameClaim.Value;

        var user = await _userManager.FindByEmailAsync(email);
        var isNewUser = false;

        if (user == null)
        {
            // If the user is new, create a new user record
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                EmailConfirmed = true // Authenticated via Google
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return BadRequest(new { Message = "Failed to create the user account.", Errors = createResult.Errors.Select(e => e.Description) });
            }

            // Assign default role
            // await _userManager.AddToRoleAsync(user, UserType.Student.ToString());
            isNewUser = true;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "No Role";

        // Generate JWT Token (TokenService already includes Email, Name, and role claims)
        var token = _tokenService.GenerateToken(user, primaryRole);

        return Ok(new
        {
            Token = token,
            Email = user.Email,
            Name = user.FullName,
            Role = primaryRole,
            RequiresRoleAssignment = isNewUser
        });
    }

    [AllowAnonymous]
    [HttpPost("google-mobile-login")]
    public async Task<IActionResult> GoogleMobileLogin([FromBody] GoogleMobileLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
            return BadRequest(new { Message = "IdToken is required." });

        try
        {
            var webClientId = _configuration["Authentication:Google:WebClientId"];
            var androidClientId = _configuration["Authentication:Google:AndroidClientId"];
            
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string> { webClientId!, androidClientId! }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

            var email = payload.Email;
            var name = payload.Name;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                return BadRequest(new { Message = "Invalid token payload: missing email or name." });

            var user = await _userManager.FindByEmailAsync(email);
            var isNewUser = false;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true 
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to create the user account.", Errors = createResult.Errors.Select(e => e.Description) });
                }

                isNewUser = true;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "No Role";

            var token = _tokenService.GenerateToken(user, primaryRole);

            return Ok(new
            {
                Token = token,
                Email = user.Email,
                Name = user.FullName,
                Role = primaryRole,
                RequiresRoleAssignment = isNewUser
            });
        }
        catch (InvalidJwtException ex)
        {
            return Unauthorized(new { Message = "Invalid Google token.", Error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while validating the Google token.", Error = ex.Message });
        }
    }

    [HttpPost("complete-registration")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized(new { Message = "User ID not found in token." });

        var user = await _userManager.FindByIdAsync(userIdString);
        if (user == null)
            return NotFound(new { Message = "User not found." });

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any())
            return BadRequest(new { Message = "User already has a role assigned." });

        if (request.Role != "Doctor" && request.Role != "Student")
            return BadRequest(new { Message = "Invalid role. Must be 'Doctor' or 'Student'." });

        if (request.Role == "Doctor")
        {
            user.UserType = UserType.Doctor;
            user.InviteCode = GenerateUniqueInviteCode(user.FullName);
        }
        else if (request.Role == "Student")
        {
            if (string.IsNullOrEmpty(request.InviteCode))
                return BadRequest(new { Message = "Invite code is required for students." });

            var doctor = _userManager.Users.FirstOrDefault(u => u.InviteCode == request.InviteCode && u.UserType == UserType.Doctor);
            if (doctor == null)
                return BadRequest(new { Message = "Invalid doctor invite code." });

            user.UserType = UserType.Student;
            user.DoctorId = doctor.Id;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(new { Message = "Failed to update user type." });

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return BadRequest(new { Message = "Failed to assign role." });

        // Generate a new token with the assigned role
        var token = _tokenService.GenerateToken(user, request.Role);

        return Ok(new
        {
            Token = token,
            Email = user.Email,
            Name = user.FullName,
            Role = request.Role,
            RequiresRoleAssignment = false
        });
    }

    private string GenerateUniqueInviteCode(string fullName)
    {
        var prefix = fullName.Length >= 3 ? fullName.Substring(0, 3).ToUpper() : "DOC";
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{random}";
    }
}

public class CompleteRegistrationRequest
{
    public string Role { get; set; } = string.Empty;
    public string? InviteCode { get; set; }
}
