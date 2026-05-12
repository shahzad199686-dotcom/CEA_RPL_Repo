using System.Net;
using System.Net.Mail;
using CEA_RPL.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CEA_RPL.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _config["SmtpSettings:Host"];
        var port = int.Parse(_config["SmtpSettings:Port"] ?? "587");
        var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");
        var username = _config["SmtpSettings:Username"];
        var password = _config["SmtpSettings:Password"];

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000 // 20 seconds timeout
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username!, "CEA RPL Portal"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false // Force plain text for better deliverability
            };
            mailMessage.To.Add(to);

            // Add basic headers to improve deliverability
            mailMessage.Headers.Add("X-Priority", "1"); // High Priority
            mailMessage.Headers.Add("Importance", "High");

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Recipient}", to);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP Error sending email to {Recipient}. Status Code: {StatusCode}, Message: {SmtpMessage}", 
                to, smtpEx.StatusCode, smtpEx.Message);
            throw; // Re-throw to handle in caller if needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General Error sending email to {Recipient}", to);
            throw;
        }
    }
}
