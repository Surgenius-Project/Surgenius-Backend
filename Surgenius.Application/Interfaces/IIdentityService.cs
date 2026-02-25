using Surgenius.Application.Models.DTOs;

namespace Surgenius.Application.Interfaces;

public interface IIdentityService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<IEnumerable<AuthResponse>> GetDoctorsAsync();
}
