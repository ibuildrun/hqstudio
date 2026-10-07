using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace HQStudio.Setup.UI.Pages;

public partial class AccountPageView : UserControl
{
    private AccountPageViewModel? _vm;
    private bool _syncing;

    public AccountPageView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => Attach(e.NewValue as AccountPageViewModel);
        Loaded += (_, _) => Dispatcher.BeginInvoke(() => FirstNameBox.Focus());
    }

    private void Attach(AccountPageViewModel? vm)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.FocusRequested -= OnFocusRequested;
        }

        _vm = vm;
        if (_vm == null)
            return;

        _vm.PropertyChanged += OnVmPropertyChanged;
        _vm.FocusRequested += OnFocusRequested;
        PushPasswords();
    }

    // PasswordBox cannot be bound, so the two boxes are mirrored by hand.
    private void PushPasswords()
    {
        if (_vm == null)
            return;
        _syncing = true;
        if (PasswordBoxMasked.Password != _vm.Password) PasswordBoxMasked.Password = _vm.Password;
        if (RepeatBoxMasked.Password != _vm.PasswordRepeat) RepeatBoxMasked.Password = _vm.PasswordRepeat;
        _syncing = false;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AccountPageViewModel.Password) or nameof(AccountPageViewModel.PasswordRepeat))
            PushPasswords();
    }

    private void OnFocusRequested(string field)
    {
        var masked = _vm?.ShowPassword != true;
        UIElement? target = field switch
        {
            nameof(AccountPageViewModel.FirstName) => FirstNameBox,
            nameof(AccountPageViewModel.LastName) => LastNameBox,
            nameof(AccountPageViewModel.Password) => masked ? PasswordBoxMasked : PasswordBoxPlain,
            _ => masked ? RepeatBoxMasked : RepeatBoxPlain
        };
        target.Focus();
    }

    private void Password_Changed(object sender, RoutedEventArgs e)
    {
        if (!_syncing && _vm != null)
            _vm.Password = PasswordBoxMasked.Password;
    }

    private void Repeat_Changed(object sender, RoutedEventArgs e)
    {
        if (!_syncing && _vm != null)
            _vm.PasswordRepeat = RepeatBoxMasked.Password;
    }

    private void TogglePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;
        _vm.ShowPassword = !_vm.ShowPassword;
        Dispatcher.BeginInvoke(() => (_vm.ShowPassword ? (UIElement)PasswordBoxPlain : PasswordBoxMasked).Focus());
    }

    private void FirstName_LostFocus(object sender, RoutedEventArgs e) => _vm?.Touch(nameof(AccountPageViewModel.FirstName));
    private void LastName_LostFocus(object sender, RoutedEventArgs e) => _vm?.Touch(nameof(AccountPageViewModel.LastName));
    private void Password_LostFocus(object sender, RoutedEventArgs e) => _vm?.Touch(nameof(AccountPageViewModel.Password));
    private void Repeat_LostFocus(object sender, RoutedEventArgs e) => _vm?.Touch(nameof(AccountPageViewModel.PasswordRepeat));
}
