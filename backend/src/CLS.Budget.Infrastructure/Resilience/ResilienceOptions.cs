namespace CLS.Budget.Infrastructure.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public SmtpResilienceOptions Smtp { get; init; } = new();
    public DatabaseResilienceOptions Database { get; init; } = new();
    public HttpResilienceOptions Http { get; init; } = new();
}

public sealed class SmtpResilienceOptions
{
    /// <summary>How many times to retry after the first failure.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Initial delay before the first retry (exponential backoff grows from this).</summary>
    public int DelayMilliseconds { get; init; } = 500;

    /// <summary>Per-attempt timeout for one SMTP send (retries each get their own budget).</summary>
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class DatabaseResilienceOptions
{
    /// <summary>EF Core / Npgsql transient fault retries (connection blips, deadlocks).</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Maximum delay between database retries.</summary>
    public int MaxRetryDelaySeconds { get; init; } = 30;
}

public sealed class HttpResilienceOptions
{
    /// <summary>Named HttpClient used for future outbound API calls.</summary>
    public string ClientName { get; init; } = "ExternalApi";

    /// <summary>Total request timeout for the standard HTTP resilience handler.</summary>
    public int AttemptTimeoutSeconds { get; init; } = 10;

    /// <summary>Total timeout across retries for one logical HTTP call.</summary>
    public int TotalRequestTimeoutSeconds { get; init; } = 30;

    public int MaxRetryAttempts { get; init; } = 3;
}
