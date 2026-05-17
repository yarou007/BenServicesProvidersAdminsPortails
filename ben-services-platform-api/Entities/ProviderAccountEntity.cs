namespace BenServicesPlatform.Api.Entities;

public class ProviderAccountEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = ProviderAccountRole.Provider;
    public string Status { get; set; } = ProviderAccountStatus.PendingApproval;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ProviderApplicationEntity> Applications { get; set; } = [];
}
