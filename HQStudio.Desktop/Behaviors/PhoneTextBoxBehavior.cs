using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HQStudio.Desktop.Utils;

namespace HQStudio.Desktop.Behaviors;

public static class PhoneTextBoxBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PhoneTextBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.TextChanged += OnTextChanged;
                textBox.PreviewKeyDown += OnPreviewKeyDown;
                textBox.LostFocus += OnLostFocus;
            }
            else
            {
                textBox.TextChanged -= OnTextChanged;
                textBox.PreviewKeyDown -= OnPreviewKeyDown;
                textBox.LostFocus -= OnLostFocus;
            }
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsReadOnly)
        {
            var cursorPosition = textBox.SelectionStart;
            var originalLength = textBox.Text.Length;
            var formatted = PhoneFormatter.FormatAsYouType(textBox.Text);

            if (textBox.Text != formatted)
            {
                textBox.TextChanged -= OnTextChanged;
                textBox.Text = formatted;
                var lengthDifference = formatted.Length - originalLength;
                textBox.SelectionStart = Math.Max(0, Math.Min(formatted.Length, cursorPosition + lengthDifference));
                textBox.TextChanged += OnTextChanged;
            }
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var allowedKeys = new[] { Key.Back, Key.Delete, Key.Tab, Key.Enter, Key.Left, Key.Right, Key.Home, Key.End };
        if (!allowedKeys.Contains(e.Key) && 
            !(e.Key >= Key.D0 && e.Key <= Key.D9) && 
            !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
        {
            e.Handled = true;
        }
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = PhoneFormatter.Format(textBox.Text);
        }
    }
}
