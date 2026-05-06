using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CEA_RPL.Helpers.Security
{
    public static class FileValidationHelper
    {
        // Define allowed extensions and their corresponding MIME types and Magic Bytes (Signatures)
        private static readonly Dictionary<string, (string MimeType, List<byte[]> Signatures)> _allowedFileTypes = 
            new Dictionary<string, (string, List<byte[]>)>
        {
            { ".jpg", ("image/jpeg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
                })
            },
            { ".jpeg", ("image/jpeg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
                })
            },
            { ".png", ("image/png", new List<byte[]>
                {
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                })
            },
            { ".pdf", ("application/pdf", new List<byte[]>
                {
                    new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } // %PDF-
                })
            }
        };

        // Dangerous extensions to outright reject immediately
        private static readonly string[] _dangerousExtensions = { ".exe", ".bat", ".js", ".dll", ".svg", ".zip", ".cmd", ".ps1", ".html", ".aspx", ".php", ".sh" };

        public static bool IsDangerousExtension(string extension)
        {
            return _dangerousExtensions.Contains(extension.ToLowerInvariant());
        }

        public static bool IsAllowedExtension(string extension)
        {
            return _allowedFileTypes.ContainsKey(extension.ToLowerInvariant());
        }

        public static bool IsValidMimeType(string extension, string mimeType)
        {
            if (_allowedFileTypes.TryGetValue(extension.ToLowerInvariant(), out var typeInfo))
            {
                return typeInfo.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public static bool IsValidSignature(string extension, Stream fileStream)
        {
            if (!_allowedFileTypes.TryGetValue(extension.ToLowerInvariant(), out var typeInfo))
            {
                return false;
            }

            // Read enough bytes to cover the longest signature we have
            var maxSignatureLength = typeInfo.Signatures.Max(s => s.Length);
            byte[] headerBytes = new byte[maxSignatureLength];

            fileStream.Position = 0;
            var bytesRead = fileStream.Read(headerBytes, 0, headerBytes.Length);
            fileStream.Position = 0; // Reset position for next reader

            if (bytesRead < maxSignatureLength) return false;

            return typeInfo.Signatures.Any(signature => 
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
            return sanitized.Replace(" ", "_"); // Replace spaces with underscores
        }
    }
}
