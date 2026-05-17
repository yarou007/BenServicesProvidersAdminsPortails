namespace BenServicesPlatform.Api.Entities;

public static class ProviderApplicationStatus
{
    public const string Pending = "Pending";
    public const string UnderReview = "UnderReview";
    public const string MissingInfo = "MissingInfo";
    public const string Rejected = "Rejected";
    public const string Accepted = "Accepted";
    public const string Verified = "Verified";
    public const string Converted = "Converted";

    private static readonly HashSet<string> ValidStatuses =
    [
        Pending,
        UnderReview,
        MissingInfo,
        Rejected,
        Accepted,
        Verified,
        Converted
    ];

    public static bool IsValid(string status)
    {
        return ValidStatuses.Contains(status);
    }
}
