using CLS.Budget.Application.Auth.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Auth.Validators;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
