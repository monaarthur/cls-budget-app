namespace CLS.Budget.Application.Abstractions;

public interface IPasswordResetSettings
{
    TimeSpan TokenLifetime { get; }
    string FrontendBaseUrl { get; }
}
