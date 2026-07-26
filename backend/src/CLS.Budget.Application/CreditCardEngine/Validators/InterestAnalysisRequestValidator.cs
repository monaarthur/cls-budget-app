using CLS.Budget.Application.CreditCardEngine.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class InterestAnalysisRequestValidator : AbstractValidator<InterestAnalysisRequest>
{
    public InterestAnalysisRequestValidator()
    {
        RuleFor(x => x.MonthlyPayment)
            .GreaterThan(0)
            .When(x => x.MonthlyPayment.HasValue);
    }
}
