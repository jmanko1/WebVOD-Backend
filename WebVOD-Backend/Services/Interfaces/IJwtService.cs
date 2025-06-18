using System.Security.Claims;

namespace WebVOD_Backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(string login);
    string GenerateRefreshToken(string login);
    int GetAccessTokenLifetime();
    int GetRefreshTokenLifetime();
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}
