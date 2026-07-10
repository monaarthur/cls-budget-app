using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Infrastructure.Auth;
using CLS.Budget.Infrastructure.Persistance;
using CLS.Budget.Infrastructure.Repositories;
using CLS.Budget.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CLS.Budget.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BudgetDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'BudgetDatabase' is not configured.");

        services.AddDbContext<BudgetDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly("CLS.Budget.Migration")));

        // Fallback tenant context for non-HTTP hosts (import CLI, design-time).
        // The API replaces this with an HTTP/claims-based implementation.
        services.TryAddScoped<ITenantContext, DefaultTenantContext>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IBudgetIncomeRepository, BudgetIncomeRepository>();
        services.AddScoped<IBudgetPaymentStatusRepository, BudgetPaymentStatusRepository>();
        services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
        services.AddScoped<IPayFrequencyTypeRepository, PayFrequencyTypeRepository>();
        services.AddScoped<IIncomeSourceRepository, IncomeSourceRepository>();
        services.AddScoped<IPayScheduleRepository, PayScheduleRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetTemplateRepository, BudgetTemplateRepository>();
        services.AddScoped<IAccountCategoryRepository, AccountCategoryRepository>();
        services.AddScoped<ITransactionImportRepository, TransactionImportRepository>();

        // Authentication / identity
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordResetSettings, PasswordResetSettings>();
        services.AddSingleton<IPasswordResetNotifier, SmtpPasswordResetNotifier>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        return services;
    }
}
