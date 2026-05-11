using System.Text.RegularExpressions;

namespace BenServicesPlatform.Api.Services;

public static partial class PasswordPolicyValidator
{
    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex SpecialCharacterRegex();

    public static bool IsStrong(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Password is required.";
            return false;
        }

        if (password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters long.";
            return false;
        }

        if (!UppercaseRegex().IsMatch(password))
        {
            errorMessage = "Password must contain at least one uppercase letter.";
            return false;
        }

        if (!LowercaseRegex().IsMatch(password))
        {
            errorMessage = "Password must contain at least one lowercase letter.";
            return false;
        }

        if (!DigitRegex().IsMatch(password))
        {
            errorMessage = "Password must contain at least one number.";
            return false;
        }

        if (!SpecialCharacterRegex().IsMatch(password))
        {
            errorMessage = "Password should contain at least one special character.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
