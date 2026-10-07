using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace HQStudio.Setup.UI;

/// <summary>Runs code on the UI thread when a dispatcher exists (no-op indirection in unit tests).</summary>
public static class UiDispatcher
{
    private static Dispatcher? _dispatcher;

    public static void Initialize(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public static void Post(Action action)
    {
        var d = _dispatcher;
        if (d == null || d.CheckAccess())
            action();
        else
            d.BeginInvoke(action);
    }
}

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Raise(name);
        return true;
    }

    protected void Raise([CallerMemberName] string? name = null)
    {
        if (name == null)
            return;
        UiDispatcher.Post(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => UiDispatcher.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
}

/// <summary>One button of the wizard footer; the current page decides what it says and does.</summary>
public sealed class ActionButton : ViewModelBase
{
    private string _text = "";
    private bool _isVisible;
    private Action? _execute;
    private Func<bool>? _canExecute;

    public ActionButton()
    {
        Command = new RelayCommand(() => _execute?.Invoke(), () => _isVisible && (_canExecute?.Invoke() ?? true));
    }

    public RelayCommand Command { get; }

    public string Text
    {
        get => _text;
        private set => Set(ref _text, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => Set(ref _isVisible, value);
    }

    public void Show(string text, Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        Text = text;
        IsVisible = true;
        Command.RaiseCanExecuteChanged();
    }

    public void Hide()
    {
        IsVisible = false;
        Command.RaiseCanExecuteChanged();
    }

    public void Refresh() => Command.RaiseCanExecuteChanged();

    public void SetText(string text) => Text = text;
}
