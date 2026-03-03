using Surgenius.Domain.Models;
using System.Security.Claims;

namespace Surgenius.Application.Interfaces.Auth;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, string role);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
