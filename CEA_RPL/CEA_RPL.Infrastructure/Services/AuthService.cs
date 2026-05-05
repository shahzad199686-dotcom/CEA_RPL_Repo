using BCrypt.Net;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Domain.Entities;
using CEA_RPL.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CEA_RPL.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public AuthService(ApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<(bool Success, User? User, string ErrorMessage)> RegisterUserAsync(string firstName, string? middleName, string? lastName, string email, string mobile, string password)
    {
        var encryptedMobile = _encryptionService.Encrypt(mobile);
        
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.Mobile == encryptedMobile);
        if (existingUser != null)
        {
            if (existingUser.Email == email) return (false, null, "Email already registered.");
            if (existingUser.Mobile == encryptedMobile) return (false, null, "Mobile number already registered.");
        }

        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User 
            { 
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Email = email, 
                Mobile = encryptedMobile, 
                PasswordHash = hash, 
                Role = "Candidate" 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, user, string.Empty);
        }
        catch (DbUpdateException)
        {
            return (false, null, "A user with this email or mobile already exists.");
        }
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
