using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Paga.Application.Exceptions;
using AuthenticationException = Paga.Application.Exceptions.AuthenticationException;

namespace Paga.Api.ExceptionHandling;

/// <summary>
/// Global exception handler that converts exceptions to ProblemDetails responses.
/// Maps domain exceptions to appropriate HTTP status codes and ensures
/// internal details are never exposed to clients.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionHandler"/>.
    /// </summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Falha de validação",
                MapValidationErrors(ve)),
            AuthenticationException => (
                StatusCodes.Status401Unauthorized,
                "Credenciais inválidas",
                (Dictionary<string, string[]>?)null),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado",
                null),
            ConflictException => (
                StatusCodes.Status409Conflict,
                exception.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno do servidor",
                null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning("Domain exception: {ExceptionType} - {Message}", exception.GetType().Name, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static Dictionary<string, string[]> MapValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(e => e.ErrorMessage).ToArray());
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
