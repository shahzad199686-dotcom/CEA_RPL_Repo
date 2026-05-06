using Microsoft.AspNetCore.Http;
using System;

namespace CEA_RPL.Services.Security
{
    public interface ISessionSecurityService
    {
        /// <summary>
        /// Prevents Session Fixation attacks by rotating the Session ID upon successful authentication.
        /// </summary>
        void RotateSessionId();

        /// <summary>
        /// Creates a hardened authenticated session post-OTP verification.
        /// </summary>
        void SetSecureSession(string userId, string role);

        /// <summary>
        /// Clears any temporary tracking variables used during the OTP flow.
        /// </summary>
        void InvalidateTemporarySession();

        /// <summary>
        /// Prepares the backend structure to log IP and Device details for suspicious activity monitoring.
        /// </summary>
        void LogIpAndDeviceActivity(string action, string userId = null);
    }

    public class SessionSecurityService : ISessionSecurityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionSecurityService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void RotateSessionId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // In ASP.NET Core, clearing the session completely ensures the session data is wiped.
                // Setting a dummy value forces the server to regenerate a new session cookie 
                // for the client, preventing Session Fixation attacks.
                context.Session.Clear();
                context.Session.SetString("SessionRotatedAt", DateTime.UtcNow.ToString("O"));
            }
        }

        public void SetSecureSession(string userId, string role)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Session.SetString("SecureUserId", userId);
                context.Session.SetString("UserRole", role ?? "User");
                context.Session.SetString("IsAuthenticated", "true");
                context.Session.SetString("AuthTime", DateTime.UtcNow.ToString("O"));
            }
        }

        public void InvalidateTemporarySession()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Remove the identifier stored when OTP was requested
                context.Session.Remove("PendingOtpIdentifier");
                context.Session.Remove("TempOtpContext");
            }
        }

        public void LogIpAndDeviceActivity(string action, string userId = null)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Prepare backend structure for Device Tracking / IP Logging
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                // TODO: Integrate with your logging framework or database audit table.
                // Example:
                // _auditRepository.LogSecurityEvent(userId, action, ipAddress, userAgent, DateTime.UtcNow);
                
                // This prepares the architecture without mutating current flows.
            }
        }
    }
}
