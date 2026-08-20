using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the update user request payload.
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O campo nome é obrigatório.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O campo email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.");

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
