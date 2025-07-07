namespace WebVOD_Backend.Dtos.Auth;

public class LoginResponseDto
{
    public string? Token { get; set; }
    public int ExpiresIn { get; set; }
    public bool TFARequired { get; set; }
}
