namespace BenServicesPlatform.Api.Services;

public interface IEmailService
{
    Task SendAdminCredentialsAsync(
        string recipientName,
        string recipientEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
