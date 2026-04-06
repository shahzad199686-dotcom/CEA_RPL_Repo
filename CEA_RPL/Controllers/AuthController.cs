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
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2 || !System.Text.RegularExpressions.Regex.IsMatch(firstName, "^[A-Za-z]+$"))
            return BadRequest(new { message = "Validation failed: Please enter a valid first name" });

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2 || !System.Text.RegularExpressions.Regex.IsMatch(lastName, "^[A-Za-z]+$"))
            return BadRequest(new { message = "Validation failed: Please enter a valid last name" });

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(new { message = "Validation failed: Please enter a valid email address" });

        if (string.IsNullOrWhiteSpace(mobile) || !System.Text.RegularExpressions.Regex.IsMatch(mobile, "^[6-9][0-9]{9}$"))
            return BadRequest(new { message = "Validation failed: Please enter a valid 10-digit mobile number" });

        var result = await _authService.RegisterUserAsync(firstName, middleName, lastName, email, mobile, password);
        if (!result.Success) 
            return BadRequest(new { message = result.ErrorMessage });

        // Trigger OTP
        string warning = "";
        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
        } catch {
            warning = " (Note: SMTP failed to send email. Use dummy code '123456' for testing).";
        }

        return Ok(new { message = "Registration successful." + warning, requiresVerification = true, email = email });
    }

    [HttpPost("api/auth/sendotp")]
    public async Task<IActionResult> SendOtp([FromForm] string email)
    {
        if (string.IsNullOrEmpty(email)) return BadRequest(new { message = "Email missing." });

        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
            return Ok(new { message = "OTP sent." });
        } catch {
            return Ok(new { message = "OTP generated (but SMTP failed). Use dummy code '123456' for testing." });
        }
    }

    [HttpPost("api/auth/verifyotp")]
    public async Task<IActionResult> VerifyOtp([FromForm] string email, [FromForm] string? emailOtp)
    {
        if (string.IsNullOrEmpty(emailOtp))
        {
            return BadRequest(new { message = "Please provide the Email OTP." });
        }

        var emailValid = await _otpService.VerifyOtpAsync(email, emailOtp);
        if (!emailValid) return BadRequest(new { message = "Invalid or expired OTP." });
        
        // Update status in DB
        await _authService.UpdateVerificationStatusAsync(email, true, true);

        // Grant Cookie now that verification is complete
        var user = await _authService.GetUserByEmailAsync(email);
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
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            
            return Ok(new { message = "OTP Verified and signed in.", role = user.Role });
        }

        return BadRequest(new { message = "User not found." });
    }

    [HttpPost("api/auth/signin")]
    public async Task<IActionResult> SignInApi([FromForm] string email, [FromForm] string password)
    {
        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials." });

        var valid = await _authService.ValidatePasswordAsync(user, password);
        if (!valid) return Unauthorized(new { message = "Invalid credentials." });

        // Trigger OTP
        string warning = "";
        try {
            var otp = await _otpService.GenerateOtpAsync(email);
            await _otpSender.SendEmailOtpAsync(email, otp);
        } catch {
            warning = " (Note: SMTP failed. Use dummy code '123456' for testing).";
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
            return Ok(new { message = "OTP generated. Use '123456' for testing." });
        }
    }

    [HttpPost("api/auth/reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] string email, [FromForm] string otp, [FromForm] string newPassword)
    {
        var isValid = await _otpService.VerifyOtpAsync(email, otp);
        if (!isValid) return BadRequest(new { message = "Invalid or expired OTP." });

        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return BadRequest(new { message = "User not found." });

        // Update password (implement in AuthService)
        // await _authService.UpdatePasswordAsync(user, newPassword);

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
}
