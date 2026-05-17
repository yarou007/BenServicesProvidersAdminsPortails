namespace BenServicesPlatform.Api.Entities;

public static class ClientServiceRequestStatus
{
    public const string New = "New";
    public const string Reviewed = "Reviewed";
    public const string Assigned = "Assigned";
    public const string InProgress = "In Progress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    private static readonly HashSet<string> ValidStatuses =
    [
        New,
        Reviewed,
        Assigned,
        InProgress,
        Completed,
        Cancelled
    ];

    public static bool IsValid(string status)
    {
        return ValidStatuses.Contains(status);
    }
}
