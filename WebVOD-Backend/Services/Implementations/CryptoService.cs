﻿using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
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

    public string GenerateResetPasswordToken(int n)
    {
        byte[] token = new byte[n];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(token);
        }

        string base64 = Convert.ToBase64String(token);

        string urlSafe = base64
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return urlSafe;
    }

    public string Sha256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            return Convert.ToBase64String(hashBytes);
        }
    }

    public string HashPassword(string password)
    {
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        var config = new Argon2Config
        {
            Password = Encoding.UTF8.GetBytes(password),
            Salt = saltBytes
        };

        using var hasher = new Argon2(config);
        using var hashBytes = hasher.Hash();

        var base64Hash = Convert.ToBase64String(hashBytes.Buffer);
        var base64Salt = Convert.ToBase64String(saltBytes);

        return base64Salt + base64Hash;
    }

    public bool VerifyPassword(string inputPassword, string hashedPassword)
    {
        var saltBase64 = hashedPassword.Substring(0, 24);
        var storedHash = hashedPassword.Substring(24);

        byte[] saltBytes = Convert.FromBase64String(saltBase64);

        var config = new Argon2Config
        {
            Password = Encoding.UTF8.GetBytes(inputPassword),
            Salt = saltBytes
        };

        using var hasher = new Argon2(config);
        using var hashBytes = hasher.Hash();

        var base64Hash = Convert.ToBase64String(hashBytes.Buffer);

        return base64Hash == storedHash;
    }
}
