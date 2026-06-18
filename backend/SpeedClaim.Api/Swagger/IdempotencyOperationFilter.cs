using Microsoft.OpenApi.Models;
using SpeedClaim.Api.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpeedClaim.Api.Swagger;

public class IdempotencyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasIdempotent = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<IdempotentAttribute>()
            .Any();

        if (!hasIdempotent) return;

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional UUID. When provided, retries with the same key return the cached response instead of re-processing.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });

        operation.Description = (operation.Description ?? "") +
            "\n\n🔁 **Idempotent:** Supports optional `Idempotency-Key` header (UUID) to prevent duplicate processing.";
    }
}
