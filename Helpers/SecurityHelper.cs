using System.Security.Cryptography;
using System.Text;

namespace OfficeNexus.Helpers
{
    /// <summary>
    /// Security helper class for AES-256 encryption/decryption of sensitive data.
    /// 
    /// IMPORTANT: In production, the encryption key and IV should be stored securely:
    /// - Use Azure Key Vault, AWS Secrets Manager, or similar
    /// - Store in appsettings.json with User Secrets (development)
    /// - Never commit keys to source control
    /// - Rotate keys periodically
    /// </summary>
    public static class SecurityHelper
    {
        // TODO: In production, load these from configuration (appsettings.json, Azure Key Vault, etc.)
        // For now, using static values. In production, these MUST be externalized.
        // Key must be exactly 32 bytes (256 bits) for AES-256
        // NOTE: This test key is exactly 32 ASCII characters -> 32 bytes.
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
        
        // IV (Initialization Vector) must be exactly 16 bytes (128 bits) for AES
        private static readonly byte[] EncryptionIV = Encoding.UTF8.GetBytes("OfficeNexusIV16!"); // 16 bytes

        /// <summary>
        /// Encrypts a plain text string using AES-256 encryption.
        /// Returns Base64-encoded cipher text.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt</param>
        /// <returns>Base64-encoded encrypted string</returns>
        /// <exception cref="ArgumentNullException">Thrown when plainText is null</exception>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = EncryptionIV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        
                        // Return Base64-encoded string for safe storage in database
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error in production
                throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a Base64-encoded cipher text string using AES-256 decryption.
        /// Returns the original plain text.
        /// </summary>
        /// <param name="cipherText">The Base64-encoded encrypted string</param>
        /// <returns>Decrypted plain text string</returns>
        /// <exception cref="ArgumentNullException">Thrown when cipherText is null</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails (invalid cipher text)</exception>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = EncryptionIV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(cipherText);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Decryption failed: Invalid Base64 format. The data may be corrupted or not encrypted.", ex);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException($"Decryption failed: Cryptographic error. The encryption key may be incorrect or data is corrupted.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
            }
        }
    }
}

