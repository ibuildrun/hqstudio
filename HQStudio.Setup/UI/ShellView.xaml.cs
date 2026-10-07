using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace HQStudio.Setup.UI;

public partial class ShellView : UserControl
{
    private WizardViewModel? _vm;

    public ShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as WizardViewModel;
        if (_vm != null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WizardViewModel.CurrentPage))
            PlayPageTransition();
        else if (e.PropertyName == nameof(WizardViewModel.HasDialog) && _vm?.HasDialog == true)
            Dispatcher.BeginInvoke(() => DialogCancel.Focus());
    }

    // A short fade and slide so a new page does not just pop in.
    private void PlayPageTransition()
    {
        if (!Ui.AnimationsEnabled || !IsLoaded)
            return;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var slide = new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        PageHost.BeginAnimation(OpacityProperty, fade);
        PageShift.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
    }

    private void Drag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
            window.WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _vm?.RequestClose();

    private void DialogConfirm_Click(object sender, RoutedEventArgs e) => _vm?.Dialog?.Confirm();

    private void DialogCancel_Click(object sender, RoutedEventArgs e) => _vm?.Dialog?.Cancel();
}
