namespace BenServicesPlatform.Api.Entities;

public static class ProviderAccountRole
{
    public const string Admin = "ADMIN";
    public const string Provider = "PROVIDER";
    public const string Customer = "CUSTOMER";

    private static readonly HashSet<string> ValidRoles =
    [
        Admin,
        Provider,
        Customer
    ];

    public static bool IsValid(string role)
    {
        return ValidRoles.Contains(role);
    }
}
