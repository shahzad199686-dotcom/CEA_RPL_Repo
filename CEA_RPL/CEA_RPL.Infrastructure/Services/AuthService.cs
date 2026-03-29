using BCrypt.Net;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Domain.Entities;
using CEA_RPL.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CEA_RPL.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> RegisterUserAsync(string email, string mobile, string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Email = email, Mobile = mobile, PasswordHash = hash };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public Task<bool> ValidatePasswordAsync(User user, string password)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }
}
