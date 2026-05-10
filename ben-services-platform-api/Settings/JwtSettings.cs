namespace BenServicesPlatform.Api.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
        {
            throw new InvalidOperationException("JWT secret is missing. Set JWT_SECRET.");
        }

        if (Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("JWT issuer is missing. Set JWT_ISSUER.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT audience is missing. Set JWT_AUDIENCE.");
        }

        if (ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiration must be a positive number of minutes.");
        }
    }
}
