using CLS.Budget.Application.Abstractions;
using CLS.Budget.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CLS.Budget.Infrastructure.Auth;

public sealed class PasswordResetSettings(IOptions<PasswordResetOptions> options) : IPasswordResetSettings
{
    private readonly PasswordResetOptions _options = options.Value;

    public TimeSpan TokenLifetime => TimeSpan.FromMinutes(_options.TokenMinutes);
    public string FrontendBaseUrl => _options.FrontendBaseUrl;
}
