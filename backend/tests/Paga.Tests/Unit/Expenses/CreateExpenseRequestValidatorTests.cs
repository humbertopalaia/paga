using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit.Expenses;

public class CreateExpenseRequestValidatorTests
{
    private readonly CreateExpenseRequestValidator _validator = new();

    private static CreateExpenseRequest ValidNonRecurring() => new()
    {
        DueDate = new DateOnly(2024, 6, 15),
        Description = "Internet bill",
        ExpenseTypeId = 1,
        Value = 120.50m,
        IsRecurring = false,
        Frequency = null
    };

    private static CreateExpenseRequest ValidRecurring() => new()
    {
        DueDate = new DateOnly(2024, 6, 15),
        Description = "Internet bill",
        ExpenseTypeId = 1,
        Value = 120.50m,
        IsRecurring = true,
        Frequency = "monthly"
    };

    [Fact]
    public void Validate_ShouldFail_WhenDueDateMissing()
    {
        var model = ValidNonRecurring() with { DueDate = default };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("A data de vencimento é obrigatória.");
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
    public void Validate_ShouldFail_WhenExpenseTypeIdZero()
    {
        var model = ValidNonRecurring() with { ExpenseTypeId = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExpenseTypeId)
            .WithErrorMessage("O tipo de despesa é obrigatório.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueZero()
    {
        var model = ValidNonRecurring() with { Value = 0m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("O valor deve ser maior que zero.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueNegative()
    {
        var model = ValidNonRecurring() with { Value = -50m };
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
            .WithErrorMessage("A frequência é obrigatória para despesas recorrentes.");
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
    public void Validate_ShouldFail_WhenNotRecurringWithFrequency()
    {
        var model = ValidNonRecurring() with { Frequency = "monthly" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Frequency)
            .WithErrorMessage("A frequência deve ser nula para despesas não recorrentes.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsValid_NonRecurring()
    {
        var model = ValidNonRecurring();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsValid_Recurring()
    {
        var model = ValidRecurring();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
