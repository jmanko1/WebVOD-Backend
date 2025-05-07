using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Security.Cryptography;
using System.Text;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class CryptoService : ICryptoService
{
    private readonly IConfiguration _configuration;

    public CryptoService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Decrypt(string encryptedText)
    {
        var secretKey = _configuration.GetSection("AES").GetValue<string>("SecretKey");

        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(secretKey);

            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherText = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

            aes.IV = iv;

            using (MemoryStream memoryStream = new MemoryStream(cipherText))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (StreamReader reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }

    public string Encrypt(string text)
    {
        var secretKey = _configuration.GetSection("AES").GetValue<string>("SecretKey");

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(secretKey);
            aes.GenerateIV();

            byte[] iv = aes.IV;
            byte[] encrypted;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(text);
                    }
                    encrypted = memoryStream.ToArray();
                }
            }

            byte[] result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
    }

    public string HashPassword(string password)
    {
        throw new NotImplementedException();
    }

    public bool VerifyPassword(string inputPassword, string hashedPassword)
    {
        throw new NotImplementedException();
    }
}
