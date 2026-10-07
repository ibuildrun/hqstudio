using System.Collections.ObjectModel;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;

namespace HQStudio.Setup.UI.Pages;

public sealed class StageViewModel : ViewModelBase
{
    private StageState _state;
    private string? _detail;

    public StageViewModel(StageInfo info)
    {
        Id = info.Id;
        Title = info.Title;
        Update(info);
    }

    public StageId Id { get; }
    public string Title { get; }

    public StageState State
    {
        get => _state;
        private set
        {
            if (!Set(ref _state, value))
                return;
            Raise(nameof(IsPending));
            Raise(nameof(IsRunning));
            Raise(nameof(IsDone));
            Raise(nameof(IsFailed));
            Raise(nameof(IsWarning));
        }
    }

    /// <summary>What is happening now (while running), or the warning / failure text.</summary>
    public string? Detail
    {
        get => _detail;
        private set
        {
            if (Set(ref _detail, value))
                Raise(nameof(HasDetail));
        }
    }

    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail) && (IsRunning || IsWarning);

    public bool IsPending => State == StageState.Pending;
    public bool IsRunning => State == StageState.Running;
    public bool IsDone => State == StageState.Done;
    public bool IsFailed => State == StageState.Failed;
    public bool IsWarning => State == StageState.Warning;

    public void Update(StageInfo info)
    {
        State = info.State;
        Detail = info.Detail;
        Raise(nameof(HasDetail));
    }

    internal void Preview(StageState state, string? detail)
    {
        State = state;
        Detail = detail;
        Raise(nameof(HasDetail));
    }
}

public sealed class FailureViewModel
{
    public FailureViewModel(FailureInfo info)
    {
        Info = info;
        Title = info.Title;
        Message = info.Message;
        Hint = info.Hint;
        StageTitle = StageNames.Title(info.Stage);
    }

    public FailureInfo Info { get; }
    public string Title { get; }
    public string Message { get; }
    public string Hint { get; }
    public string StageTitle { get; }
}

public enum InstallRunState
{
    NotStarted,
    Running,
    Failed,
    Succeeded
}

public sealed class InstallPageViewModel : PageViewModel
{
    private CancellationTokenSource? _cts;
    private InstallEngine? _engine;
    private InstallContext? _context;
    private SetupLog? _log;

    private InstallRunState _state = InstallRunState.NotStarted;
    private double _overallPercent;
    private string _currentAction = "Подготовка...";
    private FailureViewModel? _failure;
    private bool _isLogExpanded;

    public InstallPageViewModel(IWizardHost host) : base(host) { }

    /// <summary>How long the finished progress bar stays visible before the last page; zero in tests.</summary>
    public static TimeSpan SuccessPause { get; set; } = TimeSpan.FromMilliseconds(900);

    public override WizardStep Step => WizardStep.Install;
    public override string Title => State == InstallRunState.Failed ? "Не получилось" : "Установка";

    public override string Subtitle => State switch
    {
        InstallRunState.Failed => "",
        InstallRunState.Succeeded => "Почти готово...",
        _ => "Подождите, идёт установка. Окно можно свернуть, но не закрывайте его."
    };

    public ObservableCollection<StageViewModel> Stages { get; } = new();

    public event Action<string>? LogAppended;

    public InstallRunState State
    {
        get => _state;
        internal set
        {
            if (!Set(ref _state, value))
                return;
            Raise(nameof(IsRunning));
            Raise(nameof(IsFailed));
            Raise(nameof(Title));
            Raise(nameof(Subtitle));
            Raise(nameof(IsBusy));
        }
    }

    public bool IsRunning => State is InstallRunState.Running or InstallRunState.NotStarted or InstallRunState.Succeeded;
    public bool IsFailed => State == InstallRunState.Failed;
    public override bool IsBusy => State == InstallRunState.Running;

    public double OverallPercent
    {
        get => _overallPercent;
        internal set
        {
            if (Set(ref _overallPercent, value))
                Raise(nameof(PercentText));
        }
    }

    public string PercentText => $"{(int)Math.Round(OverallPercent)}%";

    public string CurrentAction
    {
        get => _currentAction;
        internal set => Set(ref _currentAction, value);
    }

    public FailureViewModel? Failure
    {
        get => _failure;
        internal set => Set(ref _failure, value);
    }

    public bool IsLogExpanded
    {
        get => _isLogExpanded;
        set => Set(ref _isLogExpanded, value);
    }

