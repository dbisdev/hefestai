using System.Security.Cryptography;
using System.Text;

namespace Loremaster.Shared.Helpers;

public static class SecurityHelper
{
    public static string GenerateRandomToken(int length = 64)
    {
        var randomNumber = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public static string HashSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GenerateSlug(int length = 8)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random[i] % chars.Length];
        }
        return new string(result);
    }
}
