using System.Security.Claims;
using CEA_RPL.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CEA_RPL.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
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

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromForm] string email, [FromForm] string mobile, [FromForm] string password)
    {
        var existing = await _authService.GetUserByEmailAsync(email);
        if (existing != null) return BadRequest(new { message = "Email already registered." });

        var user = await _authService.RegisterUserAsync(email, mobile, password);
        return Ok(new { message = "Account created. Please login." });
    }

    [HttpPost("sendotp")]
    public async Task<IActionResult> SendOtp([FromForm] string email, [FromForm] string mobile, [FromForm] string type)
    {
        // For simplicity, we just send to whatever was requested
        var contact = type == "email" ? email : mobile;
        if (string.IsNullOrEmpty(contact)) return BadRequest(new { message = "Contact missing." });

        var otp = await _otpService.GenerateOtpAsync(contact);
        
        if (type == "email")
            await _otpSender.SendEmailOtpAsync(email, otp);
        else
            await _otpSender.SendSmsOtpAsync(mobile, otp);

        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("verifyotp")]
    public async Task<IActionResult> VerifyOtp([FromForm] string email, [FromForm] string mobile, [FromForm] string? emailOtp, [FromForm] string? smsOtp)
    {
        // Verify both if provided
        if (!string.IsNullOrEmpty(emailOtp))
        {
            var valid = await _otpService.VerifyOtpAsync(email, emailOtp);
            if (!valid) return BadRequest(new { message = "Invalid or expired Email OTP." });
        }
        
        if (!string.IsNullOrEmpty(smsOtp))
        {
            var valid = await _otpService.VerifyOtpAsync(mobile, smsOtp);
            if (!valid) return BadRequest(new { message = "Invalid or expired SMS OTP." });
        }

        return Ok(new { message = "OTP Verified." });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromForm] string email, [FromForm] string password)
    {
        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials." });

        var valid = await _authService.ValidatePasswordAsync(user, password);
        if (!valid) return Unauthorized(new { message = "Invalid credentials." });

        // NOTE: In a real system, you'd enforce OTP verification BEFORE signing them in.
        // Based on the UI flow, they sign in, THEN verify OTP, then unlock the form.
        // We will grant the cookie now, but the form submission will require the Cookie.

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.MobilePhone, user.Mobile)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { message = "Signed in successfully." });
    }
    
    [HttpGet("status")]
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
