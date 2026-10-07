using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using HQStudio.Services;
using HQStudio.Services.Site;

namespace HQStudio.ViewModels
{
    /// <summary>Команда с асинхронным обработчиком: пока он работает, кнопка недоступна.</summary>
    public sealed class SiteAsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _running;

        public SiteAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _running = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SiteAsyncCommand error: {ex}");
            }
            finally
            {
                _running = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Уведомления страницы; в приложении это ToastService, в тестах запись в список.</summary>
    public interface ISiteNotifier
    {
        void Success(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }

    public sealed class ToastSiteNotifier : ISiteNotifier
    {
        public void Success(string message) => ToastService.Instance.ShowSuccess(message);
        public void Info(string message) => ToastService.Instance.ShowInfo(message);
        public void Warning(string message) => ToastService.Instance.ShowWarning(message);
        public void Error(string message) => ToastService.Instance.ShowError(message);
    }

    /// <summary>Окна, которые открывает страница. Реализация живёт в представлении, тесты подставляют заглушку.</summary>
    public interface ISiteDialogs
    {
        void ShowKeys(ISiteService service);
        void ShowLogs(ISiteService service);
        void ShowUpdates();
        bool ConfirmUninstall();
    }

    /// <summary>Запуск окна удаления и закрытие приложения после этого.</summary>
    public interface ISiteUninstallHost
    {
        SiteOperationResult LaunchUninstall();
        void ShutdownApplication();
    }

    public sealed class WindowsSiteUninstallHost : ISiteUninstallHost
    {
        public SiteOperationResult LaunchUninstall() =>
            UninstallLauncher.CreateDefault().Launch(Environment.ProcessPath ?? "");

        public void ShutdownApplication() => System.Windows.Application.Current?.Shutdown();
    }

    public sealed class SiteViewModel : BaseViewModel
    {
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);

        private readonly ISiteService _service;
        private readonly ISiteShell _shell;
        private readonly ISiteDialogs _dialogs;
        private readonly ISiteUninstallHost _uninstallHost;
        private readonly ISiteNotifier _notifier;
        private readonly SynchronizationContext? _context = SynchronizationContext.Current;

        private DispatcherTimer? _timer;
        private CancellationTokenSource? _lifetimeCts;
        private CancellationTokenSource? _operationCts;
        private bool _refreshing;
        private bool _refreshQueued;
        private SiteOperation _operation = SiteOperation.None;

        private SiteSnapshot _snapshot = SiteSnapshot.Checking;
        private IReadOnlyList<ServiceStatus> _services = SiteStatusEvaluator.PlaceholderServices();
        private bool _isBusy;
        private string _busyText = "";
        private bool _canCancel;
        private string _errorTitle = "";
        private string _errorMessage = "";
        private string _errorDetails = "";

        public SiteViewModel()
            : this(SiteManager.CreateDefault(), new WindowsSiteShell(), new Views.WpfSiteDialogs(),
                new WindowsSiteUninstallHost(), new ToastSiteNotifier())
        {
        }

        public SiteViewModel(ISiteService service, ISiteShell shell, ISiteDialogs dialogs,
            ISiteUninstallHost uninstallHost, ISiteNotifier notifier)
        {
            _service = service;
            _shell = shell;
            _dialogs = dialogs;
            _uninstallHost = uninstallHost;
            _notifier = notifier;

            StartCommand = new SiteAsyncCommand(StartSiteAsync, () => CanStart);
            StopCommand = new SiteAsyncCommand(StopSiteAsync, () => CanStop);
            RestartCommand = new SiteAsyncCommand(RestartSiteAsync, () => CanRestart);
            StartDockerCommand = new SiteAsyncCommand(StartDockerAsync, () => CanStartDocker);
            CancelCommand = new RelayCommand(_ => CancelOperation(), _ => CanCancel);
            OpenSiteCommand = new RelayCommand(_ => OpenLocal(), _ => CanOpenSite);
            CopyLocalCommand = new RelayCommand(_ => CopyText(LocalUrl, "Адрес скопирован"), _ => HasLocalUrl);
            OpenLocalCommand = new RelayCommand(_ => OpenLocal(), _ => HasLocalUrl);
            CopyPublicCommand = new RelayCommand(_ => CopyText(PublicUrl ?? "", "Адрес скопирован"), _ => HasPublicUrl);
            OpenPublicCommand = new RelayCommand(_ => OpenUrl(PublicUrl), _ => HasPublicUrl);
            KeysCommand = new RelayCommand(_ => ShowKeys(), _ => IsInstalled && !IsBusy);
            LogsCommand = new RelayCommand(_ => _dialogs.ShowLogs(_service), _ => IsInstalled);
            UpdatesCommand = new RelayCommand(_ => ShowUpdates());
            UninstallCommand = new SiteAsyncCommand(UninstallAsync, () => !IsBusy);
        }

        public ISiteService Service => _service;

        // ------------------------------------------------------------------ состояние страницы

        public SitePill Pill => _snapshot.Overview.Pill;
        public string PillText => _snapshot.Overview.PillText;
        public string Explanation => _snapshot.Overview.Explanation;
        public bool IsInstalled => _snapshot.Installed;
        public bool ShowNotInstalled => Pill == SitePill.NotInstalled;
        public bool ShowSiteContent => IsInstalled;
        public bool ShowStartDockerBanner => Pill == SitePill.DockerDown && _snapshot.Docker == SiteDockerState.NotRunning;
        public string Version => string.IsNullOrWhiteSpace(_snapshot.Version) ? "не указана" : _snapshot.Version;
        public string LocalUrl => _snapshot.LocalUrl;
        public bool HasLocalUrl => _snapshot.LocalUrl.Length > 0;
        public string? PublicUrl => _snapshot.PublicUrl;
        public bool HasPublicUrl => !string.IsNullOrEmpty(_snapshot.PublicUrl);
        public string PublicUrlText => HasPublicUrl
            ? _snapshot.PublicUrl!
            : _snapshot.TunnelConfigured ? "Адрес ещё не получен" : "Не настроен";

        public IReadOnlyList<ServiceStatus> Services => _services;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RaiseAvailability();
            }
        }

        public string BusyText
        {
            get => _busyText;
            private set => SetProperty(ref _busyText, value);
        }

        public bool CanCancel
        {
            get => _canCancel;
            private set => SetProperty(ref _canCancel, value);
        }

        public string ErrorTitle
        {
            get => _errorTitle;
            private set => SetProperty(ref _errorTitle, value);
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

        public string ErrorDetails
        {
            get => _errorDetails;
            private set
            {
                if (SetProperty(ref _errorDetails, value))
                    OnPropertyChanged(nameof(HasErrorDetails));
            }
        }

        public bool HasError => _errorMessage.Length > 0;
        public bool HasErrorDetails => _errorDetails.Length > 0;

        public bool CanStart => IsInstalled && !IsBusy && Pill is not (SitePill.Running or SitePill.Stopping or SitePill.NotInstalled);
        public bool CanStop => IsInstalled && !IsBusy && _snapshot.Docker == SiteDockerState.Running && Pill is not (SitePill.Stopped or SitePill.NotInstalled);
        public bool CanRestart => IsInstalled && !IsBusy && _snapshot.Docker == SiteDockerState.Running && Pill is SitePill.Running or SitePill.Error or SitePill.Starting;
        public bool CanStartDocker => IsInstalled && !IsBusy && _snapshot.Docker == SiteDockerState.NotRunning;
        public bool CanOpenSite => IsInstalled && HasLocalUrl && Pill == SitePill.Running;

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand StartDockerCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenSiteCommand { get; }
        public ICommand CopyLocalCommand { get; }
        public ICommand OpenLocalCommand { get; }
        public ICommand CopyPublicCommand { get; }
        public ICommand OpenPublicCommand { get; }
        public ICommand KeysCommand { get; }
        public ICommand LogsCommand { get; }
        public ICommand UpdatesCommand { get; }
        public ICommand UninstallCommand { get; }

        // ------------------------------------------------------------------ автообновление

        /// <summary>Вызывается, когда страница стала видимой: запускает опрос раз в 5 секунд.</summary>
        public void Activate()
        {
            if (_timer != null)
                return;

            _lifetimeCts = new CancellationTokenSource();
            _timer = new DispatcherTimer { Interval = RefreshInterval };
            _timer.Tick += (_, _) => _ = RefreshAsync();
            _timer.Start();
            _ = RefreshAsync();
        }

        /// <summary>Вызывается, когда страница скрыта: таймер выключается, идущий опрос отменяется.</summary>
        public void Deactivate()
        {
            _timer?.Stop();
            _timer = null;
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        public bool IsActive => _timer != null;

        /// <summary>Один опрос за раз: пока идёт предыдущий, новый запоминается и выполняется сразу после него.</summary>
        public async Task RefreshAsync()
        {
            if (_refreshing)
            {
                _refreshQueued = true;
                return;
            }

            _refreshing = true;
            try
            {
                do
                {
                    _refreshQueued = false;
                    var token = _lifetimeCts?.Token ?? CancellationToken.None;
                    var operation = _operation;
                    try
                    {
                        var snapshot = await Task.Run(() => _service.RefreshAsync(operation, token), token);
                        if (!token.IsCancellationRequested)
                            Apply(snapshot);
                    }
                    catch (OperationCanceledException)
                    {
                        _refreshQueued = false;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Site refresh error: {ex.Message}");
                    }
                }
                while (_refreshQueued);
            }
            finally
            {
                _refreshing = false;
            }
        }

        /// <summary>Применяет снимок состояния; открыт для предпросмотра и тестов.</summary>
        public void Apply(SiteSnapshot snapshot)
        {
            _snapshot = snapshot;
            if (!_services.SequenceEqual(snapshot.Services))
            {
                _services = snapshot.Services;
                OnPropertyChanged(nameof(Services));
            }

            foreach (var name in new[]
                     {
                         nameof(Pill), nameof(PillText), nameof(Explanation), nameof(IsInstalled), nameof(ShowNotInstalled),
                         nameof(ShowSiteContent), nameof(ShowStartDockerBanner), nameof(Version), nameof(LocalUrl),
                         nameof(HasLocalUrl), nameof(PublicUrl), nameof(HasPublicUrl), nameof(PublicUrlText)
                     })
                OnPropertyChanged(name);
            RaiseAvailability();
        }

        /// <summary>Выставляет состояние страницы целиком: для предпросмотра и тестов, без обращения к Docker.</summary>
        public void ShowPreview(SiteSnapshot snapshot, string? busyText = null, bool canCancel = false,
            SiteFailure? failure = null)
        {
            Apply(snapshot);
            if (busyText != null)
            {
                BusyText = busyText;
                CanCancel = canCancel;
                IsBusy = true;
            }
            if (failure != null)
            {
                ErrorTitle = failure.Title;
                ErrorMessage = failure.Message;
                ErrorDetails = failure.Details;
            }
        }

        // ------------------------------------------------------------------ действия

        public Task StartSiteAsync() => RunOperationAsync(SiteOperation.Starting, "Запускаю сайт", _service.StartAsync);

        public Task StopSiteAsync() => RunOperationAsync(SiteOperation.Stopping, "Останавливаю сайт", _service.StopAsync);

        public Task RestartSiteAsync() => RunOperationAsync(SiteOperation.Restarting, "Перезапускаю сайт", _service.RestartAsync);

        public Task StartDockerAsync() => RunOperationAsync(SiteOperation.None, "Запускаю Docker", _service.StartDockerAsync);

        private async Task RunOperationAsync(SiteOperation operation, string initialText,
            Func<Action<string>?, CancellationToken, Task<SiteOperationResult>> action)
        {
            if (IsBusy)
                return;

            ClearError();
            _operation = operation;
            _operationCts = new CancellationTokenSource();
            BusyText = initialText;
            CanCancel = true;
            IsBusy = true;
            _ = RefreshAsync();

            SiteOperationResult result;
            try
            {
                var token = _operationCts.Token;
                result = await Task.Run(() => action(text => Post(() => BusyText = text), token), token);
            }
            catch (OperationCanceledException)
            {
                result = SiteOperationResult.Fail(SiteErrorMapper.FromException(initialText, new OperationCanceledException()));
            }
            finally
            {
                _operation = SiteOperation.None;
                _operationCts?.Dispose();
                _operationCts = null;
                CanCancel = false;
                IsBusy = false;
            }

            ShowResult(result);
            await RefreshAsync();
        }

        private void ShowResult(SiteOperationResult result)
        {
            if (result.Success)
            {
                _notifier.Success(result.Message);
                return;
            }

            var failure = result.Failure;
            if (failure is { Kind: SiteFailureKind.Cancelled })
            {
                _notifier.Info(failure.Message);
                return;
            }

            ErrorTitle = failure?.Title ?? "Не получилось";
            ErrorMessage = result.Message;
            ErrorDetails = failure?.Details ?? "";
            _notifier.Error(failure?.Title ?? result.Message);
        }

        private void ClearError()
        {
            ErrorTitle = "";
            ErrorMessage = "";
            ErrorDetails = "";
        }

        public void DismissError() => ClearError();

        private void CancelOperation()
        {
            try
            {
                _operationCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Операция уже закончилась.
            }
        }

        private void OpenLocal() => OpenUrl(LocalUrl);

        private void OpenUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            if (!_shell.OpenUrl(url))
                _notifier.Warning("Не получилось открыть браузер. Скопируйте адрес и вставьте его в браузер вручную.");
        }

        private void CopyText(string text, string done)
        {
            if (text.Length == 0)
                return;
            if (_shell.CopyText(text))
                _notifier.Success(done);
            else
                _notifier.Warning("Не получилось скопировать. Попробуйте ещё раз.");
        }

        private void ShowKeys()
        {
            _dialogs.ShowKeys(_service);
            _ = RefreshAsync();
        }

        private void ShowUpdates()
        {
            _dialogs.ShowUpdates();
            _ = RefreshAsync();
        }

        private async Task UninstallAsync()
        {
            if (!_dialogs.ConfirmUninstall())
                return;

            // Копирование программы во временную папку занимает секунды: делаем это не в потоке окна.
            ClearError();
            BusyText = "Готовлю удаление";
            CanCancel = false;
            IsBusy = true;

            SiteOperationResult result;
            try
            {
                result = await Task.Run(() => _uninstallHost.LaunchUninstall());
            }
            finally
            {
                IsBusy = false;
            }

            if (result.Success)
            {
                _uninstallHost.ShutdownApplication();
                return;
            }

            ErrorTitle = result.Failure?.Title ?? "Не получилось";
            ErrorMessage = result.Message;
            ErrorDetails = result.Failure?.Details ?? "";
            _notifier.Error(ErrorTitle);
        }

        // ------------------------------------------------------------------ служебное

        private void RaiseAvailability()
        {
            foreach (var name in new[]
                     {
                         nameof(CanStart), nameof(CanStop), nameof(CanRestart), nameof(CanStartDocker), nameof(CanOpenSite)
                     })
                OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        }

        private void Post(Action action)
        {
            if (_context != null)
                _context.Post(_ => action(), null);
            else
                action();
        }
    }

    public enum SiteSecretMode
    {
        Keep,
        Edit,
        Clear
    }

    /// <summary>Окно «Ключи»: Gemini, токен Tuna и имя адреса. Сохранённые значения не показываются.</summary>
    public sealed class SiteKeysViewModel : BaseViewModel
    {
        public const string GeminiHelpUrl = "https://aistudio.google.com/apikey";
        public const string TunaHelpUrl = "https://tuna.am";
        public const string TunaDomainsUrl = "https://my.tuna.am/domains";
        public const string DomainHelpUrl = "https://tuna.am/docs/tunnels/guides/connect-self-domain";
        public const string PunycodeUrl = "https://www.reg.ru/web-tools/punycode";

        private readonly ISiteService _service;
        private readonly ISiteShell _shell;
        private readonly SynchronizationContext? _context = SynchronizationContext.Current;
        private CancellationTokenSource? _cts;

        private bool _hasGemini;
        private bool _hasToken;
        private string _originalSubdomain = "";
        private string _originalDomain = "";
        private SiteSecretMode _geminiMode;
        private SiteSecretMode _tunaMode;
        private string _geminiInput = "";
        private string _tunaInput = "";
        private string _subdomain = "";
        private string _domain = "";
        private bool _isBusy;
        private string _statusText = "";
        private string _resultMessage = "";
        private bool _resultIsError;
        private string _errorDetails = "";
        private bool _completed;

        public SiteKeysViewModel(ISiteService service, ISiteShell shell)
        {
            _service = service;
            _shell = shell;

            ChangeGeminiCommand = new RelayCommand(_ => GeminiMode = SiteSecretMode.Edit, _ => !IsBusy);
            ClearGeminiCommand = new RelayCommand(_ => GeminiMode = SiteSecretMode.Clear, _ => !IsBusy);
            KeepGeminiCommand = new RelayCommand(_ => { GeminiInput = ""; GeminiMode = SiteSecretMode.Keep; }, _ => !IsBusy);
            ChangeTunaCommand = new RelayCommand(_ => TunaMode = SiteSecretMode.Edit, _ => !IsBusy);
            ClearTunaCommand = new RelayCommand(_ => TunaMode = SiteSecretMode.Clear, _ => !IsBusy);
            KeepTunaCommand = new RelayCommand(_ => { TunaInput = ""; TunaMode = SiteSecretMode.Keep; }, _ => !IsBusy);
            OpenGeminiHelpCommand = new RelayCommand(_ => _shell.OpenUrl(GeminiHelpUrl));
            OpenTunaHelpCommand = new RelayCommand(_ => _shell.OpenUrl(TunaHelpUrl));
            OpenTunaDomainsCommand = new RelayCommand(_ => _shell.OpenUrl(TunaDomainsUrl));
            OpenDomainHelpCommand = new RelayCommand(_ => _shell.OpenUrl(DomainHelpUrl));
            OpenPunycodeCommand = new RelayCommand(_ => _shell.OpenUrl(PunycodeUrl));
            SaveCommand = new SiteAsyncCommand(SaveAsync, () => CanSave);

            Load();
        }

        public void Load()
        {
            var state = _service.ReadKeysState();
            _hasGemini = state?.HasGeminiKey ?? false;
            _hasToken = state?.HasTunaToken ?? false;
            _originalSubdomain = state?.TunaSubdomain ?? "";
            _originalDomain = state?.TunaDomain ?? "";
            _geminiMode = _hasGemini ? SiteSecretMode.Keep : SiteSecretMode.Edit;
            _tunaMode = _hasToken ? SiteSecretMode.Keep : SiteSecretMode.Edit;
            _geminiInput = "";
            _tunaInput = "";
            _subdomain = _originalSubdomain;
            _domain = _originalDomain;
            RaiseAll();
        }

        public bool HasGemini => _hasGemini;
        public bool HasToken => _hasToken;

        public SiteSecretMode GeminiMode
        {
            get => _geminiMode;
            private set
            {
                if (SetProperty(ref _geminiMode, value))
                    RaiseState();
            }
        }

        public SiteSecretMode TunaMode
        {
            get => _tunaMode;
            private set
            {
                if (SetProperty(ref _tunaMode, value))
                    RaiseState();
            }
        }

        public bool ShowGeminiSaved => _hasGemini && _geminiMode == SiteSecretMode.Keep;
        public bool ShowGeminiInput => _geminiMode == SiteSecretMode.Edit;
        public bool ShowGeminiCleared => _geminiMode == SiteSecretMode.Clear;
        public bool CanCancelGeminiEdit => _hasGemini && _geminiMode == SiteSecretMode.Edit;
        public bool ShowTunaSaved => _hasToken && _tunaMode == SiteSecretMode.Keep;
        public bool ShowTunaInput => _tunaMode == SiteSecretMode.Edit;
        public bool ShowTunaCleared => _tunaMode == SiteSecretMode.Clear;
        public bool CanCancelTunaEdit => _hasToken && _tunaMode == SiteSecretMode.Edit;

        public string GeminiInput
        {
            get => _geminiInput;
            set
            {
                if (SetProperty(ref _geminiInput, value))
                    RaiseState();
            }
        }

        public string TunaInput
        {
            get => _tunaInput;
            set
            {
                if (SetProperty(ref _tunaInput, value))
                    RaiseState();
            }
        }

        public string Subdomain
        {
            get => _subdomain;
            set
            {
                if (SetProperty(ref _subdomain, value))
                    RaiseState();
            }
        }

        public string Domain
        {
            get => _domain;
            set
            {
                if (SetProperty(ref _domain, value))
                    RaiseState();
            }
        }

        /// <summary>Пока указан свой домен, имя адреса Tuna не используется и поле недоступно.</summary>
        public bool SubdomainEnabled => _domain.Trim().Length == 0;

        public string SubdomainError
        {
            get
            {
                if (!SubdomainEnabled)
                    return "";
                var value = _subdomain.Trim();
                return SiteEnvFile.IsValidSubdomain(value)
                    ? ""
                    : "Можно использовать только латинские буквы в нижнем регистре, цифры и дефис.";
            }
        }

        public string DomainError => SiteEnvFile.CheckDomain(_domain.Trim()) switch
        {
            DomainCheck.NonAscii => "Домен нужно записать латиницей (punycode). Перевести его можно по кнопке ниже.",
            DomainCheck.Invalid => "Запишите домен так: crm.example.ru. Только строчные латинские буквы, цифры, дефис и точки, без http:// и без пути.",
            _ => ""
        };

        public bool ShowPunycodeLink => SiteEnvFile.CheckDomain(_domain.Trim()) == DomainCheck.NonAscii;

        public string GeminiInputError => _geminiMode == SiteSecretMode.Edit && !SiteEnvFile.IsSafeValue(_geminiInput.Trim())
            ? "В ключе не должно быть пробелов и необычных символов."
            : "";

        public string TunaInputError => _tunaMode == SiteSecretMode.Edit && !SiteEnvFile.IsSafeValue(_tunaInput.Trim())
            ? "В токене не должно быть пробелов и необычных символов."
            : "";

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RaiseState();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string ResultMessage
        {
            get => _resultMessage;
            private set
            {
                if (SetProperty(ref _resultMessage, value))
                    OnPropertyChanged(nameof(HasResult));
            }
        }

        public bool ResultIsError
        {
            get => _resultIsError;
            private set => SetProperty(ref _resultIsError, value);
        }

        public string ErrorDetails
        {
            get => _errorDetails;
            private set
            {
                if (SetProperty(ref _errorDetails, value))
                    OnPropertyChanged(nameof(HasErrorDetails));
            }
        }

        public bool HasResult => _resultMessage.Length > 0;
        public bool HasErrorDetails => _errorDetails.Length > 0;

        /// <summary>Хотя бы раз сохранили успешно: окно, закрываясь, сообщает странице обновить состояние.</summary>
        public bool Completed
        {
            get => _completed;
            private set => SetProperty(ref _completed, value);
        }

        public bool HasChanges => BuildUpdate().IsEmpty == false;

        public bool CanSave => !IsBusy && HasChanges && SubdomainError.Length == 0 && DomainError.Length == 0 &&
                               GeminiInputError.Length == 0 && TunaInputError.Length == 0;

        public ICommand ChangeGeminiCommand { get; }
        public ICommand ClearGeminiCommand { get; }
        public ICommand KeepGeminiCommand { get; }
        public ICommand ChangeTunaCommand { get; }
        public ICommand ClearTunaCommand { get; }
        public ICommand KeepTunaCommand { get; }
        public ICommand OpenGeminiHelpCommand { get; }
        public ICommand OpenTunaHelpCommand { get; }
        public ICommand OpenTunaDomainsCommand { get; }
        public ICommand OpenDomainHelpCommand { get; }
        public ICommand OpenPunycodeCommand { get; }
        public ICommand SaveCommand { get; }

        public SiteKeysUpdate BuildUpdate()
        {
            string? gemini = _geminiMode switch
            {
                SiteSecretMode.Clear => "",
                SiteSecretMode.Edit when _geminiInput.Trim().Length > 0 => _geminiInput.Trim(),
                _ => null
            };
            string? token = _tunaMode switch
            {
                SiteSecretMode.Clear => "",
                SiteSecretMode.Edit when _tunaInput.Trim().Length > 0 => _tunaInput.Trim(),
                _ => null
            };
            var dom = _domain.Trim();
            string? domain;
            string? subdomain;
            if (dom.Length > 0)
            {
                // При домене имя Tuna не меняем: сохранение само запишет его пустым.
                domain = dom != _originalDomain ? dom : null;
                subdomain = null;
            }
            else
            {
                domain = _originalDomain.Length > 0 ? "" : null;
                var sub = _subdomain.Trim();
                subdomain = sub != _originalSubdomain ? sub : null;
            }
            return new SiteKeysUpdate(gemini, token, subdomain, domain);
        }

        public void Cancel()
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Сохранение уже закончилось.
            }
        }

        public async Task SaveAsync()
        {
            if (!CanSave)
                return;

            ResultMessage = "";
            ErrorDetails = "";
            ResultIsError = false;
            StatusText = "Сохраняю настройки";
            IsBusy = true;
            _cts = new CancellationTokenSource();

            SiteOperationResult result;
            try
            {
                var update = BuildUpdate();
                var token = _cts.Token;
                result = await Task.Run(() => _service.ApplyKeysAsync(update, text => Post(() => StatusText = text), token), token);
            }
            catch (OperationCanceledException)
            {
                result = SiteOperationResult.Fail(SiteErrorMapper.FromException("Сохранение", new OperationCanceledException()));
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                IsBusy = false;
                StatusText = "";
            }

            ResultIsError = !result.Success;
            ResultMessage = result.Message;
            ErrorDetails = result.Failure?.Details ?? "";

            // Настройки на диске могли измениться и при частичной неудаче (сохранили, но не применили).
            if (result.Success || result.ConfigSaved)
            {
                Completed = true;
                Load();
            }
        }

        private void RaiseAll()
        {
            foreach (var name in new[]
                     {
                         nameof(HasGemini), nameof(HasToken), nameof(GeminiMode), nameof(TunaMode), nameof(GeminiInput),
                         nameof(TunaInput), nameof(Subdomain), nameof(Domain)
                     })
                OnPropertyChanged(name);
            RaiseState();
        }

        private void RaiseState()
        {
            foreach (var name in new[]
                     {
                         nameof(ShowGeminiSaved), nameof(ShowGeminiInput), nameof(ShowGeminiCleared), nameof(CanCancelGeminiEdit),
                         nameof(ShowTunaSaved), nameof(ShowTunaInput), nameof(ShowTunaCleared), nameof(CanCancelTunaEdit),
                         nameof(SubdomainError), nameof(SubdomainEnabled), nameof(DomainError), nameof(ShowPunycodeLink), nameof(GeminiInputError), nameof(TunaInputError), nameof(HasChanges), nameof(CanSave)
                     })
                OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        }

        private void Post(Action action)
        {
            if (_context != null)
                _context.Post(_ => action(), null);
            else
                action();
        }
    }

    /// <summary>Окно «Логи»: последние 300 строк выбранной службы с замазанными секретами.</summary>
    public sealed class SiteLogsViewModel : BaseViewModel
    {
        private readonly ISiteService _service;
        private readonly ISiteShell _shell;
        private readonly ISiteNotifier _notifier;
        private CancellationTokenSource? _cts;

        private string _selectedService = SiteServiceIds.Api;
        private string _logText = "";
        private bool _isLoading;
        private string _statusText = "";
        private string _errorMessage = "";

        public SiteLogsViewModel(ISiteService service, ISiteShell shell, ISiteNotifier notifier)
        {
            _service = service;
            _shell = shell;
            _notifier = notifier;

            foreach (var id in SiteServiceIds.All)
                Services.Add(new SiteLogChoice(id, SiteServiceIds.Title(id)) { IsSelected = id == _selectedService });

            RefreshCommand = new SiteAsyncCommand(RefreshAsync, () => !IsLoading);
            CopyCommand = new RelayCommand(_ => Copy(), _ => _logText.Length > 0);
            SelectCommand = new RelayCommand(p =>
            {
                if (p is string id)
                    SelectedService = id;
            });
        }

        public ObservableCollection<SiteLogChoice> Services { get; } = new();

        public string SelectedService
        {
            get => _selectedService;
            set
            {
                if (!SetProperty(ref _selectedService, value))
                    return;

                foreach (var choice in Services)
                    choice.IsSelected = choice.Id == value;
                _ = RefreshAsync();
            }
        }

        public string LogText
        {
            get => _logText;
            private set
            {
                if (SetProperty(ref _logText, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
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

        public bool HasError => _errorMessage.Length > 0;

        public ICommand RefreshCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand SelectCommand { get; }

        /// <summary>Новый запрос отменяет прежний: быстрое переключение служб не накапливает очередь.</summary>
        public async Task RefreshAsync()
        {
            _cts?.Cancel();
            var cts = new CancellationTokenSource();
            _cts = cts;
            var service = _selectedService;

            IsLoading = true;
            ErrorMessage = "";
            StatusText = "Загружаю журнал";
            try
            {
                var result = await Task.Run(() => _service.GetLogsAsync(service, cts.Token), cts.Token);
                if (cts.IsCancellationRequested)
                    return;

                if (result.Success)
                {
                    LogText = result.Text;
                    var lines = result.Text.Split('\n').Length;
                    StatusText = $"Строк: {lines}. Обновлено в {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    LogText = "";
                    ErrorMessage = result.Failure?.Message ?? "Не удалось прочитать журнал.";
                    StatusText = "";
                }
            }
            catch (OperationCanceledException)
            {
                // Выбрана другая служба или окно закрыто.
            }
            finally
            {
                if (ReferenceEquals(_cts, cts))
                {
                    IsLoading = false;
                    _cts = null;
                }
                cts.Dispose();
            }
        }

        public void Close()
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Запрос уже закончился.
            }
        }

        private void Copy()
        {
            if (_shell.CopyText(_logText))
                _notifier.Success("Журнал скопирован");
            else
                _notifier.Warning("Не получилось скопировать. Попробуйте ещё раз.");
        }
    }

    public sealed class SiteLogChoice : BaseViewModel
    {
        private bool _isSelected;

        public SiteLogChoice(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public string Id { get; }
        public string Title { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
