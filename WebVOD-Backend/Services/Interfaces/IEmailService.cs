namespace WebVOD_Backend.Services.Interfaces;

public interface IEmailService
{
    Task SendEmail(string to, string subject, string body);
}
