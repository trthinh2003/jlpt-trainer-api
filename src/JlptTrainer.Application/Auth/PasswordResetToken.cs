using System.Security.Cryptography;
using System.Text;

namespace JlptTrainer.Application.Auth;

internal static class PasswordResetToken
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
}
