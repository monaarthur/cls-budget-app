using CLS.Budget.Application.Payments.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Payments.Validators;

public sealed class UpdatePaymentRequestValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentRequestValidator()
    {
        RuleFor(x => x.BudgetId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.BudgetPaymentStatusId).GreaterThan(0);
        RuleFor(x => x.PaymentMade).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentDate).NotEmpty();
    }
}
