using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.AutoPayoff;
using CLS.Budget.Application.AutoPayoff.Dtos;
using CLS.Budget.Application.AutoPayoff.Validators;
using CLS.Budget.Domain.AutoPayoff;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.AutoPayoff;

public sealed class AutoPayoffServiceTests
{
    [Fact]
    public void Validator_Requires_Unique_Payoff_Name()
    {
        var validator = new SaveAutoPayoffConfigRequestValidator();
        validator.Validate(new SaveAutoPayoffConfigRequest { Name = "" }).IsValid.Should().BeFalse();
        validator.Validate(new SaveAutoPayoffConfigRequest { Name = "   " }).IsValid.Should().BeFalse();
        validator.Validate(new SaveAutoPayoffConfigRequest { Name = "Credit cards first" }).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Save_Rejects_Duplicate_Name()
    {
        var repo = new Mock<IAutoPayoffConfigRepository>();
        repo.Setup(r => r.NameExistsAsync("Credit cards first", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService(repo.Object);
        var result = await sut.SaveConfigAsync(new SaveAutoPayoffConfigRequest
        {
            Name = "Credit cards first",
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("already exists"));
        repo.Verify(r => r.AddAsync(It.IsAny<AutoPayoffConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_Creates_When_Name_Is_Unique()
    {
        var repo = new Mock<IAutoPayoffConfigRepository>();
        repo.Setup(r => r.NameExistsAsync("Snowball extra", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<AutoPayoffConfig>(), It.IsAny<CancellationToken>()))
            .Callback<AutoPayoffConfig, CancellationToken>((config, _) => config.AutoPayoffConfigId = 9)
            .ReturnsAsync((AutoPayoffConfig config, CancellationToken _) => config);

        var sut = CreateService(repo.Object);
        var result = await sut.SaveConfigAsync(new SaveAutoPayoffConfigRequest
        {
            Name = "Snowball extra",
            ExtraMonthlyAmount = 150,
        });

        result.Success.Should().BeTrue();
        result.Data!.AutoPayoffConfigId.Should().Be(9);
        result.Data.Name.Should().Be("Snowball extra");
    }

    [Fact]
    public async Task Save_Allows_Keeping_The_Same_Name_On_Update()
    {
        var existing = new AutoPayoffConfig
        {
            AutoPayoffConfigId = 4,
            Name = "Avalanche",
            ExcludeStatusIdsJson = "[3]",
            ExcludeCategoryIdsJson = "[]",
            ExcludeAccountIdsJson = "[]",
            TargetCategoryIdsJson = "[]",
        };
        var repo = new Mock<IAutoPayoffConfigRepository>();
        repo.Setup(r => r.NameExistsAsync("Avalanche", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateService(repo.Object);
        var result = await sut.SaveConfigAsync(new SaveAutoPayoffConfigRequest
        {
            AutoPayoffConfigId = 4,
            Name = "Avalanche",
            ExtraMonthlyAmount = 50,
        });

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Avalanche");
        repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AutoPayoffService CreateService(IAutoPayoffConfigRepository repo) =>
        new(
            repo,
            new Mock<IAccountRepository>().Object,
            new Mock<IPaymentRepository>().Object,
            new AutoPayoffEngine());
}
