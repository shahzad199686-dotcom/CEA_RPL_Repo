using System.Net;
using System.Net.Mail;
using CEA_RPL.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CEA_RPL.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _config["SmtpSettings:Host"];
        var port = int.Parse(_config["SmtpSettings:Port"] ?? "587");
        var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");
        var username = _config["SmtpSettings:Username"];
        var password = _config["SmtpSettings:Password"];

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(username!, "CEA RPL Portal"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
