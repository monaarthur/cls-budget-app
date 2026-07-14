using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace CLS.Budget.Infrastructure.Resilience;

public static class ResiliencePipelineNames
{
    public const string Smtp = "smtp";
}

public static class ResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));

        var resilience = configuration
            .GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>()
            ?? new ResilienceOptions();

        var smtp = resilience.Smtp;
        services.AddResiliencePipeline(ResiliencePipelineNames.Smtp, (builder, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("CLS.Budget.Resilience.Smtp");

            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = Math.Max(0, smtp.MaxRetryAttempts),
                    Delay = TimeSpan.FromMilliseconds(Math.Max(0, smtp.DelayMilliseconds)),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<SmtpException>()
                        .Handle<SocketException>()
                        .Handle<IOException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            args.Outcome.Exception,
                            "SMTP send failed; retry {Attempt} of {MaxAttempts} after {Delay}.",
                            args.AttemptNumber,
                            smtp.MaxRetryAttempts,
                            args.RetryDelay);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(Math.Max(1, smtp.TimeoutSeconds))
                });
        });

        var http = resilience.Http;
        services
            .AddHttpClient(http.ClientName)
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(Math.Max(1, http.AttemptTimeoutSeconds));
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(Math.Max(1, http.TotalRequestTimeoutSeconds));
                options.Retry.MaxRetryAttempts = Math.Max(0, http.MaxRetryAttempts);
            });

        return services;
    }
}
