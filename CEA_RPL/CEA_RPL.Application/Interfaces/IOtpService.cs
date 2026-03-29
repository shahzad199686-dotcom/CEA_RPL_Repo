namespace CEA_RPL.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string contactKey);
    Task<bool> VerifyOtpAsync(string contactKey, string otp);
}

public interface IOtpSender
{
    Task SendEmailOtpAsync(string email, string otp);
    Task SendSmsOtpAsync(string mobile, string otp);
}
