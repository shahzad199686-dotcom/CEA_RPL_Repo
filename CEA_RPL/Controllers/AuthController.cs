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
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Application");
        return View();
    }

    [HttpGet("Auth/SignUp")]
    public IActionResult SignUp()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Application");
        return View();
    }

    [HttpGet("Auth/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("api/auth/signup")]
    public async Task<IActionResult> SignUpApi([FromForm] string email, [FromForm] string mobile, [FromForm] string password)
    {
        var result = await _authService.RegisterUserAsync(email, mobile, password);
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
                new Claim(ClaimTypes.MobilePhone, user.Mobile)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        return Ok(new { message = "OTP Verified and signed in." });
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
