using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Paga.Api.ExceptionHandling;
using Paga.Application.Exceptions;
using AuthenticationException = Paga.Application.Exceptions.AuthenticationException;

namespace Paga.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(responseBody);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn400WithErrors_WhenValidationException()
    {
        // Arrange
        var context = CreateHttpContext();
        var failures = new List<ValidationFailure>
        {
            new("Email", "O campo email é obrigatório."),
            new("Password", "A senha deve ter no mínimo 6 caracteres.")
        };
        var exception = new ValidationException(failures);

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = await ReadResponseAsync(context);
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("title").GetString().Should().Be("Falha de validação");

        var errors = problem.GetProperty("errors");
        errors.GetProperty("email")[0].GetString().Should().Be("O campo email é obrigatório.");
        errors.GetProperty("password")[0].GetString().Should().Be("A senha deve ter no mínimo 6 caracteres.");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn404_WhenNotFoundException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new NotFoundException("Usuário não encontrado.");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var problem = await ReadResponseAsync(context);
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.GetProperty("title").GetString().Should().Be("Recurso não encontrado");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn409_WhenConflictException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ConflictException("O email informado já está cadastrado.");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var problem = await ReadResponseAsync(context);
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("title").GetString().Should().Be("O email informado já está cadastrado.");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn401_WhenAuthenticationException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new AuthenticationException("Credenciais inválidas");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var problem = await ReadResponseAsync(context);
        problem.GetProperty("status").GetInt32().Should().Be(401);
        problem.GetProperty("title").GetString().Should().Be("Credenciais inválidas");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturn500WithoutStackTrace_WhenGenericException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Secret internal error");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var problem = await ReadResponseAsync(context);
        problem.GetProperty("status").GetInt32().Should().Be(500);
        problem.GetProperty("title").GetString().Should().Be("Erro interno do servidor");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rawBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        rawBody.Should().NotContain("Secret internal error");
        rawBody.Should().NotContain("StackTrace");
    }
}
