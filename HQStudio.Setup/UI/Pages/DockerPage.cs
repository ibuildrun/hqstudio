using System.Net.Http;
using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.UI.Pages;

public enum DockerPageState
{
    Checking,
    Running,
    Stopped,
    StartingEngine,
    Missing,
    Downloading,
    Installing,
    RebootRequired
}

/// <summary>"ok", "warn", "error", "busy" - drives the colour and icon of the status card.</summary>
public static class StatusKinds
{
    public const string Ok = "ok";
    public const string Warn = "warn";
    public const string Error = "error";
    public const string Busy = "busy";
}

public sealed class DockerPageViewModel : PageViewModel
{
    public const string DownloadPageUrl = "https://www.docker.com/products/docker-desktop/";
    public const string GuideUrl = "https://docs.docker.com/desktop/setup/install/windows-install/";
    public const int MaxEngineStartPolls = 80;

    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    private readonly IDockerClient _docker;
    private readonly IDockerInstaller _installer;
    private readonly IShellActions _shell;

    private CancellationTokenSource? _pollCts;
    private CancellationTokenSource? _workCts;
    private int _engineStartPolls;
    private bool _startedAfterInstall;
    private bool _rebootAfterTimeout;

    private DockerPageState _state = DockerPageState.Checking;
    private bool _skipDocker;
    private double _downloadPercent;
    private string _downloadText = "";
    private string? _errorText;
    private string? _postponedNote;

    public DockerPageViewModel(IWizardHost host) : base(host)
    {
        _docker = host.Services.Docker;
        _installer = host.Services.DockerInstaller;
        _shell = host.Services.Shell;

        StartDockerCommand = new RelayCommand(() => _ = StartDockerAsync(), () => State == DockerPageState.Stopped);
        InstallAutoCommand = new RelayCommand(() => _ = InstallAutomaticallyAsync(), () => State == DockerPageState.Missing);
        OpenDownloadPageCommand = new RelayCommand(() => _shell.OpenUrl(DownloadPageUrl));
        OpenGuideCommand = new RelayCommand(() => _shell.OpenUrl(GuideUrl));
        CancelDownloadCommand = new RelayCommand(() => _workCts?.Cancel(), () => State == DockerPageState.Downloading);
        RebootNowCommand = new RelayCommand(() => _shell.RebootNow());
        RebootLaterCommand = new RelayCommand(RebootLater);
    }

    public override WizardStep Step => WizardStep.Docker;
    public override string Title => "Docker";
    public override string Subtitle => "Бесплатная программа, в которой работает сайт студии.";

    public RelayCommand StartDockerCommand { get; }
    public RelayCommand InstallAutoCommand { get; }
    public RelayCommand OpenDownloadPageCommand { get; }
    public RelayCommand OpenGuideCommand { get; }
    public RelayCommand CancelDownloadCommand { get; }
    public RelayCommand RebootNowCommand { get; }
    public RelayCommand RebootLaterCommand { get; }

    public DockerPageState State
    {
        get => _state;
        internal set
        {
            if (!Set(ref _state, value))
                return;

            Raise(nameof(IsRunning));
            Raise(nameof(IsStopped));
            Raise(nameof(IsStarting));
            Raise(nameof(IsMissing));
            Raise(nameof(IsDownloading));
            Raise(nameof(IsInstalling));
            Raise(nameof(IsRebootRequired));
            Raise(nameof(IsInstallingOrStarting));
            Raise(nameof(ShowSkip));
            Raise(nameof(StatusKind));
            Raise(nameof(StatusIsOk));
            Raise(nameof(StatusIsWarn));
            Raise(nameof(StatusIsError));
            Raise(nameof(StatusIsBusy));
            Raise(nameof(StatusTitle));
            Raise(nameof(StatusText));
            Raise(nameof(IsBusy));
            RefreshCommands();
        }
    }

    public bool IsRunning => State == DockerPageState.Running;
    public bool IsStopped => State == DockerPageState.Stopped;
    public bool IsStarting => State == DockerPageState.StartingEngine;
    public bool IsMissing => State == DockerPageState.Missing;
    public bool IsDownloading => State == DockerPageState.Downloading;
    public bool IsInstalling => State == DockerPageState.Installing;
    public bool IsRebootRequired => State == DockerPageState.RebootRequired;
    public bool IsInstallingOrStarting => State is DockerPageState.Installing or DockerPageState.StartingEngine;

