using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Application.Interfaces;

public interface IAuthService
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<(bool Success, User? User, string ErrorMessage)> RegisterUserAsync(string firstName, string? middleName, string? lastName, string email, string mobile, string password);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task UpdateVerificationStatusAsync(string email, bool emailVerified, bool mobileVerified);
}
