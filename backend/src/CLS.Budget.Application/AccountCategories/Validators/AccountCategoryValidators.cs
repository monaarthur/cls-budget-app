using CLS.Budget.Application.AccountCategories.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.AccountCategories.Validators;

public sealed class CreateAccountCategoryRequestValidator
    : AbstractValidator<CreateAccountCategoryRequest>
{
    public CreateAccountCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateAccountSubCategoryRequestValidator
    : AbstractValidator<CreateAccountSubCategoryRequest>
{
    public CreateAccountSubCategoryRequestValidator()
    {
        RuleFor(x => x.AccountCategoryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
