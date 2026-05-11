using System.Net;
using System.Net.Mail;
using BenServicesPlatform.Api.Settings;

namespace BenServicesPlatform.Api.Services;

public class SmtpEmailService(
    SmtpSettings smtpSettings,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private const int SmtpTimeoutMilliseconds = 15000;

    public async Task SendAdminCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        smtpSettings.ValidateForCredentialEmails();

        var subject = "Your Ben's Services Admin Account";
        var body = $"""
Hello {recipientName},

An admin account has been created for you on Ben's Services Providers Admin Portal.

Login URL: {smtpSettings.FrontendLoginUrl}
Username/Login: {username}
Temporary password: {temporaryPassword}

For security reasons, please change your password after your first login.

Thank you.
""";

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpSettings.FromEmail, smtpSettings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        mailMessage.To.Add(recipientEmail);

        using var smtpClient = new SmtpClient(smtpSettings.Host, smtpSettings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpSettings.User, smtpSettings.Password),
            Timeout = SmtpTimeoutMilliseconds
        };

        logger.LogInformation(
            "Starting SMTP send for admin credentials email to RecipientEmail={RecipientEmail}",
            recipientEmail);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SmtpTimeoutMilliseconds);

        try
        {
            timeoutCts.Token.ThrowIfCancellationRequested();
            await smtpClient.SendMailAsync(mailMessage, timeoutCts.Token);
            logger.LogInformation(
                "Completed SMTP send for admin credentials email to RecipientEmail={RecipientEmail}",
                recipientEmail);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "SMTP send failed for admin credentials email to RecipientEmail={RecipientEmail}",
                recipientEmail);
            throw;
        }
    }
}
