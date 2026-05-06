using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CEA_RPL.Services.Security
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly string _encryptionKey;

        public EncryptionService(IConfiguration configuration)
        {
            // Read key securely from config
            // Key should be injected from environment variables or secure vault in production
            _encryptionKey = configuration["EncryptionSettings:SecretKey"];

            if (string.IsNullOrEmpty(_encryptionKey))
            {
                throw new InvalidOperationException("Encryption key is missing from configuration.");
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = GetValidKey(_encryptionKey);
                    
                    // Generate secure IV automatically for each encryption
                    aesAlg.GenerateIV();

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // Store the IV at the beginning of the encrypted data
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        // Return Base64 encrypted string
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Encryption failed.", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = GetValidKey(_encryptionKey);

                    // Extract the IV from the beginning of the data
                    byte[] iv = new byte[aesAlg.BlockSize / 8];
                    if (cipherBytes.Length < iv.Length)
                    {
                        throw new CryptographicException("Invalid cipher text length.");
                    }
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                    aesAlg.IV = iv;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Decryption failed.", ex);
            }
        }

        // Helper to derive a valid 32-byte (256-bit) key for AES-256 from the configured string
        private byte[] GetValidKey(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }
    }
}
