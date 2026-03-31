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

    public async Task<(bool Success, User? User, string ErrorMessage)> RegisterUserAsync(string email, string mobile, string password)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.Mobile == mobile);
        if (existingUser != null)
        {
            if (existingUser.Email == email) return (false, null, "Email already registered.");
            if (existingUser.Mobile == mobile) return (false, null, "Mobile number already registered.");
        }

        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Email = email, Mobile = mobile, PasswordHash = hash };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, user, string.Empty);
        }
        catch (DbUpdateException)
        {
            return (false, null, "A user with this email or mobile already exists.");
        }
    }

    public Task<bool> ValidatePasswordAsync(User user, string password)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }

    public async Task UpdateVerificationStatusAsync(string email, bool emailVerified, bool mobileVerified)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null)
        {
            if (emailVerified) user.IsEmailVerified = true;
            if (mobileVerified) user.IsMobileVerified = true;
            await _context.SaveChangesAsync();
        }
    }
}
