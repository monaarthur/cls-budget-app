using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Accounts.Validators;
using FluentAssertions;

namespace CLS.Budget.UnitTests.Accounts;

public class CreateAccountRequestValidatorTests
{
    private readonly CreateAccountRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
    {
        var request = ValidRequest();
        request = new CreateAccountRequest
        {
            Name = "",
            Number = request.Number,
            Balance = request.Balance,
            Limit = request.Limit,
            AccountOpenDate = request.AccountOpenDate,
            MonthlyPayment = request.MonthlyPayment,
            Phone = request.Phone,
            Email = request.Email,
            Url = request.Url,
            AccountCategoryId = request.AccountCategoryId
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccountRequest.Name));
    }

    [Fact]
    public void Should_Pass_WhenRequestIsValid()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    private static CreateAccountRequest ValidRequest() => new()
    {
        Name = "Checking",
        Number = "CHK-001",
        Balance = 100m,
        Limit = 0m,
        AccountOpenDate = DateTime.UtcNow,
        MonthlyPayment = 50m,
        Phone = "555-0100",
        Email = "test@example.com",
        Url = "https://bank.example.com",
        AccountCategoryId = 1
    };
}
