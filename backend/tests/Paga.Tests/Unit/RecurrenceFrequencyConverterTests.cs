using FluentAssertions;
using Paga.Domain.Enums;
using Paga.Infrastructure.Persistence.Converters;

namespace Paga.Tests.Unit;

public class RecurrenceFrequencyConverterTests
{
    private readonly Func<RecurrenceFrequency?, string?> _toProvider;
    private readonly Func<string?, RecurrenceFrequency?> _fromProvider;

    public RecurrenceFrequencyConverterTests()
    {
        var converter = new RecurrenceFrequencyConverter();
        _toProvider = converter.ConvertToProviderExpression.Compile();
        _fromProvider = converter.ConvertFromProviderExpression.Compile();
    }

    [Fact]
    public void ConvertToProvider_DeveConverterWeeklyParaTexto()
    {
        // Arrange
        RecurrenceFrequency? value = RecurrenceFrequency.Weekly;

        // Act
        var result = _toProvider(value);

        // Assert
        result.Should().Be("weekly");
    }

    [Fact]
    public void ConvertToProvider_DeveConverterMonthlyParaTexto()
    {
        // Arrange
        RecurrenceFrequency? value = RecurrenceFrequency.Monthly;

        // Act
        var result = _toProvider(value);

        // Assert
        result.Should().Be("monthly");
    }

    [Fact]
    public void ConvertToProvider_DeveConverterYearlyParaTexto()
    {
        // Arrange
        RecurrenceFrequency? value = RecurrenceFrequency.Yearly;

        // Act
        var result = _toProvider(value);

        // Assert
        result.Should().Be("yearly");
    }

    [Fact]
    public void ConvertToProvider_DeveConverterNullParaNull()
    {
        // Arrange
        RecurrenceFrequency? value = null;

        // Act
        var result = _toProvider(value);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Weekly, "weekly")]
    [InlineData(RecurrenceFrequency.Monthly, "monthly")]
    [InlineData(RecurrenceFrequency.Yearly, "yearly")]
    public void ConvertFromProvider_DeveConverterTextoParaEnum(RecurrenceFrequency expected, string text)
    {
        // Arrange & Act
        var result = _fromProvider(text);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertFromProvider_DeveConverterNullParaNull()
    {
        // Arrange & Act
        var result = _fromProvider(null);

        // Assert
        result.Should().BeNull();
    }
}
