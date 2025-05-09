namespace WebVOD_Backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateJwtToken(string userId);
    int GetExpiresIn();
}
