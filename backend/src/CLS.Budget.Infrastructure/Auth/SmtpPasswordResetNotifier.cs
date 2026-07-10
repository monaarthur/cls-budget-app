using System.Net;
using System.Net.Mail;
using CLS.Budget.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CLS.Budget.Infrastructure.Auth;

/// <summary>
/// Sends password reset links via SMTP (e.g. Gmail). Falls back to logging when SMTP is not configured.
/// </summary>
public sealed class SmtpPasswordResetNotifier(
    IOptions<PasswordResetOptions> passwordResetOptions,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<SmtpPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    private readonly PasswordResetOptions _passwordReset = passwordResetOptions.Value;
    private readonly SmtpOptions _smtp = smtpOptions.Value;

    public async Task SendResetLinkAsync(
        string email,
        string rawToken,
        CancellationToken cancellationToken = default,
        bool isAccountInvite = false)
    {
        var baseUrl = _passwordReset.FrontendBaseUrl.TrimEnd('/');
        var inviteQuery = isAccountInvite ? "&invite=1" : "";
        var link =
            $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}{inviteQuery}";

        if (!_smtp.IsConfigured)
        {
            logger.LogInformation(
                isAccountInvite
                    ? "Account invite for {Email}. SMTP not configured — setup link: {ResetLink}"
                    : "Password reset requested for {Email}. SMTP not configured — reset link: {ResetLink}",
                email,
                link);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_smtp.FromAddress, _smtp.FromDisplayName),
                Subject = isAccountInvite
                    ? "Create your CLS Budget account"
                    : "Reset your CLS Budget password",
                Body = isAccountInvite
                    ? $"""
                        You were invited to CLS Budget.

                        Open this link to create your password and access your household (expires in {_passwordReset.TokenMinutes} minutes):
                        {link}

                        If you were not expecting this invitation, you can ignore this email.
                        """
                    : $"""
                        You requested a password reset for CLS Budget.

                        Open this link to choose a new password (expires in {_passwordReset.TokenMinutes} minutes):
                        {link}

                        If you did not request this, you can ignore this email.
                        """,
                IsBodyHtml = false
            };
            message.To.Add(email);

            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.UseStartTls,
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password)
            };

            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send password reset email to {Email}. Reset link: {ResetLink}",
                email,
                link);
        }
    }
}
