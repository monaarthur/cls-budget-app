using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CLS.Budget.Api.Auth;

/// <summary>
/// Development-only authentication scheme used when <c>Auth:Enabled</c> is <c>false</c>.
/// Every request is authenticated as the configured dev user/tenant so the app can run
/// without login while still flowing a tenant through <see cref="HttpTenantContext"/>.
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";

    private readonly AuthOptions _auth = authOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", _auth.DevUserId.ToString()),
            new Claim("tenant_id", _auth.DevTenantId.ToString()),
            new Claim("email", _auth.DevEmail),
            new Claim("name", _auth.DevDisplayName),
            new Claim(ClaimTypes.Role, _auth.DevRole)
        };

        var identity = new ClaimsIdentity(claims, SchemeName, "name", ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
