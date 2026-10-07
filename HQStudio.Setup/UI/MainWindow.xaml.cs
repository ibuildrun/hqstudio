using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Setup.UI;

public partial class MainWindow : Window
{
    private readonly WizardViewModel _vm;

    public MainWindow(WizardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Shell.DataContext = vm;

        Loaded += (_, _) => _vm.Start();
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_vm.AllowClose)
            return;

        // Alt+F4 and the taskbar go through the same confirmation as the cross button.
        e.Cancel = true;
        _vm.RequestClose();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _vm.HandleEscape())
            e.Handled = true;
    }
}
