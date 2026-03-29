using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Application.Interfaces;

public interface IAuthService
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> RegisterUserAsync(string email, string mobile, string password);
    Task<bool> ValidatePasswordAsync(User user, string password);
}
