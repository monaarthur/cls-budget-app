namespace CLS.Budget.Application.Abstractions;

public interface IPasswordResetNotifier
{
    Task SendResetLinkAsync(
        string email,
        string rawToken,
        CancellationToken cancellationToken = default,
        bool isAccountInvite = false);
}
