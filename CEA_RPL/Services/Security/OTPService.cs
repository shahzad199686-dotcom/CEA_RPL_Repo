using System;

namespace CEA_RPL.Services.Security
{
    public interface IOtpSecurityService
    {
        /// <summary>
        /// Validates if the OTP is within the acceptable time window.
        /// </summary>
        bool IsOtpExpired(DateTime createdAt, int expiryMinutes = 5);

        /// <summary>
        /// Validates if the OTP has already been used (Replay Attack Prevention).
        /// </summary>
        bool IsOtpReplayed(bool isUsed);
    }

    public class OtpSecurityService : IOtpSecurityService
    {
        public bool IsOtpExpired(DateTime createdAt, int expiryMinutes = 5)
        {
            // OTP should expire after exactly X minutes (default 5)
            // Returns true if current UTC time is greater than the creation time + expiry duration
            return DateTime.UtcNow > createdAt.AddMinutes(expiryMinutes);
        }

        public bool IsOtpReplayed(bool isUsed)
        {
            // OTP must become invalid immediately after successful verification
            // This flag should be updated in the DB immediately upon successful validation
            return isUsed;
        }
    }
}
