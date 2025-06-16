namespace WebVOD_Backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateJwtToken(string login);
    int GetExpiresIn();
}