    private IReadOnlyList<string> _previewLog = Array.Empty<string>();

    public IReadOnlyList<string> LogSnapshot => _log?.Snapshot() ?? _previewLog;

    public InstallContext? Context => _context;

    public override void OnEntered()
    {
        Secondary.Hide();
        Tertiary.Hide();
        Primary.Show("Идёт установка...", () => { }, () => false);

        if (State == InstallRunState.NotStarted)
            _ = StartAsync();
    }

    public async Task StartAsync()
    {
        _log = new SetupLog(Host.Services.Paths.SetupLog);
        foreach (var warning in Host.Options.Warnings)
            _log.Write(warning);
        _log.Write($"HQ Studio Setup {VersionInfo.Current}" + (Host.Services.IsSimulation ? " (simulation)" : ""));
        _log.LineWritten += line => UiDispatcher.Post(() => LogAppended?.Invoke(line));

        _context = new InstallContext(Host.Answers, Host.Services, _log);
        _engine = new InstallEngine(_context);

        var map = new Dictionary<StageId, StageViewModel>();
        foreach (var info in _engine.Stages)
        {
            var vm = new StageViewModel(info);
            map[info.Id] = vm;
            Stages.Add(vm);
        }

        _engine.StageChanged += info => UiDispatcher.Post(() =>
        {
            map[info.Id].Update(info);
            if (info.State == StageState.Running)
                CurrentAction = $"Шаг {Stages.IndexOf(map[info.Id]) + 1} из {Stages.Count}: {info.Title}";
        });
        _engine.ProgressChanged += (fraction, _) => UiDispatcher.Post(() => OverallPercent = Math.Max(OverallPercent, fraction * 100));

        await RunAsync();
    }

    private async Task RunAsync()
    {
        if (_engine == null || _context == null)
            return;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        Failure = null;
        State = InstallRunState.Running;
        Secondary.Hide();
        Primary.Show("Идёт установка...", () => { }, () => false);

        var outcome = await _engine.RunAsync(_cts.Token);
        switch (outcome)
        {
            case InstallOutcome.Success:
                State = InstallRunState.Succeeded;
                OverallPercent = 100;
                CurrentAction = "Готово";
                if (SuccessPause > TimeSpan.Zero)
                    await Task.Delay(SuccessPause);
                Host.ShowDone(_context);
                break;

            case InstallOutcome.Failed:
                State = InstallRunState.Failed;
                Failure = new FailureViewModel(_engine.LastFailure!);
                CurrentAction = Failure.Title;
                Primary.Show("Повторить", () => _ = RunAsync());
                Secondary.Show("Сообщить об ошибке", ReportProblem);
                break;

            default:
                State = InstallRunState.Failed;
                break;
        }
    }

    public void ReportProblem()
    {
        if (Failure == null || _log == null)
            return;

        var info = Failure.Info;
        var link = IssueReport.Build(
            new IssueReportContext(VersionInfo.Current, info.Stage, info.Kind, info.Technical, Host.Services.IsSimulation),
            _log.Snapshot(),
            _log.Masker);
        Host.Services.Shell.OpenUrl(link.Url);
    }

    public void CancelInstall() => _cts?.Cancel();

    public override bool OnEscape()
    {
        if (State != InstallRunState.Running)
            return false;
        Host.RequestClose();
        return true;
    }

    // ---- page preview (--render-pages) -------------------------------------------------------------------------

    internal void LoadPreview(IReadOnlyList<(StageId Id, StageState State, string? Detail)> stages, double percent, string action, InstallRunState state, FailureInfo? failure = null, IReadOnlyList<string>? log = null)
    {
        _previewLog = log ?? Array.Empty<string>();
        Stages.Clear();
        foreach (var (id, stageState, detail) in stages)
        {
            var vm = new StageViewModel(new StageInfo(id, StageNames.Title(id), 1));
            vm.Preview(stageState, detail);
            Stages.Add(vm);
        }

        OverallPercent = percent;
        CurrentAction = action;
        State = state;
        Failure = failure == null ? null : new FailureViewModel(failure);

        Secondary.Hide();
        if (state == InstallRunState.Failed)
        {
            Primary.Show("Повторить", () => { });
            Secondary.Show("Сообщить об ошибке", () => { });
        }
        else
        {
            Primary.Show("Идёт установка...", () => { }, () => false);
        }
    }
}
