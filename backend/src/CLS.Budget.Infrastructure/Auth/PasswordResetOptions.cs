namespace CLS.Budget.Infrastructure.Auth;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>Frontend base URL for reset links (no trailing slash).</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    public int TokenMinutes { get; set; } = 60;
}
