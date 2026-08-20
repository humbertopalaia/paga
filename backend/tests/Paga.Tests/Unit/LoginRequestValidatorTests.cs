using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenEmailEmpty()
    {
        var model = new LoginRequest { Email = "", Password = "test" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordEmpty()
    {
        var model = new LoginRequest { Email = "a@b.com", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllValid()
    {
        var model = new LoginRequest { Email = "a@b.com", Password = "test" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
