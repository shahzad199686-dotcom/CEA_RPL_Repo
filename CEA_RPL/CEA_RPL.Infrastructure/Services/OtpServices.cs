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
        
        // Invalidate previous unverified OTPs for this contact
        var existing = await _context.OtpRecords
            .Where(o => o.ContactKey == contactKey && !o.IsVerified)
            .ToListAsync();
        foreach (var old in existing) old.IsVerified = true; // Mark as invalidated

        var record = new Domain.Entities.OtpRecord
        {
            ContactKey = contactKey,
            OtpCode = otp,
            ExpiryTime = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false
        };

        _context.OtpRecords.Add(record);
        await _context.SaveChangesAsync();
        return otp;
    }

    public async Task<bool> VerifyOtpAsync(string contactKey, string otp)
    {
        // Special check for cooldown (returns true if allowed to send again)
        if (otp == "CHECK_COOLDOWN")
        {
            var last = await _context.OtpRecords
                .Where(o => o.ContactKey == contactKey)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (last == null) return true;
            return (DateTime.UtcNow - last.CreatedAt).TotalSeconds > 60;
        }

        // Special check for registration flow (has this email been verified recently?)
        if (otp == "CHECK_VERIFIED")
        {
            return await _context.OtpRecords
                .AnyAsync(o => o.ContactKey == contactKey && o.IsVerified && o.ExpiryTime > DateTime.UtcNow.AddMinutes(-10));
        }

        // Removed global dummy code for security

        var record = await _context.OtpRecords
            .Where(o => o.ContactKey == contactKey && o.OtpCode == otp && !o.IsVerified && o.ExpiryTime > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (record != null)
        {
            record.IsVerified = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}

public class SmtpOtpSender : IOtpSender
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SmtpOtpSender> _logger;

    public SmtpOtpSender(IEmailService emailService, ILogger<SmtpOtpSender> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendEmailOtpAsync(string email, string otp)
    {
        try
        {
            var subject = "CEA RPL Portal - OTP Verification";
            var body = $@"Your OTP is: {otp}
This OTP is valid for 5 minutes. Do not share it with anyone.";

            await _emailService.SendEmailAsync(email, subject, body);
            _logger.LogInformation($"OTP email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send OTP email to {email}");
            throw;
        }
    }

    public Task SendSmsOtpAsync(string mobile, string otp)
    {
        _logger.LogWarning("SMS OTP requested but functionality is disabled.");
        return Task.CompletedTask;
    }
}
