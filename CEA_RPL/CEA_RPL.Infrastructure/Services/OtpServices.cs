using System.Collections.Concurrent;
using CEA_RPL.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CEA_RPL.Infrastructure.Services;

public class InMemoryOtpService : IOtpService
{
    private readonly ConcurrentDictionary<string, (string otp, DateTime expiry)> _cache = new();

    public Task<string> GenerateOtpAsync(string contactKey)
    {
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();
        _cache[contactKey] = (otp, DateTime.UtcNow.AddMinutes(10));
        return Task.FromResult(otp);
    }

    public Task<bool> VerifyOtpAsync(string contactKey, string otp)
    {
        if (_cache.TryGetValue(contactKey, out var record))
        {
            if (record.otp == otp && record.expiry > DateTime.UtcNow)
            {
                _cache.TryRemove(contactKey, out _); // OTP used successfully
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}

public class ConsoleOtpSender : IOtpSender
{
    private readonly ILogger<ConsoleOtpSender> _logger;

    public ConsoleOtpSender(ILogger<ConsoleOtpSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailOtpAsync(string email, string otp)
    {
        _logger.LogWarning("=============== SIMULATED EMAIL ===============");
        _logger.LogWarning($"To: {email}");
        _logger.LogWarning($"Subject: Your CEA RPL Verification OTP");
        _logger.LogWarning($"Body: Your OTP code is {otp}. Valid for 10 minutes.");
        _logger.LogWarning("===============================================");
        return Task.CompletedTask;
    }

    public Task SendSmsOtpAsync(string mobile, string otp)
    {
        _logger.LogWarning("=============== SIMULATED SMS ===============");
        _logger.LogWarning($"To: {mobile}");
        _logger.LogWarning($"Message: Your security code is {otp}");
        _logger.LogWarning("=============================================");
        return Task.CompletedTask;
    }
}
