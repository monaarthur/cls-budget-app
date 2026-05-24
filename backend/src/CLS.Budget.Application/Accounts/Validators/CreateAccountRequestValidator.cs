using CLS.Budget.Application.Accounts.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Accounts.Validators;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Username).MaximumLength(200);
        RuleFor(x => x.Password).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.AccountCategoryId).GreaterThan(0);
        RuleFor(x => x.MonthlyPayment)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MonthlyPayment.HasValue);
        RuleFor(x => x.PaymentDay)
            .InclusiveBetween(1, 31)
            .When(x => x.PaymentDay.HasValue);
    }
}
