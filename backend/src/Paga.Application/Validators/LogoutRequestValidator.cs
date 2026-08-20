using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the logout request payload.
/// </summary>
public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("O campo refresh token é obrigatório.");
    }
}
