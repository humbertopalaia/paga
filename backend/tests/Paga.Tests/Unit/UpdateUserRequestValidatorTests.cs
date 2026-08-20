using FluentValidation.TestHelper;
using Paga.Application.DTOs;
using Paga.Application.Validators;

namespace Paga.Tests.Unit;

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenNameEmpty()
    {
        var model = new UpdateUserRequest { Name = "", Email = "a@b.com", Password = null };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailInvalidFormat()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "notanemail", Password = null };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordPresentAndLessThan6Chars()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "a@b.com", Password = "12345" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordNull()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "a@b.com", Password = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordEmptyString()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "a@b.com", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllValidWithoutPassword()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "a@b.com", Password = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllValidWithPassword()
    {
        var model = new UpdateUserRequest { Name = "Test", Email = "a@b.com", Password = "123456" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
