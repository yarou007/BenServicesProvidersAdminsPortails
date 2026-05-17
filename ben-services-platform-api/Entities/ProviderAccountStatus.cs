namespace BenServicesPlatform.Api.Entities;

public static class ProviderAccountStatus
{
    public const string PendingApproval = "PendingApproval";
    public const string Active = "Active";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";

    private static readonly HashSet<string> ValidStatuses =
    [
        PendingApproval,
        Active,
        Rejected,
        Suspended
    ];

    public static bool IsValid(string status)
    {
        return ValidStatuses.Contains(status);
    }
}
