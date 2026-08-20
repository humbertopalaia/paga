using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the login request payload.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O campo email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("O campo senha é obrigatório.");
    }
}
