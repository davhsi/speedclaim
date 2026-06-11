using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpeedClaim.Api.Swagger;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerAuth = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .ToList() ?? [];

        var actionAuth = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .ToList();

        var allAuth = controllerAuth.Concat(actionAuth).ToList();

        var isAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (isAnonymous || allAuth.Count == 0)
        {
            operation.Description = (operation.Description ?? "") + "\n\n🔓 **Auth:** Public";
            return;
        }

        var roles = allAuth
            .Where(a => !string.IsNullOrEmpty(a.Roles))
            .SelectMany(a => a.Roles!.Split(','))
            .Select(r => r.Trim())
            .Distinct()
            .ToList();

        var roleLabel = roles.Count > 0
            ? string.Join(", ", roles)
            : "Any authenticated user";

        operation.Description = (operation.Description ?? "") + $"\n\n🔐 **Auth:** Bearer JWT required  \n👤 **Roles:** `{roleLabel}`";

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized – JWT token missing or invalid" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden – insufficient role" });
    }
}
