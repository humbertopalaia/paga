using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit.Incomes;

public class UpdateIncomeRequestValidatorTests
{
    private readonly UpdateIncomeRequestValidator _validator = new();

    private static UpdateIncomeRequest ValidNonRecurring() => new()
    {
        Date = new DateOnly(2024, 6, 15),
        Description = "Salário",
        Value = 5000m,
        IsRecurring = false,
        Frequency = null
    };

    private static UpdateIncomeRequest ValidRecurring() => new()
    {
        Date = new DateOnly(2024, 6, 15),
        Description = "Salário",
        Value = 5000m,
        IsRecurring = true,
        Frequency = "monthly"
    };

    [Fact]
    public void Validate_ShouldFail_WhenDateMissing()
    {
        var model = ValidNonRecurring() with { Date = default };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("A data é obrigatória.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionEmpty()
    {
        var model = ValidNonRecurring() with { Description = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("A descrição é obrigatória.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceeds300Characters()
    {
        var model = ValidNonRecurring() with { Description = new string('A', 301) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("A descrição deve ter no máximo 300 caracteres.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueIsZero()
    {
        var model = ValidNonRecurring() with { Value = 0m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("O valor deve ser maior que zero.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueIsNegative()
    {
        var model = ValidNonRecurring() with { Value = -100m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("O valor deve ser maior que zero.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenRecurringWithoutFrequency()
    {
        var model = ValidRecurring() with { Frequency = null };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Frequency)
            .WithErrorMessage("A frequência é obrigatória para receitas recorrentes.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenRecurringWithInvalidFrequency()
    {
        var model = ValidRecurring() with { Frequency = "daily" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Frequency)
            .WithErrorMessage("Frequência inválida. Valores aceitos: weekly, monthly, yearly.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNotRecurringWithFrequencySet()
    {
        var model = ValidNonRecurring() with { Frequency = "monthly" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Frequency)
            .WithErrorMessage("A frequência deve ser nula para receitas não recorrentes.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsValidNonRecurring()
    {
        var model = ValidNonRecurring();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsValidRecurring()
    {
        var model = ValidRecurring();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
