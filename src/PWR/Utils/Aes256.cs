using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PWR.Utils
{
    public enum CryptoError
    {
        EncryptionError,
        DecryptionError
    }

    public class CryptoException : Exception
    {
        public CryptoError ErrorType { get; }

        public CryptoException(CryptoError errorType, string message) : base(message)
        {
            ErrorType = errorType;
        }
    }

    public static class AES256
    {
        private const int IterationCount = 65536;
        private const int KeyLength = 32; // 256 bits
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("your-salt-value");
        private const int IvLength = 16;

        public static byte[] Encrypt(byte[] data, string password)
        {
            try
            {
                // Generate key using PBKDF2
                byte[] key = GenerateKey(password);

                // Generate random IV
                byte[] iv = new byte[IvLength];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                // Encrypt data
                byte[] encryptedData;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        encryptedData = ms.ToArray();
                    }
                }

                // Combine IV and encrypted data
                byte[] result = new byte[IvLength + encryptedData.Length];
                Buffer.BlockCopy(iv, 0, result, 0, IvLength);
                Buffer.BlockCopy(encryptedData, 0, result, IvLength, encryptedData.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new CryptoException(CryptoError.EncryptionError, ex.Message);
            }
        }

        public static byte[] Decrypt(byte[] encryptedDataWithIv, string password)
        {
            try
            {
                if (encryptedDataWithIv.Length <= IvLength)
                {
                    throw new CryptoException(CryptoError.DecryptionError, "Invalid input length");
                }

                // Generate key using PBKDF2
                byte[] key = GenerateKey(password);

                // Extract IV and encrypted data
                byte[] iv = new byte[IvLength];
                byte[] encryptedData = new byte[encryptedDataWithIv.Length - IvLength];
                
                Buffer.BlockCopy(encryptedDataWithIv, 0, iv, 0, IvLength);
                Buffer.BlockCopy(encryptedDataWithIv, IvLength, encryptedData, 0, encryptedData.Length);

                // Decrypt data
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedData, 0, encryptedData.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch (CryptoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CryptoException(CryptoError.DecryptionError, ex.Message);
            }
        }

        private static byte[] GenerateKey(string password)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                password,
                Salt,
                IterationCount,
                HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(KeyLength);
            }
        }
    }
}