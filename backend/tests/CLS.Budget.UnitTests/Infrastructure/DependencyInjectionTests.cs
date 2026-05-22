using CLS.Budget.Infrastructure;
using CLS.Budget.Infrastructure.Persistance;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CLS.Budget.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersBudgetDbContext()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        services.AddInfrastructure(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(BudgetDbContext));
    }

    [Fact]
    public void AddInfrastructure_ResolvesBudgetDbContext()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();

        context.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_ThrowsWhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BudgetDatabase*");
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BudgetDatabase"] =
                    "Host=localhost;Port=5432;Database=cls_budget_test;Username=postgres;Password=test"
            })
            .Build();
}
