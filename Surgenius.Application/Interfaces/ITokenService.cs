using Surgenius.Domain.Models;

using System.Security.Claims;

namespace Surgenius.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, string role);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
