using System.Collections.ObjectModel;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.UI.Pages;

namespace HQStudio.Setup.UI;

public enum StepState
{
    Pending,
    Active,
    Done,
    Skipped
}

public sealed class StepItem : ViewModelBase
{
    private StepState _state;

    public StepItem(int number, string name)
    {
        Number = number;
        Name = name;
    }

    public int Number { get; }
    public string Name { get; }
    public bool IsLast { get; internal set; }

    private bool _hasError;

    /// <summary>The step failed (shown in red in the rail).</summary>
    public bool HasError
    {
        get => _hasError;
        internal set => Set(ref _hasError, value);
    }

    public StepState State
    {
        get => _state;
        internal set
        {
            if (!Set(ref _state, value))
                return;
            Raise(nameof(IsActive));
            Raise(nameof(IsDone));
            Raise(nameof(IsPending));
            Raise(nameof(IsSkipped));
        }
    }

    public bool IsActive => State == StepState.Active;
    public bool IsDone => State == StepState.Done;
    public bool IsPending => State == StepState.Pending;
    public bool IsSkipped => State == StepState.Skipped;
}

/// <summary>Owns the wizard: the pages, the left rail, navigation and the confirmation overlay.</summary>
public sealed class WizardViewModel : ViewModelBase, IWizardHost
{
    private readonly Action _closeWindow;
    private readonly Action<string> _copy;
    private PageViewModel _currentPage;
    private ConfirmDialogViewModel? _dialog;

    public WizardViewModel(SetupServices services, SetupOptions options, Action closeWindow, Action<string>? copyToClipboard = null)
    {
        Services = services;
        Options = options;
        _closeWindow = closeWindow;
        _copy = copyToClipboard ?? (_ => { });

        Welcome = new WelcomePageViewModel(this);
        Docker = new DockerPageViewModel(this);
        Account = new AccountPageViewModel(this);
        Keys = new KeysPageViewModel(this);
        Summary = new SummaryPageViewModel(this);
        Install = new InstallPageViewModel(this);
        Done = new DonePageViewModel(this);
        Pages = new PageViewModel[] { Welcome, Docker, Account, Keys, Summary, Install, Done };

        var names = new[] { "Приветствие", "Docker", "Аккаунт", "Ключи", "Проверка", "Установка", "Готово" };
        for (var i = 0; i < names.Length; i++)
            Steps.Add(new StepItem(i + 1, names[i]) { IsLast = i == names.Length - 1 });

        Install.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InstallPageViewModel.State))
                Steps[(int)WizardStep.Install].HasError = Install.State == InstallRunState.Failed;
        };

        _currentPage = Welcome;
    }

    public InstallAnswers Answers { get; } = new();
    public SetupServices Services { get; }
    public SetupOptions Options { get; }

    public WelcomePageViewModel Welcome { get; }
    public DockerPageViewModel Docker { get; }
    public AccountPageViewModel Account { get; }
    public KeysPageViewModel Keys { get; }
    public SummaryPageViewModel Summary { get; }
    public InstallPageViewModel Install { get; }
    public DonePageViewModel Done { get; }
    public IReadOnlyList<PageViewModel> Pages { get; }

    public ObservableCollection<StepItem> Steps { get; } = new();

    public string VersionText => "Версия " + VersionInfo.Current;
    public bool IsSimulation => Services.IsSimulation;

    /// <summary>Set before the window is closed for real, so the Closing handler lets it through.</summary>
    public bool AllowClose { get; private set; }

    public PageViewModel CurrentPage
    {
        get => _currentPage;
        private set => Set(ref _currentPage, value);
    }

    public ConfirmDialogViewModel? Dialog
    {
        get => _dialog;
        private set
        {
            if (Set(ref _dialog, value))
                Raise(nameof(HasDialog));
        }
    }

    public bool HasDialog => Dialog != null;

    public void Start() => NavigateTo(Welcome);

    public void Next()
    {
        var index = IndexOf(CurrentPage);
        if (index < 0 || index >= Pages.Count - 1)
            return;

        var next = Pages[index + 1];
        // Account and keys only make sense for the site, which is skipped without Docker.
        if (Answers.SkipSite && (next == Account || next == Keys))
            next = Summary;
        NavigateTo(next);
    }

    public void Back()
    {
        var index = IndexOf(CurrentPage);
        if (index <= 0 || CurrentPage == Install || CurrentPage == Done)
            return;

        var previous = Pages[index - 1];
        if (Answers.SkipSite && (previous == Keys || previous == Account))
            previous = Docker;
        NavigateTo(previous);
    }

    public void ShowDone(InstallContext context)
    {
        Done.Load(context);
        NavigateTo(Done);
    }

    public void NavigateTo(PageViewModel page)
    {
        var old = _currentPage;
        if (!ReferenceEquals(old, page))
            old.OnLeft();

        CurrentPage = page;
        UpdateSteps(page);
        page.OnEntered();
    }

    /// <summary>Shows a page for --render-pages without starting its background work.</summary>
    internal void ShowForPreview(PageViewModel page)
    {
        CurrentPage = page;
        UpdateSteps(page);
        switch (page)
        {
            case DockerPageViewModel docker:
                docker.ConfigureForPreview();
                break;
            case InstallPageViewModel:
                break;
            default:
                page.OnEntered();
                break;
        }
    }

    private void UpdateSteps(PageViewModel page)
    {
        var current = (int)page.Step;
        for (var i = 0; i < Steps.Count; i++)
        {
            var skipped = Answers.SkipSite && i is (int)WizardStep.Account or (int)WizardStep.Keys && i < current;
            Steps[i].State = i == current ? StepState.Active
                : i < current ? (skipped ? StepState.Skipped : StepState.Done)
                : StepState.Pending;
        }
        if (page == Done)
            Steps[^1].State = StepState.Done;
    }

    public bool ShouldConfirmClose => CurrentPage.IsBusy;

    public void RequestClose()
    {
        if (Dialog != null)
        {
            Dialog.Cancel();
            return;
        }

        if (!ShouldConfirmClose)
        {
            CloseNow();
            return;
        }

        var isInstall = CurrentPage == Install;
        ShowConfirm(new ConfirmDialogViewModel(
            isInstall ? "Прервать установку?" : "Закрыть установщик?",
            isInstall
                ? "Установка ещё не закончена. Если закрыть окно сейчас, сайт может остаться недоустановленным. Позже установщик можно запустить снова."
                : "Идёт скачивание или установка Docker. Если закрыть окно сейчас, Docker придётся ставить заново.",
            isInstall ? "Прервать" : "Закрыть",
            isInstall ? "Продолжить установку" : "Подождать",
            CloseNow,
            isDanger: true));
    }

    public void CloseNow()
    {
        AllowClose = true;
        Install.CancelInstall();
        Docker.CancelBackgroundWork();
        _closeWindow();
    }

    public void ShowConfirm(ConfirmDialogViewModel dialog)
    {
        dialog.Dismissed += () => Dialog = null;
        Dialog = dialog;
    }

    public void CopyToClipboard(string text) => _copy(text);

    /// <summary>Esc: closes the overlay, otherwise lets the page decide (usually "back").</summary>
    public bool HandleEscape()
    {
        if (Dialog != null)
        {
            Dialog.Cancel();
            return true;
        }
        return CurrentPage.OnEscape();
    }

    private int IndexOf(PageViewModel page)
    {
        for (var i = 0; i < Pages.Count; i++)
        {
            if (ReferenceEquals(Pages[i], page))
                return i;
        }
        return -1;
    }
}
