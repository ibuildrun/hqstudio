using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using HQStudio.Services.Updates;
using HQStudio.Views.Dialogs;

namespace HQStudio.ViewModels
{
    /// <summary>State and commands of the update UI (dialog and settings section share it).</summary>
    public class UpdateViewModel : BaseViewModel
    {
        private const int MaxLogLines = 500;

        private readonly UpdateCoordinator _coordinator;
        private readonly Func<string, string, bool> _confirm;
        private readonly Action<string> _openUrl;
        private readonly Dispatcher? _dispatcher;

        private readonly ConcurrentQueue<string> _pendingLog = new();
        private readonly LinkedList<string> _logLines = new();
        private int _logFlushQueued;
        private UpdateProgress? _pendingProgress;

        private bool _isBusy;
        private double _progress;
        private string _progressStage = "";
        private string _progressDetail = "";
        private string _statusMessage = "";
        private string _errorMessage = "";
        private string _helpUrl = "";
        private string _logText = "";
        private bool _isLogVisible;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                if (SetProperty(ref _progress, value))
                    OnPropertyChanged(nameof(ProgressPercentText));
            }
        }

        public string ProgressPercentText => $"{Progress:F0}%";

        public string ProgressStage
        {
            get => _progressStage;
            private set => SetProperty(ref _progressStage, value);
        }

        public string ProgressDetail
        {
            get => _progressDetail;
            private set => SetProperty(ref _progressDetail, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public string HelpUrl
        {
            get => _helpUrl;
            private set
            {
                if (SetProperty(ref _helpUrl, value))
                    OnPropertyChanged(nameof(HasHelpUrl));
            }
        }

        public bool HasHelpUrl => !string.IsNullOrEmpty(_helpUrl);

        public string LogText
        {
            get => _logText;
            private set => SetProperty(ref _logText, value);
        }

        public bool IsLogVisible
        {
            get => _isLogVisible;
            set
            {
                if (SetProperty(ref _isLogVisible, value))
                    OnPropertyChanged(nameof(LogToggleText));
            }
        }

        public string LogToggleText => IsLogVisible ? "Скрыть подробности" : "Показать подробности";

        // Values derived from the coordinator status
        public string InstalledAppVersion => Status.InstalledAppVersion;
        public string LatestVersion => Status.Latest != null ? Status.LatestVersion : "не проверялось";

        public string InstalledServerVersion
        {
            get
            {
                if (!Status.SiteInstalled) return "не установлен";
                return string.IsNullOrWhiteSpace(Status.InstalledServerVersion)
                    ? "не определена"
                    : Status.InstalledServerVersion!;
            }
        }

        public string AppStateText => Status.Latest == null ? "" : Status.AppUpdateAvailable ? "Доступно обновление" : "Актуальная версия";

        public string ServerStateText => !Status.SiteInstalled ? "Не установлен"
            : Status.Latest == null ? ""
            : Status.ServerUpdateAvailable ? "Доступно обновление" : "Актуальная версия";

        public bool AppUpdateAvailable => Status.AppUpdateAvailable;
        public bool ServerUpdateAvailable => Status.ServerUpdateAvailable;
        public bool AnyUpdateAvailable => Status.AnyUpdateAvailable;
        public bool SiteInstalled => Status.SiteInstalled;

        public string SiteHint => Status.SiteInstalled ? ""
            : Status.SiteProblem ?? "Сайт не установлен на этом компьютере";

        public string ReleaseTitle => Status.Latest != null ? $"Что нового в версии {Status.LatestVersion}" : "";

        public string ReleaseNotes
        {
            get
            {
                var text = ReleaseNotesFormatter.ToPlainText(Status.Latest?.Notes);
                return Status.Latest != null && text.Length == 0 && Status.LatestIsComplete
                    ? "Описание изменений не указано."
                    : text;
            }
        }

        public bool HasReleaseInfo => Status.Latest != null;

        private UpdateStatus Status => _coordinator.Status;

        public ICommand CheckCommand { get; }
        public ICommand UpdateAppCommand { get; }
        public ICommand UpdateServerCommand { get; }
        public ICommand UpdateAllCommand { get; }
        public ICommand ToggleLogCommand { get; }
        public ICommand OpenHelpCommand { get; }

        public UpdateViewModel() : this(UpdateCoordinator.Instance, DefaultConfirm, DefaultOpenUrl)
        {
        }

        public UpdateViewModel(UpdateCoordinator coordinator, Func<string, string, bool> confirm, Action<string> openUrl)
        {
            _coordinator = coordinator;
            _confirm = confirm;
            _openUrl = openUrl;
            _dispatcher = System.Windows.Application.Current?.Dispatcher;

            CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => !IsBusy);
            UpdateAppCommand = new RelayCommand(async _ => await UpdateAppAsync(), _ => !IsBusy);
            UpdateServerCommand = new RelayCommand(async _ => await UpdateServerAsync(), _ => !IsBusy && SiteInstalled);
            UpdateAllCommand = new RelayCommand(async _ => await UpdateAllAsync(), _ => !IsBusy && SiteInstalled);
            ToggleLogCommand = new RelayCommand(_ => IsLogVisible = !IsLogVisible);
            OpenHelpCommand = new RelayCommand(_ => _openUrl(HelpUrl), _ => HasHelpUrl);

            if (!coordinator.IsBusy)
                coordinator.RefreshLocalState();

            _isBusy = coordinator.IsBusy;
            if (_isBusy && coordinator.LastProgress is { } running)
            {
                _progress = Math.Clamp(running.Percent, 0, 100);
                _progressStage = running.Stage;
                _progressDetail = running.Detail ?? "";
            }
            foreach (var line in coordinator.GetLog())
                AppendLogLine(line);
            LogText = BuildLogText();

            Attach(coordinator, new WeakReference<UpdateViewModel>(this));
        }

        // Handlers hold the view model weakly: Settings creates a new instance on every navigation.
        private static void Attach(UpdateCoordinator coordinator, WeakReference<UpdateViewModel> weak)
        {
            EventHandler? status = null;
            EventHandler? busy = null;
            EventHandler<UpdateProgress>? progress = null;
            EventHandler<string>? log = null;

            void Detach()
            {
                coordinator.StatusChanged -= status;
                coordinator.BusyChanged -= busy;
                coordinator.ProgressChanged -= progress;
                coordinator.LogAdded -= log;
            }

            status = (_, _) => { if (weak.TryGetTarget(out var vm)) vm.OnStatusChanged(); else Detach(); };
            busy = (_, _) => { if (weak.TryGetTarget(out var vm)) vm.OnBusyChanged(); else Detach(); };
            progress = (_, p) => { if (weak.TryGetTarget(out var vm)) vm.OnProgress(p); else Detach(); };
            log = (_, line) => { if (weak.TryGetTarget(out var vm)) vm.OnLog(line); else Detach(); };

            coordinator.StatusChanged += status;
            coordinator.BusyChanged += busy;
            coordinator.ProgressChanged += progress;
            coordinator.LogAdded += log;
        }

        private static bool DefaultConfirm(string title, string message)
        {
            // Own the confirmation by the window the user is looking at (the update dialog may be open).
            var app = System.Windows.Application.Current;
            var owner = app?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? app?.MainWindow;
            return ConfirmDialog.Show(title, message, ConfirmDialog.DialogType.Question, "Обновить", "Отмена", owner);
        }

        private static void DefaultOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenUrl failed: {ex.Message}");
            }
        }

        /// <summary>Checks once when the window opens, unless a fresh result is already known.</summary>
        public async Task CheckIfNeededAsync()
        {
            if (Status.Latest == null && !IsBusy)
                await CheckAsync();
        }

        public async Task CheckAsync()
        {
            ClearMessages();
            try
            {
                var result = await _coordinator.CheckAsync();
                if (result.Success) StatusMessage = result.Message;
                else ErrorMessage = result.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось проверить обновления: {ex.Message}";
            }
        }

        public async Task UpdateAppAsync()
        {
            var version = Status.Latest != null ? $" до версии {Status.LatestVersion}" : "";
            await RunAsync("Обновление приложения",
                $"Обновить приложение{version}?\n\nПрограмма закроется и сама запустится заново уже с новой версией.",
                () => _coordinator.UpdateAppAsync());
        }

        public async Task UpdateServerAsync()
        {
            var version = Status.Latest != null ? $" до версии {Status.LatestVersion}" : "";
            await RunAsync("Обновление сайта",
                $"Обновить сайт{version}?\n\nСайт будет недоступен 1-2 минуты. Нужны запущенный Docker Desktop и интернет. " +
                "Не закрывайте программу, пока идёт обновление.",
                () => _coordinator.UpdateServerAsync());
        }

        public async Task UpdateAllAsync()
        {
            var version = Status.Latest != null ? $" до версии {Status.LatestVersion}" : "";
            await RunAsync("Обновление всего",
                $"Обновить сайт и приложение{version}?\n\nСначала обновится сайт (несколько минут), затем программа перезапустится. " +
                "Не закрывайте программу, пока идёт обновление.",
                () => _coordinator.UpdateAllAsync());
        }

        private async Task RunAsync(string title, string question, Func<Task<UpdateOperationResult>> action)
        {
            if (IsBusy || !_confirm(title, question))
                return;

            ClearMessages();
            Progress = 0;
            ProgressStage = "Подготовка";
            ProgressDetail = "";

            try
            {
                var result = await action();
                if (result.Success)
                {
                    StatusMessage = result.Message;
                }
                else
                {
                    ErrorMessage = result.Message;
                    HelpUrl = result.HelpUrl ?? "";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
            }
        }

        private void ClearMessages()
        {
            StatusMessage = "";
            ErrorMessage = "";
            HelpUrl = "";
        }

        private void OnStatusChanged() => Ui(() =>
        {
            foreach (var name in new[]
            {
                nameof(InstalledAppVersion), nameof(LatestVersion), nameof(InstalledServerVersion),
                nameof(AppStateText), nameof(ServerStateText), nameof(AppUpdateAvailable),
                nameof(ServerUpdateAvailable), nameof(AnyUpdateAvailable), nameof(SiteInstalled),
                nameof(SiteHint), nameof(ReleaseTitle), nameof(ReleaseNotes), nameof(HasReleaseInfo)
            })
                OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        });

        private void OnBusyChanged() => Ui(() => IsBusy = _coordinator.IsBusy);

        private void OnProgress(UpdateProgress p)
        {
            // Keep only the newest report while the UI thread is busy.
            var queue = Interlocked.Exchange(ref _pendingProgress, p) == null;
            if (queue)
                Ui(() =>
                {
                    var latest = Interlocked.Exchange(ref _pendingProgress, null);
                    if (latest == null) return;
                    Progress = Math.Clamp(latest.Percent, 0, 100);
                    ProgressStage = latest.Stage;
                    ProgressDetail = latest.Detail ?? "";
                });
        }

        private void OnLog(string line)
        {
            _pendingLog.Enqueue(line);
            if (Interlocked.Exchange(ref _logFlushQueued, 1) == 0)
                Ui(() =>
                {
                    Interlocked.Exchange(ref _logFlushQueued, 0);
                    while (_pendingLog.TryDequeue(out var l))
                        AppendLogLine(l);
                    LogText = BuildLogText();
                });
        }

        private void AppendLogLine(string line)
        {
            _logLines.AddLast(line);
            while (_logLines.Count > MaxLogLines)
                _logLines.RemoveFirst();
        }

        private string BuildLogText()
        {
            var sb = new StringBuilder();
            foreach (var l in _logLines)
                sb.AppendLine(l);
            return sb.ToString();
        }

        private void Ui(Action action)
        {
            if (_dispatcher == null || _dispatcher.CheckAccess())
                action();
            else
                _dispatcher.BeginInvoke(action);
        }
    }
}
