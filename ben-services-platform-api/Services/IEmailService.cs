namespace BenServicesPlatform.Api.Services;

public interface IEmailService
{
    Task SendAdminCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default);

    Task SendProviderApplicationReceivedAsync(
        string recipientName,
        string recipientEmail,
        CancellationToken cancellationToken = default);

    Task SendProviderApplicationUnderReviewAsync(
        string recipientName,
        string recipientEmail,
        CancellationToken cancellationToken = default);

    Task SendProviderApplicationMissingInfoAsync(
        string recipientName,
        string recipientEmail,
        string missingInfoReason,
        CancellationToken cancellationToken = default);

    Task SendProviderApplicationRejectedAsync(
        string recipientName,
        string recipientEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    Task SendProviderCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
