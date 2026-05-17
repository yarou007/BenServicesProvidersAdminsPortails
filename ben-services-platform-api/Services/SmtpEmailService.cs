using System.Net;
using System.Net.Mail;
using BenServicesPlatform.Api.Settings;

namespace BenServicesPlatform.Api.Services;

public class SmtpEmailService(SmtpSettings smtpSettings) : IEmailService
{
    public Task SendAdminCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
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

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public Task SendProviderApplicationReceivedAsync(
        string recipientName,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        var subject = "Your Ben's Services Provider Application Was Received";
        var body = $"""
Hello {recipientName},

Thank you for applying to join the Ben's Services Provider Network.

Your application has been received and is currently pending admin review.
You will receive another email once the review process is completed.

No login credentials are issued at this stage.

Thank you.
""";

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public Task SendProviderApplicationUnderReviewAsync(
        string recipientName,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        var subject = "Your Ben's Services Application Is Under Review";
        var body = $"""
Hello {recipientName},

Your provider application is currently under review by the Ben's Services admin team.

No login credentials are available yet and your account is not active at this stage.

We will contact you again after review.
""";

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public Task SendProviderApplicationMissingInfoAsync(
        string recipientName,
        string recipientEmail,
        string missingInfoReason,
        CancellationToken cancellationToken = default)
    {
        var subject = "More Information Needed for Your Ben's Services Application";
        var body = $"""
Hello {recipientName},

Your provider application is being reviewed.
We need more information before continuing.

Missing information / reason:
{missingInfoReason}

Please contact Ben's Services or provide the requested documents/details so we can continue your review.
""";

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public Task SendProviderApplicationRejectedAsync(
        string recipientName,
        string recipientEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var subject = "Update About Your Ben's Services Provider Application";
        var body = $"""
Hello {recipientName},

Thank you for applying to join Ben's Services.
After review, your application was not approved at this time.

Reason:
{rejectionReason}

We appreciate your interest in Ben's Services.
""";

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    public Task SendProviderCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Ben's Services Provider Network";
        var body = $"""
Hello {recipientName},

Congratulations. Your Ben's Services provider application has been accepted and verified.
Your account is now ready to work with Ben's Services and wait for new job opportunities in your service area.

Login URL: {smtpSettings.FrontendLoginUrl}
Login email: {recipientEmail}
Temporary password: {temporaryPassword}

After your first login, you must change your temporary password.
""";

        return SendPlainTextEmailAsync(recipientEmail, subject, body, cancellationToken);
    }

    private async Task SendPlainTextEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        smtpSettings.ValidateForCredentialEmails();

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
            Credentials = new NetworkCredential(smtpSettings.User, smtpSettings.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
