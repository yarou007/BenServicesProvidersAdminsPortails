using System.Security.Claims;

namespace BenServicesPlatform.Api.Extensions;

public static class HttpContextExtensions
{
    public static int? GetAuthenticatedAdminId(this HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        return int.TryParse(value, out var adminId) ? adminId : null;
    }
}
