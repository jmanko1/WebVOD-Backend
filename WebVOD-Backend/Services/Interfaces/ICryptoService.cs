namespace WebVOD_Backend.Services.Interfaces;

public interface ICryptoService
{
    string Encrypt(string text);
    string Decrypt(string encryptedText);
    string HashPassword(string password);
    bool VerifyPassword(string inputPassword, string hashedPassword);
}
