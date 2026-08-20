using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenNameEmpty()
    {
        var model = new CreateUserRequest { Name = "", Email = "a@b.com", Password = "123456" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailEmpty()
    {
        var model = new CreateUserRequest { Name = "Test", Email = "", Password = "123456" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailInvalidFormat()
    {
        var model = new CreateUserRequest { Name = "Test", Email = "notanemail", Password = "123456" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordEmpty()
    {
        var model = new CreateUserRequest { Name = "Test", Email = "a@b.com", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordLessThan6Chars()
    {
        var model = new CreateUserRequest { Name = "Test", Email = "a@b.com", Password = "12345" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllValid()
    {
        var model = new CreateUserRequest { Name = "Test", Email = "a@b.com", Password = "123456" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
