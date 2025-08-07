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
        var emailSettings = _configuration.GetSection("EmailSettings");

        var from = emailSettings.GetValue<string>("Username");
        var password = emailSettings.GetValue<string>("Password");
        var smtpServer = emailSettings.GetValue<string>("SmtpServer");
        var port = emailSettings.GetValue<int>("Port");

        try
        {
            using (var client = new SmtpClient(smtpServer, port))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(from, password);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(from);
                    message.To.Add(new MailAddress(to));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    await client.SendMailAsync(message);
                }
            }
        }
        catch (Exception ex)
        {
            return;
        }
    }
}
