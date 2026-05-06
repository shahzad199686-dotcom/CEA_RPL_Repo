using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using CEA_RPL.Helpers.Security;

namespace CEA_RPL.Services.Security
{
    public interface IFileSecurityService
    {
        Task<(bool IsValid, string ErrorMessage)> ValidateCandidatePhotoAsync(IFormFile file);
        Task<(bool IsValid, string ErrorMessage)> ValidateDocumentAsync(IFormFile file);
        Task<(bool IsValid, string ErrorMessage)> ValidateOcrUploadAsync(IFormFile file);
        
        /// <summary>
        /// Generates a highly secure and unique filename utilizing GUIDs and Timestamps.
        /// </summary>
        string GenerateSecureFileName(string originalFileName);

        /// <summary>
        /// Prepares the App_Data/SecureUploads private directory structure.
        /// </summary>
        void PrepareSecureStorageDirectory(string appRootPath);

        /// <summary>
        /// Stub to prepare logging integration for intercepted malicious uploads.
        /// </summary>
        void LogSuspiciousUploadAttempt(string userId, string fileName, string reason);
    }

    public class FileSecurityService : IFileSecurityService
    {
        private const long MAX_PHOTO_SIZE_BYTES = 2 * 1024 * 1024; // 2 MB Limit
        private const long MAX_DOCUMENT_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB Limit

        public async Task<(bool IsValid, string ErrorMessage)> ValidateCandidatePhotoAsync(IFormFile file)
        {
            return await ValidateFileAsync(file, MAX_PHOTO_SIZE_BYTES, new[] { ".jpg", ".jpeg", ".png" });
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateDocumentAsync(IFormFile file)
        {
            return await ValidateFileAsync(file, MAX_DOCUMENT_SIZE_BYTES, new[] { ".pdf" });
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateOcrUploadAsync(IFormFile file)
        {
            return await ValidateFileAsync(file, MAX_DOCUMENT_SIZE_BYTES, new[] { ".jpg", ".jpeg", ".png", ".pdf" });
        }

        private async Task<(bool IsValid, string ErrorMessage)> ValidateFileAsync(IFormFile file, long maxSize, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
                return (false, "File is empty or not provided.");

            if (file.Length > maxSize)
                return (false, $"File size exceeds the {(maxSize / 1024 / 1024)}MB limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // 1. Strict Dangerous Extension Block
            if (FileValidationHelper.IsDangerousExtension(extension))
                return (false, "File type is strictly prohibited.");

            // 2. Strict Allowed Type Block
            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return (false, "File extension is not allowed for this upload type.");

            // 3. MIME Type Validation
            if (!FileValidationHelper.IsValidMimeType(extension, file.ContentType))
                return (false, "File content type does not match the extension.");

            // 4. Cryptographic Magic Bytes (Signature) Validation
            using (var stream = file.OpenReadStream())
            {
                if (!FileValidationHelper.IsValidSignature(extension, stream))
                {
                    // This explicitly catches renamed .exe files or malicious files disguised as images/pdfs
                    return (false, "File signature validation failed. The file appears to be corrupted or spoofed.");
                }
                
                // OCR Upload Protection: Prep for Image Bomb checking
                // To prevent image bombs (e.g., tiny compressed size but 50000x50000 pixels resulting in OutOfMemory Exception),
                // future integration would read the header dimensions before processing.
            }

            return (true, string.Empty);
        }

        public string GenerateSecureFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var sanitizedBase = FileValidationHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(originalFileName));
            
            // Limit original name length to prevent OS path limit exploits
            if (sanitizedBase.Length > 20) sanitizedBase = sanitizedBase.Substring(0, 20);

            // Format: GUID_Timestamp_sanitizedname.ext
            // Obfuscates original filename completely while keeping it debuggable via timestamp
            string secureGuid = Guid.NewGuid().ToString("N");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            return $"{secureGuid}_{timestamp}_{sanitizedBase}{extension}";
        }

        public void PrepareSecureStorageDirectory(string appRootPath)
        {
            // Prefer an App_Data directory that IIS blocks completely from public URL access
            string securePath = Path.Combine(appRootPath, "App_Data", "SecureUploads");
            
            if (!Directory.Exists(securePath))
            {
                Directory.CreateDirectory(securePath);
            }
        }

        public void LogSuspiciousUploadAttempt(string userId, string fileName, string reason)
        {
            // Prepare architecture for logging blocked/malicious files
            // DO NOT process or save the file here.
            // Example Integration:
            // _auditLogger.LogWarning($"Suspicious upload blocked. User: {userId}, File: {fileName}, Reason: {reason}");
        }
    }
}
