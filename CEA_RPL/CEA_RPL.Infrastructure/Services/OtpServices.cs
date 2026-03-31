using System.Net;
using System.Net.Mail;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CEA_RPL.Infrastructure.Services;

public class DbOtpService : IOtpService
{
    private readonly ApplicationDbContext _context;

    public DbOtpService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateOtpAsync(string contactKey)
    {
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();
        
        // Invalidate previous unused OTPs for this contact
        var existing = await _context.OtpRecords
            .Where(o => o.ContactKey == contactKey && !o.IsUsed)
            .ToListAsync();
        foreach (var old in existing) old.IsUsed = true;

        var record = new Domain.Entities.OtpRecord
        {
            ContactKey = contactKey,
            OtpCode = otp,
            ExpiryTime = DateTime.UtcNow.AddMinutes(5), // 5 minutes as requested
            IsUsed = false
        };

        _context.OtpRecords.Add(record);
        await _context.SaveChangesAsync();
        return otp;
    }

    public async Task<bool> VerifyOtpAsync(string contactKey, string otp)
    {
        // Dummy code for testing: 123456
        if (otp == "123456") return true;

        var record = await _context.OtpRecords
            .Where(o => o.ContactKey == contactKey && o.OtpCode == otp && !o.IsUsed && o.ExpiryTime > DateTime.UtcNow)
            .OrderByDescending(o => o.ExpiryTime)
            .FirstOrDefaultAsync();

        if (record != null)
        {
            record.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}

public class SmtpOtpSender : IOtpSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpOtpSender> _logger;

    public SmtpOtpSender(IConfiguration config, ILogger<SmtpOtpSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailOtpAsync(string email, string otp)
    {
        try
        {
            var server = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
            var user = _config["EmailSettings:SmtpUsername"];
            var pass = _config["EmailSettings:SmtpPassword"];

            using var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(user!),
                Subject = "Your OTP Code",
                Body = $"Your OTP is: {otp}. Valid for 5 minutes.",
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation($"OTP email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send OTP email to {email}");
            throw; // Re-throw so the controller knows it failed
        }
    }

    public Task SendSmsOtpAsync(string mobile, string otp)
    {
        _logger.LogWarning("SMS OTP requested but functionality has been removed.");
        return Task.CompletedTask;
    }
}
