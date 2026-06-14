using CLS.Budget.Infrastructure.Persistance;
using CLS.Budget.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CLS.Budget.EfCore;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    public BudgetDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("BudgetDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'BudgetDatabase' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<BudgetDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly("CLS.Budget.Migration"));

        return new BudgetDbContext(optionsBuilder.Options, new DefaultTenantContext());
    }
}
