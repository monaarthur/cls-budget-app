using CLS.Budget.Application.Admin.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Admin.Validators;

public sealed class InviteTenantUserRequestValidator : AbstractValidator<InviteTenantUserRequest>
{
    public InviteTenantUserRequestValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role is "Owner" or "Member" or "0" or "1")
            .WithMessage("Role must be Owner or Member.");
    }
}
