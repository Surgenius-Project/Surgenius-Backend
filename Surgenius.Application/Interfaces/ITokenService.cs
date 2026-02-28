using Surgenius.Domain.Models;

namespace Surgenius.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, string role);
}
