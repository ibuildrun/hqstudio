using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.UI.Pages;

public interface IWizardHost
{
    InstallAnswers Answers { get; }
    SetupServices Services { get; }
    SetupOptions Options { get; }

    void Next();
    void Back();
    void RequestClose();
    void CloseNow();
    void ShowDone(Install.InstallContext context);
    void CopyToClipboard(string text);
    void ShowConfirm(ConfirmDialogViewModel dialog);
}

/// <summary>Zero-based index of the wizard step shown in the left rail.</summary>
public enum WizardStep
{
    Welcome = 0,
    Docker = 1,
    Account = 2,
    Keys = 3,
    Summary = 4,
    Install = 5,
    Done = 6
}

public abstract class PageViewModel : ViewModelBase
{
    protected PageViewModel(IWizardHost host) => Host = host;

    protected IWizardHost Host { get; }

    public abstract WizardStep Step { get; }
    public abstract string Title { get; }
    public virtual string Subtitle => "";
    public virtual bool ShowHeader => true;

    public ActionButton Primary { get; } = new();
    public ActionButton Secondary { get; } = new();
    public ActionButton Tertiary { get; } = new();

    /// <summary>Closing the window while the page is busy needs the user's confirmation.</summary>
    public virtual bool IsBusy => false;

    public virtual void OnEntered() { }
    public virtual void OnLeft() { }

    /// <summary>Esc key. Returns true when the page handled it.</summary>
    public virtual bool OnEscape()
    {
        if (!Secondary.IsVisible || !Secondary.Command.CanExecute(null))
            return false;
        Secondary.Command.Execute(null);
        return true;
    }
}

public sealed class ConfirmDialogViewModel : ViewModelBase
{
    public ConfirmDialogViewModel(string title, string message, string confirmText, string cancelText, Action onConfirm, bool isDanger = false)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        IsDanger = isDanger;
        _onConfirm = onConfirm;
    }

    private readonly Action _onConfirm;

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public bool IsDanger { get; }

    public event Action? Dismissed;

    public void Confirm()
    {
        Dismissed?.Invoke();
        _onConfirm();
    }

    public void Cancel() => Dismissed?.Invoke();
}
