using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HQStudio.Setup.UI;

/// <summary>Looks a Geometry up by its resource key ("IconCheck").</summary>
public sealed class IconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && Application.Current?.TryFindResource(key) is Geometry g ? g : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

public sealed class NullOrEmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Colour of one of the three password-strength segments (parameter = 1..3).</summary>
public sealed class StrengthSegmentBrushConverter : IValueConverter
{
    private static readonly Brush Empty = Frozen(0x2A, 0x2A, 0x2A);
    private static readonly Brush Weak = Frozen(0xF4, 0x43, 0x36);
    private static readonly Brush Medium = Frozen(0xFF, 0xC1, 0x07);
    private static readonly Brush Strong = Frozen(0x4C, 0xAF, 0x50);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var strength = value is int i ? i : 0;
        var segment = int.Parse(parameter?.ToString() ?? "1", CultureInfo.InvariantCulture);
        if (strength < segment)
            return Empty;
        return strength switch { 1 => Weak, 2 => Medium, _ => Strong };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
