using CLS.Budget.Domain;
using FluentValidation;
using CLS.Budget.Application.PaySchedules.Dtos;

namespace CLS.Budget.Application.PaySchedules.Validators;

public sealed class CreatePayScheduleRequestValidator : AbstractValidator<CreatePayScheduleRequest>
{
    public CreatePayScheduleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IncomeSourceId).GreaterThan(0);
        RuleFor(x => x.PayFrequencyTypeId).GreaterThan(0);
        RuleFor(x => x).Must(HasRequiredFieldsForFrequency)
            .WithMessage("Pay schedule fields do not match the selected frequency.");
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6)
            .When(x => x.DayOfWeek.HasValue);
        RuleFor(x => x.SemiMonthlyDay1)
            .InclusiveBetween(1, 31)
            .When(x => x.SemiMonthlyDay1.HasValue);
        RuleFor(x => x.SemiMonthlyDay2)
            .InclusiveBetween(1, 31)
            .When(x => x.SemiMonthlyDay2.HasValue);
    }

    internal static bool HasRequiredFieldsForFrequency(CreatePayScheduleRequest request) =>
        HasRequiredFieldsForFrequency(
            request.PayFrequencyTypeId,
            request.AnchorDate,
            request.SemiMonthlyDay1,
            request.SemiMonthlyDay2);

    internal static bool HasRequiredFieldsForFrequency(
        int payFrequencyTypeId,
        DateTime? anchorDate,
        int? semiMonthlyDay1,
        int? semiMonthlyDay2) =>
        payFrequencyTypeId switch
        {
            PayFrequencyTypeIds.Weekly or PayFrequencyTypeIds.BiWeekly => anchorDate.HasValue,
            PayFrequencyTypeIds.SemiMonthly =>
                semiMonthlyDay1 is >= 1 and <= 31 &&
                semiMonthlyDay2 is >= 1 and <= 31,
            _ => false
        };
}

public sealed class UpdatePayScheduleRequestValidator : AbstractValidator<UpdatePayScheduleRequest>
{
    public UpdatePayScheduleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IncomeSourceId).GreaterThan(0);
        RuleFor(x => x.PayFrequencyTypeId).GreaterThan(0);
        RuleFor(x => x).Must(HasRequiredFieldsForFrequency)
            .WithMessage("Pay schedule fields do not match the selected frequency.");
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6)
            .When(x => x.DayOfWeek.HasValue);
        RuleFor(x => x.SemiMonthlyDay1)
            .InclusiveBetween(1, 31)
            .When(x => x.SemiMonthlyDay1.HasValue);
        RuleFor(x => x.SemiMonthlyDay2)
            .InclusiveBetween(1, 31)
            .When(x => x.SemiMonthlyDay2.HasValue);
    }

    private static bool HasRequiredFieldsForFrequency(UpdatePayScheduleRequest request) =>
        CreatePayScheduleRequestValidator.HasRequiredFieldsForFrequency(
            request.PayFrequencyTypeId,
            request.AnchorDate,
            request.SemiMonthlyDay1,
            request.SemiMonthlyDay2);
}
