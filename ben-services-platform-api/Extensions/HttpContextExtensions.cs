using System.Security.Claims;
using BenServicesPlatform.Api.Entities;

namespace BenServicesPlatform.Api.Extensions;

public static class HttpContextExtensions
{
    public static int? GetAuthenticatedAdminId(this HttpContext httpContext)
    {
        if (!httpContext.IsAuthenticatedRole(AdminRole.SuperAdmin, AdminRole.Admin, AdminRole.Staff))
        {
            return null;
        }

        return httpContext.GetAuthenticatedUserId();
    }

    public static int? GetAuthenticatedUserId(this HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        return int.TryParse(value, out var userId) ? userId : null;
    }

    public static string? GetAuthenticatedRole(this HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(ClaimTypes.Role)
            ?? httpContext.User.FindFirstValue("role");
    }

    public static bool IsAuthenticatedRole(this HttpContext httpContext, params string[] roles)
    {
        var role = httpContext.GetAuthenticatedRole();
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return roles.Any(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
