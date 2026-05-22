using CLS.Budget.Application;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CLS.Budget.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddApplication();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddApplication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
