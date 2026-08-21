using FluentValidation;
using Paga.Application.DTOs;

namespace Paga.Application.Validators;

/// <summary>
/// Validates the payload for creating a new income.
/// </summary>
public class CreateIncomeRequestValidator : AbstractValidator<CreateIncomeRequest>
{
    private static readonly string[] ValidFrequencies = ["weekly", "monthly", "yearly"];

    public CreateIncomeRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("A data é obrigatória.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(300).WithMessage("A descrição deve ter no máximo 300 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("A frequência é obrigatória para receitas recorrentes.")
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Frequência inválida. Valores aceitos: weekly, monthly, yearly.")
            .When(x => x.IsRecurring);

        RuleFor(x => x.Frequency)
            .Null().WithMessage("A frequência deve ser nula para receitas não recorrentes.")
            .When(x => !x.IsRecurring);
    }
}
