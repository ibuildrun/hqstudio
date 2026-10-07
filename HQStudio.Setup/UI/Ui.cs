using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace HQStudio.Setup.UI;

/// <summary>Attached properties used by the styles.</summary>
public static class Ui
{
    public static readonly DependencyProperty HasErrorProperty = DependencyProperty.RegisterAttached(
        "HasError", typeof(bool), typeof(Ui), new PropertyMetadata(false));

    public static bool GetHasError(DependencyObject d) => (bool)d.GetValue(HasErrorProperty);
    public static void SetHasError(DependencyObject d, bool value) => d.SetValue(HasErrorProperty, value);

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder", typeof(string), typeof(Ui), new PropertyMetadata(""));

    public static string GetPlaceholder(DependencyObject d) => (string)d.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(DependencyObject d, string value) => d.SetValue(PlaceholderProperty, value);

    /// <summary>False in --render-pages mode: pictures must show the final state, not a frame of an animation.</summary>
    public static bool AnimationsEnabled { get; set; } = true;

    /// <summary>Progress value that glides to the new number instead of jumping.</summary>
    public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached(
        "SmoothValue", typeof(double), typeof(Ui), new PropertyMetadata(0.0, OnSmoothValueChanged));

    public static double GetSmoothValue(DependencyObject d) => (double)d.GetValue(SmoothValueProperty);
    public static void SetSmoothValue(DependencyObject d, double value) => d.SetValue(SmoothValueProperty, value);

    private static void OnSmoothValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not System.Windows.Controls.Primitives.RangeBase range)
            return;

        var target = (double)e.NewValue;
        if (!AnimationsEnabled)
        {
            range.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
            range.Value = target;
            return;
        }

        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(380))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        range.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }
}
