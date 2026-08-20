using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the payload for updating an existing expense type.
/// </summary>
public class UpdateExpenseTypeRequestValidator : AbstractValidator<UpdateExpenseTypeRequest>
{
    public UpdateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");
    }
}
