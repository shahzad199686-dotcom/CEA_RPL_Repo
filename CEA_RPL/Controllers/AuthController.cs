using System.Security.Claims;
using CEA_RPL.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CEA_RPL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IOtpSender _otpSender;

    public AuthController(IAuthService authService, IOtpService otpService, IOtpSender otpSender)
    {
        _authService = authService;
        _otpService = otpService;
        _otpSender = otpSender;
    }

    [HttpGet("Auth/Login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Finance")) return RedirectToAction("Dashboard", "Finance");
            return RedirectToAction("Index", "Application");
        }
        return View();
    }

    [HttpGet("Auth/SignUp")]
    public IActionResult SignUp()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            return RedirectToAction("Index", "Application");
        }
        return View();
    }

    [HttpGet("Candidate/Login")]
    public IActionResult CandidateLogin() => View("Login");

    [HttpGet("Candidate/Register")]
    public IActionResult CandidateRegister() => View("SignUp");

    [HttpGet("Admin/Login")]
    public IActionResult AdminLogin() 
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin")) 
            return RedirectToAction("Dashboard", "Admin");
            
        ViewBag.Role = "Admin";
        return View();
    }

    [HttpGet("Auth/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("api/auth/signup")]
    public async Task<IActionResult> SignUpApi([FromForm] string firstName, [FromForm] string? middleName, [FromForm] string? lastName, [FromForm] string email, [FromForm] string mobile, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
            return BadRequest(new { message = "Validation failed: Please enter a valid first name" });

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2)
            return BadRequest(new { message = "Validation failed: Please enter a valid last name" });

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(new { message = "Validation failed: Please enter a valid email address" });

        if (string.IsNullOrWhiteSpace(mobile) || !System.Text.RegularExpressions.Regex.IsMatch(mobile, "^[6-9][0-9]{9}$"))
            return BadRequest(new { message = "Validation failed: Please enter a valid 10-digit mobile number" });

        if (!IsValidPassword(password))
            return BadRequest(new { message = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character." });

        // IMPORTANT: Verify that the email was previously verified via OTP
        var isVerified = await _otpService.VerifyOtpAsync(email, "CHECK_VERIFIED");
        if (!isVerified) 
            return BadRequest(new { message = "Please verify your email address via OTP before completing registration." });

        var result = await _authService.RegisterUserAsync(firstName, middleName, lastName, email, mobile, password);
        if (!result.Success) 
            return BadRequest(new { message = result.ErrorMessage });

        // Update user's verification status once created
        await _authService.UpdateVerificationStatusAsync(email, true, true);

        // Sign in automatically after registration
        var user = result.User;
        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.Mobile),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        return Ok(new { message = "Account created and verified successfully.", redirect = "/Application/Index" });
    }

    [HttpPost("api/auth/sendotp")]
    public async Task<IActionResult> SendOtp([FromForm] string email)
    {
        if (string.IsNullOrEmpty(email)) return BadRequest(new { message = "Email missing." });

        // Cooldown check (60 seconds)
        var canSend = await _otpService.VerifyOtpAsync(email, "CHECK_COOLDOWN");
        if (!canSend) return BadRequest(new { message = "Please wait before requesting a new OTP." });
        
        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
            return Ok(new { message = "OTP sent to your email." });
        } catch (Exception) {
            return BadRequest(new { message = "Failed to send OTP. Please try again later." });
        }
    }

    [HttpPost("api/auth/verifyotp")]
    public async Task<IActionResult> VerifyOtp([FromForm] string email, [FromForm] string? emailOtp, [FromForm] string? requiredRole)
    {
        if (string.IsNullOrEmpty(emailOtp))
        {
            return BadRequest(new { message = "Please provide the Email OTP." });
        }

        var emailValid = await _otpService.VerifyOtpAsync(email, emailOtp);
        if (!emailValid) return BadRequest(new { message = "Invalid or expired OTP." });
        
        var user = await _authService.GetUserByEmailAsync(email);
        
        // If user exists (Login flow), sign them in
        if (user != null)
        {
            if (!string.IsNullOrEmpty(requiredRole) && user.Role != requiredRole)
            {
                return Unauthorized(new { message = $"This account is not authorized for {requiredRole} login." });
            }

            await _authService.UpdateVerificationStatusAsync(email, true, true);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.Mobile),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            
            return Ok(new { message = "Verification successful.", role = user.Role });
        }

        // If user doesn't exist (SignUp flow), just confirm verification
        return Ok(new { message = "Email verified! You can now complete your profile registration." });
    }

    [HttpPost("api/auth/signin")]
    public async Task<IActionResult> SignInApi([FromForm] string email, [FromForm] string password, [FromForm] string? requiredRole)
    {
        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials." });

        if (!string.IsNullOrEmpty(requiredRole) && user.Role != requiredRole)
        {
            return Unauthorized(new { message = $"This account is not authorized for {requiredRole} login." });
        }

        var valid = await _authService.ValidatePasswordAsync(user, password);
        if (!valid) return Unauthorized(new { message = "Invalid credentials." });

        // Trigger OTP
        string warning = "";
        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
        } catch {
            // Log error but don't expose to user
        }

        return Ok(new { message = "Password correct." + warning, requiresVerification = true, email = email });
    }
    
    [HttpGet("Auth/ForgotPassword")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("api/auth/reset-request")]
    public async Task<IActionResult> ResetRequest([FromForm] string email)
    {
        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return Ok(new { message = "If your email is registered, you will receive an OTP." });

        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
            return Ok(new { message = "OTP sent to your registered email." });
        } catch {
            return BadRequest(new { message = "Failed to send reset code. Please try again." });
        }
    }

    [HttpPost("api/auth/reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] string email, [FromForm] string otp, [FromForm] string newPassword)
    {
        if (!IsValidPassword(newPassword))
            return BadRequest(new { message = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character." });

        var isValid = await _otpService.VerifyOtpAsync(email, otp);
        if (!isValid) return BadRequest(new { message = "Invalid or expired OTP." });

        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return BadRequest(new { message = "User not found." });

        var success = await _authService.UpdatePasswordAsync(user, newPassword);
        if (!success) return BadRequest(new { message = "Failed to update password. Please try again." });

        return Ok(new { message = "Password updated successfully. You can now login." });
    }

    [HttpGet("api/auth/status")]
    public IActionResult Status()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok(new { 
                authenticated = true, 
                email = User.FindFirstValue(ClaimTypes.Email),
                mobile = User.FindFirstValue(ClaimTypes.MobilePhone)
            });
        }
        return Ok(new { authenticated = false });
    }

    private bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");
    }
}
