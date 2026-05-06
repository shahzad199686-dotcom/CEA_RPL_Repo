using System;
using Microsoft.Extensions.Caching.Memory;

namespace CEA_RPL.Services.Security
{
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if an OTP can be resent based on a cooldown period.
        /// </summary>
        bool CanResendOtp(string identifier, out TimeSpan remainingCooldown);
        
        /// <summary>
        /// Records that an OTP was sent to trigger the cooldown.
        /// </summary>
        void RecordOtpResend(string identifier, int cooldownSeconds = 60);
        
        /// <summary>
        /// Checks if the user is allowed to attempt OTP verification (blocks brute force).
        /// </summary>
        bool CanAttemptVerification(string identifier, int maxAttempts = 5);
        
        /// <summary>
        /// Records a failed OTP attempt.
        /// </summary>
        void RecordFailedAttempt(string identifier, int blockMinutes = 15);
        
        /// <summary>
        /// Resets the attempt counter upon successful validation.
        /// </summary>
        void ResetAttempts(string identifier);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;

        public RateLimitService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool CanResendOtp(string identifier, out TimeSpan remainingCooldown)
        {
            string cacheKey = $"OtpResend_{identifier}";
            if (_cache.TryGetValue(cacheKey, out DateTime lastSentTime))
            {
                var timePassed = DateTime.UtcNow - lastSentTime;
                var cooldown = TimeSpan.FromSeconds(60); // 60 seconds default cooldown
                
                if (timePassed < cooldown)
                {
                    remainingCooldown = cooldown - timePassed;
                    return false;
                }
            }

            remainingCooldown = TimeSpan.Zero;
            return true;
        }

        public void RecordOtpResend(string identifier, int cooldownSeconds = 60)
        {
            string cacheKey = $"OtpResend_{identifier}";
            _cache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromSeconds(cooldownSeconds));
        }

        public bool CanAttemptVerification(string identifier, int maxAttempts = 5)
        {
            string cacheKey = $"OtpAttempts_{identifier}";
            if (_cache.TryGetValue(cacheKey, out int attempts))
            {
                if (attempts >= maxAttempts)
                {
                    return false; // Blocked
                }
            }
            return true;
        }

        public void RecordFailedAttempt(string identifier, int blockMinutes = 15)
        {
            string cacheKey = $"OtpAttempts_{identifier}";
            
            int attempts = _cache.GetOrCreate(cacheKey, entry => 
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(blockMinutes);
                return 0;
            });

            _cache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(blockMinutes));
        }

        public void ResetAttempts(string identifier)
        {
            string cacheKey = $"OtpAttempts_{identifier}";
            _cache.Remove(cacheKey);
        }
    }
}
