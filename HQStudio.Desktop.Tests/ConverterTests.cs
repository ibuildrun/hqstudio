using FluentAssertions;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для WPF конвертеров (логика без WPF зависимостей)
/// </summary>
public class ConverterLogicTests
{
    [Theory]
    [InlineData(true, "Visible")]
    [InlineData(false, "Collapsed")]
    public void BoolToVisibility_ReturnsCorrectValue(bool input, string expected)
    {
        var result = BoolToVisibilityLogic.Convert(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Visible", true)]
    [InlineData("Collapsed", false)]
    [InlineData("Hidden", false)]
    public void BoolToVisibility_ConvertBack_ReturnsCorrectValue(string input, bool expected)
    {
        var result = BoolToVisibilityLogic.ConvertBack(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InverseBool_ReturnsCorrectValue(bool input, bool expected)
    {
        var result = InverseBoolLogic.Convert(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "Collapsed")]
    [InlineData(false, "Visible")]
    public void InverseBoolToVisibility_ReturnsCorrectValue(bool input, string expected)
    {
        var result = InverseBoolToVisibilityLogic.Convert(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "Collapsed")]
    [InlineData(1, "Visible")]
    [InlineData(5, "Visible")]
    [InlineData(-1, "Collapsed")]
    public void CountToVisibility_ReturnsCorrectValue(int count, string expected)
    {
        var result = CountToVisibilityLogic.Convert(count);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "Collapsed")]
    [InlineData(1, "Collapsed")]
    [InlineData(2, "Visible")]
    [InlineData(10, "Visible")]
    public void GreaterThanOneToVisibility_ReturnsCorrectValue(int count, string expected)
    {
        var result = GreaterThanOneToVisibilityLogic.Convert(count);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.5, false)]
    [InlineData(0.0, false)]
    public void BoolToOpacity_ReturnsCorrectValue(double opacity, bool expected)
    {
        var result = BoolToOpacityLogic.ConvertBack(opacity);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 0.5)]
    public void BoolToOpacity_Convert_ReturnsCorrectValue(bool input, double expected)
    {
        var result = BoolToOpacityLogic.Convert(input);
        result.Should().Be(expected);
    }
}

/// <summary>
/// Тесты для EqualityConverter логики
/// </summary>
public class EqualityLogicTests
{
    [Theory]
    [InlineData("test", "test", true)]
    [InlineData("test", "other", false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(null, null, true)]
    public void Equality_ReturnsCorrectValue(object? a, object? b, bool expected)
    {
        var result = EqualityLogic.AreEqual(a, b);
        result.Should().Be(expected);
    }
}

// Converter logic implementations (без WPF зависимостей)

public static class BoolToVisibilityLogic
{
    public static string Convert(bool value) => value ? "Visible" : "Collapsed";
    public static bool ConvertBack(string visibility) => visibility == "Visible";
}

public static class InverseBoolLogic
{
    public static bool Convert(bool value) => !value;
}

public static class InverseBoolToVisibilityLogic
{
    public static string Convert(bool value) => value ? "Collapsed" : "Visible";
}

public static class CountToVisibilityLogic
{
    public static string Convert(int count) => count > 0 ? "Visible" : "Collapsed";
}

public static class GreaterThanOneToVisibilityLogic
{
    public static string Convert(int count) => count > 1 ? "Visible" : "Collapsed";
}

public static class BoolToOpacityLogic
{
    public static double Convert(bool value) => value ? 1.0 : 0.5;
    public static bool ConvertBack(double opacity) => opacity >= 1.0;
}

public static class EqualityLogic
{
    public static bool AreEqual(object? a, object? b) => Equals(a, b);
}
