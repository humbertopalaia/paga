using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the refresh token request payload.
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("O campo refresh token é obrigatório.");
    }
}
