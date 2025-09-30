namespace WebVOD_Backend.Services.Interfaces;

public interface ICaptchaService
{
    public Task<bool> VerifyCaptchaToken(string captchaToken);
}
