namespace BenServicesPlatform.Api.Entities;

public static class AdminRole
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string Admin = "ADMIN";
    public const string Staff = "STAFF";
    public const string Provider = "PROVIDER";

    private static readonly HashSet<string> ValidRoles =
    [
        SuperAdmin,
        Admin,
        Staff,
        Provider
    ];

    public static bool IsValid(string role)
    {
        return ValidRoles.Contains(role);
    }
}
