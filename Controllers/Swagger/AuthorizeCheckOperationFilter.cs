using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi; // OpenApi v2 types
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestroPlate.Controllers.Swagger;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>()
            .ToList() ?? new List<AuthorizeAttribute>();

        if (authAttributes.Any())
        {
            var allowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous) 
                return;

            var roles = authAttributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .Select(a => a.Roles)
                .Distinct()
                .ToList();

            if (roles.Any())
            {
                var roleText = string.Join(", ", roles);
                operation.Description += $"\n\n<br><b>Required Roles:</b> {roleText}";
            }
            else
            {
                operation.Description += "\n\n<br><b>Requires Authentication</b>";
            }
        }
    }
}
