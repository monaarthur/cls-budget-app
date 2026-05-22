using CLS.Budget.Infrastructure.Persistance;
using CLS.Budget.Migration.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CLS.Budget.Migration.Design;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    public BudgetDbContext CreateDbContext(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();
        var connectionString = config.GetConnectionString("DefaultConnection") ??
                               "Server=(localdb)\\mssqllocaldb;Database=ClsBudge;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<BudgetDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BudgetDbContext(optionsBuilder.Options);
    }
}