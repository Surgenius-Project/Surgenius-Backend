using System.Security.Claims;

namespace Surgenius.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the Authenticated User ID (Guid) from the JWT Claims.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));

        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

        return userId;
    }
}
