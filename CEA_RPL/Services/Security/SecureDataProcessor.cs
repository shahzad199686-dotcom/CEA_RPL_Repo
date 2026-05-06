using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using CEA_RPL.Helpers.Security;

namespace CEA_RPL.Services.Security
{
    public interface ISecureDataProcessor
    {
        /// <summary>
        /// Safely decrypts a value for internal backend processing ONLY.
        /// NEVER return this value to the frontend or log it.
        /// </summary>
        string SafeDecrypt(string encryptedValue, string fieldNameForLogging = "Unknown");

        /// <summary>
        /// Securely compares a plain text input against an encrypted database value
        /// without exposing the decrypted value outside this method.
        /// </summary>
        bool SecureCompare(string encryptedDbValue, string plainTextInput, string fieldNameForLogging = "Unknown");

        /// <summary>
        /// Safely decrypts and then instantly masks the value, ready to be sent to the frontend or logged safely.
        /// </summary>
        string GetMaskedValueForFrontend(string encryptedValue, string type);
    }

    public class SecureDataProcessor : ISecureDataProcessor
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SecureDataProcessor> _logger;

        public SecureDataProcessor(IEncryptionService encryptionService, ILogger<SecureDataProcessor> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public string SafeDecrypt(string encryptedValue, string fieldNameForLogging = "Unknown")
        {
            if (string.IsNullOrEmpty(encryptedValue))
                return string.Empty;

            try
            {
                return _encryptionService.Decrypt(encryptedValue);
            }
            catch (CryptographicException ex)
            {
                // CRITICAL LOGGING RULE: NEVER log the raw encrypted value or the decrypted value.
                _logger.LogError(ex, "Cryptographic failure while decrypting field: {FieldName}. The data may be corrupted, spoofed, or keys may be mismatched.", fieldNameForLogging);
                
                // Return null so the calling service knows the decryption fundamentally failed.
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected runtime error decrypting field: {FieldName}.", fieldNameForLogging);
                return null;
            }
        }

        public bool SecureCompare(string encryptedDbValue, string plainTextInput, string fieldNameForLogging = "Unknown")
        {
            if (string.IsNullOrEmpty(encryptedDbValue) || string.IsNullOrEmpty(plainTextInput))
                return false;

            string decryptedDbValue = SafeDecrypt(encryptedDbValue, fieldNameForLogging);
            
            if (decryptedDbValue == null)
            {
                // Decryption failed securely. Do not throw exception to caller, just fail the validation.
                return false;
            }

            // Secure validation comparison
            // Use StringComparison.Ordinal for exact, culture-agnostic matching on sensitive identifiers
            return string.Equals(decryptedDbValue, plainTextInput, StringComparison.Ordinal);
        }

        public string GetMaskedValueForFrontend(string encryptedValue, string type)
        {
            string decryptedValue = SafeDecrypt(encryptedValue, type);

            if (string.IsNullOrEmpty(decryptedValue))
                return "ERROR"; // Do not expose cryptographic failure reasons to frontend

            switch (type.ToLowerInvariant())
            {
                case "aadhaar":
                    return MaskingHelper.MaskAadhaar(decryptedValue);
                case "pan":
                    return MaskingHelper.MaskPAN(decryptedValue);
                case "mobile":
                    return MaskingHelper.MaskMobile(decryptedValue);
                // Note: Add MaskEmail() to MaskingHelper.cs if Email masking is required on the frontend.
                default:
                    // Generic fallback mask if type is unknown
                    return new string('*', decryptedValue.Length > 4 ? decryptedValue.Length - 4 : decryptedValue.Length) 
                           + (decryptedValue.Length > 4 ? decryptedValue.Substring(decryptedValue.Length - 4) : "");
            }
        }
    }
}
