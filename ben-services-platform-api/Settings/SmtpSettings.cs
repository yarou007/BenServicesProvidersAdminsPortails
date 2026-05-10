namespace BenServicesPlatform.Api.Settings;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Ben's Services";
    public string FrontendLoginUrl { get; set; } = "http://localhost:4200/login";

    public void ValidateForCredentialEmails()
    {
        if (string.IsNullOrWhiteSpace(Host)
            || Port <= 0
            || string.IsNullOrWhiteSpace(User)
            || string.IsNullOrWhiteSpace(Password)
            || string.IsNullOrWhiteSpace(FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are incomplete. Configure SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASSWORD, SMTP_FROM_EMAIL.");
        }
    }
}
