using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the payload for updating an existing expense.
/// </summary>
public class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    private static readonly string[] ValidFrequencies = ["weekly", "monthly", "yearly"];

    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("A data de vencimento é obrigatória.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(300).WithMessage("A descrição deve ter no máximo 300 caracteres.");

        RuleFor(x => x.ExpenseTypeId)
            .GreaterThan(0).WithMessage("O tipo de despesa é obrigatório.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("A frequência é obrigatória para despesas recorrentes.")
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Frequência inválida. Valores aceitos: weekly, monthly, yearly.")
            .When(x => x.IsRecurring);

        RuleFor(x => x.Frequency)
            .Null().WithMessage("A frequência deve ser nula para despesas não recorrentes.")
            .When(x => !x.IsRecurring);
    }
}
