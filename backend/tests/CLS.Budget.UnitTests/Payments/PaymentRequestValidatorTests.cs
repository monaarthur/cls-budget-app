using CLS.Budget.Application.Payments.Dtos;
using CLS.Budget.Application.Payments.Validators;
using FluentAssertions;

namespace CLS.Budget.UnitTests.Payments;

public sealed class PaymentRequestValidatorTests
{
    private readonly CreatePaymentRequestValidator _create = new();
    private readonly UpdatePaymentRequestValidator _update = new();

    [Fact]
    public void Create_AllowsNotesUpTo1000Characters()
    {
        var request = new CreatePaymentRequest
        {
            BudgetId = 1,
            AccountId = 1,
            PaymentMade = 0,
            Amount = 100,
            BudgetPaymentStatusId = 2,
            PaymentDate = DateTime.UtcNow.Date,
            Notes = new string('n', 1000)
        };

        _create.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_RejectsNotesOver1000Characters()
    {
        var request = new CreatePaymentRequest
        {
            BudgetId = 1,
            AccountId = 1,
            PaymentMade = 0,
            Amount = 100,
            BudgetPaymentStatusId = 2,
            PaymentDate = DateTime.UtcNow.Date,
            Notes = new string('n', 1001)
        };

        var result = _create.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentRequest.Notes));
    }

    [Fact]
    public void Update_RejectsNotesOver1000Characters()
    {
        var request = new UpdatePaymentRequest
        {
            BudgetId = 1,
            AccountId = 1,
            PaymentMade = 0,
            Amount = 100,
            BudgetPaymentStatusId = 2,
            PaymentDate = DateTime.UtcNow.Date,
            Notes = new string('n', 1001)
        };

        var result = _update.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePaymentRequest.Notes));
    }
}
