using CLS.Budget.Application.CreditCardEngine.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class ActivatePayoffPlanRequestValidator : AbstractValidator<ActivatePayoffPlanRequest>
{
    public ActivatePayoffPlanRequestValidator()
    {
        RuleFor(x => x.SavedPayoffPlanId)
            .GreaterThan(0)
            .When(x => x.SavedPayoffPlanId.HasValue);

        RuleFor(x => x)
            .Must(x => x.SavedPayoffPlanId is > 0
                || (!string.IsNullOrWhiteSpace(x.Strategy) && x.TotalMonthlyDebtPayment is > 0))
            .WithMessage("Provide a savedPayoffPlanId or inline strategy and totalMonthlyDebtPayment.");

        RuleFor(x => x.Strategy)
            .Must(s => s is null || CreateForecastRequestValidator.ParseStrategy(s) is not null)
            .When(x => !string.IsNullOrWhiteSpace(x.Strategy))
            .WithMessage("Strategy must be Avalanche, Snowball, or MinimumsOnly.");

        RuleFor(x => x.TotalMonthlyDebtPayment)
            .GreaterThan(0)
            .When(x => x.SavedPayoffPlanId is null);

        RuleFor(x => x.ExtraMonthlyPayment)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ExtraMonthlyPayment.HasValue);

        RuleFor(x => x.TargetUtilizationPercent)
            .InclusiveBetween(1m, 99m)
            .When(x => x.TargetUtilizationPercent.HasValue);

        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ReviseActivePayoffPlanRequestValidator : AbstractValidator<ReviseActivePayoffPlanRequest>
{
    public ReviseActivePayoffPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Strategy)
            .Must(s => CreateForecastRequestValidator.ParseStrategy(s) is not null)
            .WithMessage("Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        RuleFor(x => x.TotalMonthlyDebtPayment).GreaterThan(0);
        RuleFor(x => x.ExtraMonthlyPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetUtilizationPercent)
            .InclusiveBetween(1m, 99m)
            .When(x => x.TargetUtilizationPercent.HasValue);
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.PostUtilizationStrategy)
            .Must(s => s is null
                || s.Equals("Avalanche", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Snowball", StringComparison.OrdinalIgnoreCase))
            .WithMessage("PostUtilizationStrategy must be Avalanche, Snowball, or omitted.");
    }
}

public sealed class RecordPayoffPlanPaymentRequestValidator : AbstractValidator<RecordPayoffPlanPaymentRequest>
{
    public RecordPayoffPlanPaymentRequestValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