    /// <summary>The "install Docker later" tick makes sense whenever Docker is not ready.</summary>
    public bool ShowSkip => State is not (DockerPageState.Running or DockerPageState.Checking or DockerPageState.RebootRequired
        or DockerPageState.Downloading or DockerPageState.Installing);

    public bool SkipDocker
    {
        get => _skipDocker;
        set
        {
            if (Set(ref _skipDocker, value))
                Primary.Refresh();
        }
    }

    public double DownloadPercent
    {
        get => _downloadPercent;
        internal set => Set(ref _downloadPercent, value);
    }

    public string DownloadText
    {
        get => _downloadText;
        internal set => Set(ref _downloadText, value);
    }

    public string? ErrorText
    {
        get => _errorText;
        internal set
        {
            if (Set(ref _errorText, value))
                Raise(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public string? PostponedNote
    {
        get => _postponedNote;
        internal set
        {
            if (Set(ref _postponedNote, value))
                Raise(nameof(HasPostponedNote));
        }
    }

    public bool HasPostponedNote => !string.IsNullOrEmpty(PostponedNote);

    public override bool IsBusy => State is DockerPageState.Downloading or DockerPageState.Installing;

    public string StatusKind => State switch
    {
        DockerPageState.Running => StatusKinds.Ok,
        DockerPageState.Stopped => StatusKinds.Warn,
        DockerPageState.Missing => StatusKinds.Error,
        DockerPageState.RebootRequired => StatusKinds.Warn,
        _ => StatusKinds.Busy
    };

    public bool StatusIsOk => StatusKind == StatusKinds.Ok;
    public bool StatusIsWarn => StatusKind == StatusKinds.Warn;
    public bool StatusIsError => StatusKind == StatusKinds.Error;
    public bool StatusIsBusy => StatusKind == StatusKinds.Busy;

    public string StatusTitle => State switch
    {
        DockerPageState.Checking => "Ищу Docker на этом компьютере...",
        DockerPageState.Running => "Docker найден и работает",
        DockerPageState.Stopped => "Docker установлен, но не запущен",
        DockerPageState.StartingEngine => "Запускаю Docker...",
        DockerPageState.Missing => "Docker не найден",
        DockerPageState.Downloading => "Скачиваю Docker",
        DockerPageState.Installing => "Устанавливаю Docker",
        DockerPageState.RebootRequired => "Нужна перезагрузка",
        _ => ""
    };

    public string StatusText => State switch
    {
        DockerPageState.Checking => "Это займёт несколько секунд.",
        DockerPageState.Running => "Всё в порядке, можно переходить к следующему шагу.",
        DockerPageState.Stopped => "Нажмите кнопку ниже: установщик сам запустит Docker и дождётся, пока он будет готов.",
        DockerPageState.StartingEngine => "Обычно это занимает 1-2 минуты. Окно можно не закрывать, установщик сам увидит, когда Docker будет готов.",
        DockerPageState.Missing => "Он нужен, чтобы на вашем компьютере работал сайт.",
        DockerPageState.Downloading => "Файл большой, поэтому нужно немного подождать.",
        DockerPageState.Installing => "Когда Windows спросит разрешение, нажмите «Да». Установка занимает 5-10 минут, окно закрывать не нужно.",
        DockerPageState.RebootRequired => _rebootAfterTimeout
            ? "Docker установлен, но пока не отвечает. Чаще всего помогает перезагрузка компьютера. Если Docker просит принять условия, примите их."
            : "Docker установлен, но Windows должна перезагрузиться, чтобы он заработал.",
        _ => ""
    };

    internal void ConfigureForPreview()
    {
        Primary.Show("Далее", Advance, () => State == DockerPageState.Running || SkipDocker);
        Secondary.Show("Назад", Host.Back, () => !IsBusy);
        Tertiary.Hide();
    }

    public override void OnEntered()
    {
        ConfigureForPreview();

        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    public override void OnLeft()
    {
        _pollCts?.Cancel();
        _pollCts = null;
    }

    public async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await RefreshAsync(ct);
                await Task.Delay(PollInterval, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>One detection pass; the page calls it every 3 seconds.</summary>
    public async Task RefreshAsync(CancellationToken ct)
    {
        if (State is DockerPageState.Downloading or DockerPageState.Installing or DockerPageState.RebootRequired)
            return;

        var status = await _docker.GetStatusAsync(ct);
        ct.ThrowIfCancellationRequested();

        switch (status)
        {
            case DockerStatus.Running:
                ErrorText = null;
                State = DockerPageState.Running;
                break;

            case DockerStatus.InstalledNotRunning:
                if (State == DockerPageState.StartingEngine)
                    OnEngineStillStarting();
                else
                    State = DockerPageState.Stopped;
                break;

            default:
                if (State != DockerPageState.StartingEngine)
                    State = DockerPageState.Missing;
                break;
        }
    }

    private void OnEngineStillStarting()
    {
        if (++_engineStartPolls < MaxEngineStartPolls)
            return;

        if (_startedAfterInstall)
        {
            _rebootAfterTimeout = true;
            State = DockerPageState.RebootRequired;
        }
        else
        {
            ErrorText = "Docker так и не запустился. Откройте Docker Desktop вручную, дождитесь значка кита и подождите, пока установщик это увидит.";
            State = DockerPageState.Stopped;
        }
    }

    private void Advance()
    {
        Host.Answers.SkipSite = State != DockerPageState.Running && SkipDocker;
        Host.Next();
    }

    public async Task StartDockerAsync()
    {
        ErrorText = null;
        if (!_docker.TryStartDesktop())
        {
            ErrorText = "Не получилось запустить Docker Desktop. Откройте его через меню «Пуск».";
            return;
        }

        BeginEngineStart(afterInstall: false);
        await RefreshAsync(CancellationToken.None);
    }

    private void BeginEngineStart(bool afterInstall)
    {
        _rebootAfterTimeout = false;
        _engineStartPolls = 0;
        _startedAfterInstall = afterInstall;
        State = DockerPageState.StartingEngine;
    }

    public async Task InstallAutomaticallyAsync()
    {
        ErrorText = null;
        _workCts?.Dispose();
        _workCts = new CancellationTokenSource();
        var ct = _workCts.Token;

        try
        {
            DownloadPercent = 0;
            DownloadText = "Подключаюсь к docker.com...";
            State = DockerPageState.Downloading;

            var path = await _installer.DownloadAsync(new DirectProgress<DownloadProgress>(OnDownloadProgress), ct);

            State = DockerPageState.Installing;
            var result = await _installer.RunAsync(path, ct);

            switch (result.Outcome)
            {
                case DockerInstallOutcome.Success:
                    _docker.TryStartDesktop();
                    BeginEngineStart(afterInstall: true);
                    break;

                case DockerInstallOutcome.RebootRequired:
                    State = DockerPageState.RebootRequired;
                    break;

                case DockerInstallOutcome.Declined:
                    State = DockerPageState.Missing;
                    ErrorText = "Вы не разрешили установку. Нажмите кнопку ещё раз и ответьте «Да» в окне Windows.";
                    break;

                default:
                    State = DockerPageState.Missing;
                    ErrorText = $"Установщик Docker завершился с ошибкой (код {result.ExitCode}). Попробуйте ещё раз или установите Docker вручную.";
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            State = DockerPageState.Missing;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            State = DockerPageState.Missing;
            ErrorText = "Не удалось скачать Docker. Проверьте интернет (если сайт docker.com не открывается, включите VPN) или скачайте Docker вручную.";
        }
    }

    private void OnDownloadProgress(DownloadProgress p)
    {
        if (p.Fraction is { } fraction)
        {
            DownloadPercent = fraction * 100;
            DownloadText = $"Скачано {ByteSize.Format(p.Received)} из {ByteSize.Format(p.Total!.Value)} ({(int)Math.Round(fraction * 100)}%)";
        }
        else
        {
            DownloadText = $"Скачано {ByteSize.Format(p.Received)}";
        }
    }

    private void RebootLater()
    {
        PostponedNote = "Хорошо. Когда перезагрузите компьютер, запустите установщик HQ Studio ещё раз.";
        State = DockerPageState.Missing;
    }

    public void CancelBackgroundWork()
    {
        _workCts?.Cancel();
        _pollCts?.Cancel();
    }

    private void RefreshCommands()
    {
        StartDockerCommand.RaiseCanExecuteChanged();
        InstallAutoCommand.RaiseCanExecuteChanged();
        CancelDownloadCommand.RaiseCanExecuteChanged();
        Primary.Refresh();
        Secondary.Refresh();
    }

    /// <summary>IProgress that calls back immediately (Progress&lt;T&gt; would post to a context that may not exist).</summary>
    private sealed class DirectProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public DirectProgress(Action<T> handler) => _handler = handler;

        public void Report(T value) => _handler(value);
    }
}
