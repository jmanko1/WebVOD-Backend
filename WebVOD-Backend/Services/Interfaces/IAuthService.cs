using WebVOD_Backend.Dtos.Auth;

namespace WebVOD_Backend.Services.Interfaces;

public interface IAuthService
{
    Task Register(RegisterDto registerDto);
    Task<LoginResponseDto> Authenticate(LoginDto loginDto, HttpContext httpContext, HttpRequest httpRequest);
    Task<LoginResponseDto> Code(string code, HttpContext httpContext, HttpRequest httpRequest);
    Task InitiateResetPassword(string email);
    Task ResetPassword(string token);
}
