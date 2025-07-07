using System.Security.Claims;

namespace WebVOD_Backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(string login);
    string GenerateRefreshToken(string login);
    int GetAccessTokenLifetime();
    int GetRefreshTokenLifetime();
    string? GetJti(string jwt);
    DateTime GetExpiresAt(string jwt);
    Task<ClaimsPrincipal?> ValidateRefreshToken(string refreshToken);
}
