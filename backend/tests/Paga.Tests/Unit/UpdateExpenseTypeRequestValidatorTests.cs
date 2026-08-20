using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit;

public class UpdateExpenseTypeRequestValidatorTests
{
    private readonly UpdateExpenseTypeRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var model = new UpdateExpenseTypeRequest { Name = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameIsValid()
    {
        var model = new UpdateExpenseTypeRequest { Name = "Transporte" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceeds100Characters()
    {
        var model = new UpdateExpenseTypeRequest { Name = new string('A', 101) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
