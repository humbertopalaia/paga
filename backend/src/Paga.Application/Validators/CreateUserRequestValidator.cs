using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the create user request payload.
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O campo nome é obrigatório.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O campo email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("O campo senha é obrigatório.")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.");
    }
}
