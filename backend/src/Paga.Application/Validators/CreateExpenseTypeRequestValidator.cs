using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the payload for creating a new expense type.
/// </summary>
public class CreateExpenseTypeRequestValidator : AbstractValidator<CreateExpenseTypeRequest>
{
    public CreateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");
    }
}
