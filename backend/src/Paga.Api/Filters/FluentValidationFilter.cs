using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Paga.Api.Filters;

/// <summary>
/// Action filter that validates [FromBody] parameters using registered FluentValidation validators.
/// Only validates parameters that are complex objects (not primitives or strings).
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value is null)
                continue;

            var type = value.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(CancellationToken))
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator is null)
                continue;

            var validationContext = new ValidationContext<object>(value);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
