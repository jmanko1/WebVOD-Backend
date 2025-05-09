namespace WebVOD_Backend.Dtos.Auth;

public class LoginResponseDto
{
    public int StatusCode { get; set; }
    public string Token { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public string RedirectUrl { get; set; } = null!;
}
