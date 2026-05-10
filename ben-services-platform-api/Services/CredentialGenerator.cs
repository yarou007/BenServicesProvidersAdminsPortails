using System.Security.Cryptography;
using System.Text;

namespace BenServicesPlatform.Api.Services;

public static class CredentialGenerator
{
    private const string LowercaseCharacters = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string SpecialCharacters = "!@#$%^&*()-_=+[]{}:,.?";
    private const string UsernameAllowedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789._";

    public static string GenerateTemporaryPassword(int length = 12)
    {
        if (length < 12)
        {
            length = 12;
        }

        var requiredCharacters = new List<char>
        {
            GetRandomCharacter(UppercaseCharacters),
            GetRandomCharacter(LowercaseCharacters),
            GetRandomCharacter(Digits),
            GetRandomCharacter(SpecialCharacters)
        };

        var allCharacters = LowercaseCharacters + UppercaseCharacters + Digits + SpecialCharacters;
        while (requiredCharacters.Count < length)
        {
            requiredCharacters.Add(GetRandomCharacter(allCharacters));
        }

        Shuffle(requiredCharacters);
        return new string(requiredCharacters.ToArray());
    }

    public static string SanitizeUsernameBase(string email)
    {
        var prefix = email.Split('@')[0].Trim().ToLowerInvariant();
        var builder = new StringBuilder(prefix.Length);

        foreach (var character in prefix)
        {
            if (UsernameAllowedCharacters.Contains(character))
            {
                builder.Append(character);
            }
        }

        var username = builder.ToString().Trim('.');
        return string.IsNullOrWhiteSpace(username) ? "admin" : username;
    }

    private static char GetRandomCharacter(string charset)
    {
        var index = RandomNumberGenerator.GetInt32(charset.Length);
        return charset[index];
    }

    private static void Shuffle(List<char> characters)
    {
        for (var index = characters.Count - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
        }
    }
}
