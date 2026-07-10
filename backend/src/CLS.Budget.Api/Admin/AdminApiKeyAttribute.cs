using CLS.Budget.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace CLS.Budget.Api.Admin;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (HttpMethods.IsOptions(context.HttpContext.Request.Method))
        {
            return;
        }

        var options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<AdminOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail("Admin API is not configured."))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        if (!TryGetProvidedKey(context.HttpContext, out var provided)
            || !string.Equals(provided, options.ApiKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Invalid admin API key."));
        }
    }

    private static bool TryGetProvidedKey(HttpContext httpContext, out string provided)
    {
        provided = "";

        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            provided = headerValue.ToString()!;
            return true;
        }

        var auth = httpContext.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            provided = auth["Bearer ".Length..].Trim();
            return provided.Length > 0;
        }

        return false;
    }
}
