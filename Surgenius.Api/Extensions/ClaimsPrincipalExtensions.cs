using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Surgenius.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the Authenticated User ID (Guid) from the JWT Claims.
    /// Supports both the long 'NameIdentifier' and the standard 'sub' (JwtRegisteredClaimNames.Sub) claim.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));

        // Look for 'sub' or the name identifier schema
        var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

        return userId;
    }
}
