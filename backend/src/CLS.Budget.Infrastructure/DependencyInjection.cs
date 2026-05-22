using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Infrastructure.Persistance;
using CLS.Budget.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IBudgetPaymentStatusRepository, BudgetPaymentStatusRepository>();
        services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetTemplateRepository, BudgetTemplateRepository>();

        return services;
    }
}
