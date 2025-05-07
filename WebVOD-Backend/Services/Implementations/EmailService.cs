using System.Net.Mail;
using System.Net;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmail(string to, string subject, string body)
    {
        var emailSettings = _configuration.GetSection("Email");

        var from = emailSettings.GetValue<string>("Username");
        var password = emailSettings.GetValue<string>("Password");
        var smtpServer = emailSettings.GetValue<string>("SmtpServer");
        var port = emailSettings.GetValue<int>("Port");

        try
        {
            var client = new SmtpClient(smtpServer, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from, password)
            };

            var message = new MailMessage(from, to, subject, body);
            await client.SendMailAsync(message);
        }
        catch(Exception ex)
        {
            throw;
        }
    }
}
