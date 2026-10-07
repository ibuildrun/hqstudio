using System.Windows.Input;
using HQStudio.Services;
using HQStudio.Services.BugReport;

namespace HQStudio.ViewModels
{
    public enum BugReportStage
    {
        Form,
        Review,
        SigningIn,
        Sending,
        Success,
        Error
    }

    public class BugReportViewModel : BaseViewModel
    {
        public const int MaxDescriptionLength = 5000;

        private readonly Exception? _crash;
        private readonly BugReportService _service;
        private readonly DiagnosticsCollector _collector;
        private readonly IShellActions _shell;
        private readonly Func<string?> _apiUrlProvider;
        private readonly Dictionary<bool, DiagnosticsSnapshot> _snapshots = new();

        private CancellationTokenSource? _cts;
        private BugReportDraft? _draft;
        private bool _closing;
        private int _attempt;

        private BugReportStage _stage = BugReportStage.Form;
        private string _title = "";
        private string _description = "";
        private bool _includeDiagnostics = true;
        private bool _isBusy;
        private string _validationMessage = "";
        private string _previewTitle = "";
        private string _previewBody = "";
        private string _userCode = "";
        private string _verificationUri = BugReportConfig.DeviceActivationUrl;
        private string _statusText = "";
        private string _copyHint = "";
        private string _issueUrl = "";
        private string _successTitle = "Готово!";
        private string _successMessage = "";
        private string _errorMessage = "";
        private string _errorDetails = "";

        public BugReportViewModel(Exception? crash = null, BugReportService? service = null,
            DiagnosticsCollector? collector = null, IShellActions? shell = null, Func<string?>? apiUrlProvider = null)
        {
            _crash = crash;
            _service = service ?? BugReportService.CreateDefault();
            _collector = collector ?? new DiagnosticsCollector();
            _shell = shell ?? new SystemShellActions();
            _apiUrlProvider = apiUrlProvider ?? (() => SettingsService.Instance.ApiUrl);

            if (crash != null)
                _title = BugReportDraftBuilder.BuildCrashTitle(crash);

            NextCommand = new RelayCommand(_ => _ = GoToReviewAsync(), _ => !IsBusy);
            BackCommand = new RelayCommand(_ => GoBack());
            SendCommand = new RelayCommand(_ => _ = SendViaGitHubAsync());
            SendViaBrowserCommand = new RelayCommand(_ => SendViaBrowser());
            RetryCommand = new RelayCommand(_ => _ = SendViaGitHubAsync());
            CancelSendCommand = new RelayCommand(_ => CancelSend());
            CopyCodeCommand = new RelayCommand(_ => CopyCode());
            OpenVerificationCommand = new RelayCommand(_ => OpenVerification());
            OpenIssueCommand = new RelayCommand(_ => OpenIssue());
            CancelCommand = new RelayCommand(_ => RequestClose());
        }

        public event Action<bool>? CloseRequested;

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand SendViaBrowserCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand CancelSendCommand { get; }
        public ICommand CopyCodeCommand { get; }
        public ICommand OpenVerificationCommand { get; }
        public ICommand OpenIssueCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsCrashReport => _crash != null;
        public bool IsAutomaticAvailable => _service.IsAutomaticModeAvailable;

        /// <summary>True, если отчёт ушёл (issue создан или открыт браузер).</summary>
        public bool Succeeded { get; private set; }

        public BugReportStage Stage
        {
            get => _stage;
            private set
            {
                if (!SetProperty(ref _stage, value)) return;
                OnPropertyChanged(nameof(IsFormStage));
                OnPropertyChanged(nameof(IsReviewStage));
                OnPropertyChanged(nameof(IsSigningInStage));
                OnPropertyChanged(nameof(IsSendingStage));
                OnPropertyChanged(nameof(IsSuccessStage));
                OnPropertyChanged(nameof(IsErrorStage));
                OnPropertyChanged(nameof(StepTitle));
            }
        }

        public bool IsFormStage => Stage == BugReportStage.Form;
        public bool IsReviewStage => Stage == BugReportStage.Review;
        public bool IsSigningInStage => Stage == BugReportStage.SigningIn;
        public bool IsSendingStage => Stage == BugReportStage.Sending;
        public bool IsSuccessStage => Stage == BugReportStage.Success;
        public bool IsErrorStage => Stage == BugReportStage.Error;

        public string StepTitle => Stage switch
        {
            BugReportStage.Form => "Шаг 1: опишите проблему",
            BugReportStage.Review => "Шаг 2: проверьте, что будет отправлено",
            BugReportStage.SigningIn => "Шаг 3: войдите в GitHub",
            BugReportStage.Sending => "Отправляем отчёт",
            BugReportStage.Success => "Готово!",
            _ => "Не получилось отправить"
        };

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? "");
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? "");
        }

        public bool IncludeDiagnostics
        {
            get => _includeDiagnostics;
            set => SetProperty(ref _includeDiagnostics, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        public string PreviewTitle
        {
            get => _previewTitle;
            private set => SetProperty(ref _previewTitle, value);
        }

        public string PreviewBody
        {
            get => _previewBody;
            private set => SetProperty(ref _previewBody, value);
        }

        public string SendModeHint => IsAutomaticAvailable
            ? "Если вы уже входили в GitHub, отчёт уйдёт сразу. Иначе мы попросим войти (или зарегистрироваться - это бесплатно)."
            : "Мы откроем страницу GitHub с уже заполненным текстом. Вам останется войти в аккаунт (или зарегистрироваться - это бесплатно) и нажать кнопку «Submit new issue».";

        public string UserCode
        {
            get => _userCode;
            private set => SetProperty(ref _userCode, value);
        }

        public string VerificationUri
        {
            get => _verificationUri;
            private set => SetProperty(ref _verificationUri, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string CopyHint
        {
            get => _copyHint;
            private set => SetProperty(ref _copyHint, value);
        }

        public string IssueUrl
        {
            get => _issueUrl;
            private set
            {
                if (SetProperty(ref _issueUrl, value))
                    OnPropertyChanged(nameof(IsIssueLinkVisible));
            }
        }

        public bool IsIssueLinkVisible => !string.IsNullOrEmpty(IssueUrl);

        public string SuccessTitle
        {
            get => _successTitle;
            private set => SetProperty(ref _successTitle, value);
        }

        public string SuccessMessage
        {
            get => _successMessage;
            private set => SetProperty(ref _successMessage, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string ErrorDetails
        {
            get => _errorDetails;
            private set => SetProperty(ref _errorDetails, value);
        }

        public async Task GoToReviewAsync()
        {
            ValidationMessage = "";
            if (!IsCrashReport && string.IsNullOrWhiteSpace(Description))
            {
                ValidationMessage = "Напишите, что случилось, хотя бы в двух словах.";
                return;
            }

            IsBusy = true;
            try
            {
                var snapshot = await GetSnapshotAsync(IncludeDiagnostics);
                _draft = BugReportDraftBuilder.Build(Title, Description, snapshot, IncludeDiagnostics, _crash);
                PreviewTitle = _draft.Title;
                PreviewBody = _draft.Body;
                Stage = BugReportStage.Review;
            }
            catch (Exception ex)
            {
                ValidationMessage = "Не удалось подготовить отчёт: " + DiagnosticsSanitizer.Sanitize(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<DiagnosticsSnapshot> GetSnapshotAsync(bool includeLogs)
        {
            if (_snapshots.TryGetValue(includeLogs, out var cached)) return cached;

            string? apiUrl = null;
            try { apiUrl = _apiUrlProvider(); } catch { }

            var snapshot = await _collector.CollectAsync(_crash, includeLogs, apiUrl, serverVersion: null);
            _snapshots[includeLogs] = snapshot;
            return snapshot;
        }

        private void GoBack()
        {
            CancelPending();
            Stage = BugReportStage.Form;
        }

        private void CancelSend()
        {
            CancelPending();
            Stage = BugReportStage.Review;
        }

        public async Task SendViaGitHubAsync()
        {
            var draft = _draft;
            if (draft == null) return;

            CancelPending();
            var attempt = _attempt;
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            StatusText = "Проверяем вход в GitHub...";
            CopyHint = "";
            Stage = BugReportStage.Sending;

            try
            {
                var issue = await _service.SubmitAsync(
                    draft, new ImmediateProgress(p => OnProgress(attempt, p)), ct);

                // Отчёт ушёл, даже если пользователь успел нажать "Назад": показываем итог в любом случае.
                _attempt++;
                IssueUrl = issue.HtmlUrl;
                SuccessTitle = "Готово!";
                SuccessMessage = "Спасибо! Ваше обращение отправлено разработчикам. Вот ссылка на него:";
                Succeeded = true;
                if (!_closing) Stage = BugReportStage.Success;
            }
            catch (OperationCanceledException)
            {
                if (attempt == _attempt && !_closing) Stage = BugReportStage.Review;
            }
            catch (Exception ex)
            {
                if (attempt != _attempt) return;
                _attempt++;
                if (_closing) return;

                ShowError(DescribeError(ex as BugReportException), ex.Message);
            }
        }

        // attempt отсекает запоздавшие уведомления отменённой или уже завершённой отправки.
        private void OnProgress(int attempt, BugReportProgress progress)
        {
            if (_closing || attempt != _attempt) return;

            switch (progress.State)
            {
                case BugReportState.RequestingCode:
                    StatusText = "Связываемся с GitHub...";
                    Stage = BugReportStage.Sending;
                    break;
                case BugReportState.WaitingForUser when progress.DeviceCode != null:
                    UserCode = progress.DeviceCode.UserCode;
                    VerificationUri = progress.DeviceCode.VerificationUri;
                    CopyHint = "";
                    Stage = BugReportStage.SigningIn;
                    break;
                case BugReportState.CreatingIssue:
                    StatusText = "Отправляем обращение...";
                    Stage = BugReportStage.Sending;
                    break;
            }
        }

        private void SendViaBrowser()
        {
            var draft = _draft;
            if (draft == null) return;

            CancelPending();

            var url = IssueUrlBuilder.Build(draft.Title, draft.Body);
            var copied = _shell.CopyToClipboard(draft.Body);

            try
            {
                _shell.OpenUrl(url.Url);
            }
            catch (Exception ex)
            {
                var message = "Не удалось открыть браузер. Откройте в нём адрес " + BugReportConfig.NewIssueWebUrl +
                              (copied ? " и вставьте текст отчёта (он уже скопирован)." : ".");
                ShowError(message, ex.Message);
                return;
            }

            IssueUrl = "";
            SuccessTitle = "Почти готово!";
            SuccessMessage = "Мы открыли страницу GitHub в браузере, текст уже подставлен. " +
                             "Войдите в аккаунт (или зарегистрируйтесь - это бесплатно) и нажмите зелёную кнопку «Submit new issue»." +
                             (copied ? " Полный текст отчёта также скопирован в буфер обмена." : "");
            Succeeded = true;
            Stage = BugReportStage.Success;
        }

        private void CopyCode()
        {
            CopyHint = _shell.CopyToClipboard(UserCode)
                ? "Код скопирован"
                : "Не удалось скопировать - введите код вручную";
        }

        private void OpenVerification()
        {
            CopyCode();
            OpenSafely(VerificationUri);
        }

        private void OpenIssue() => OpenSafely(IssueUrl);

        // Открываем только адреса github.com: ссылки пришли из ответа сервера.
        private void OpenSafely(string url)
        {
            if (!url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                url = BugReportConfig.DeviceActivationUrl;

            try
            {
                _shell.OpenUrl(url);
            }
            catch (Exception ex)
            {
                CopyHint = "Не удалось открыть браузер. Откройте вручную: " + url;
                System.Diagnostics.Debug.WriteLine($"OpenUrl failed: {ex.Message}");
            }
        }

        private void ShowError(string message, string details)
        {
            ErrorMessage = message;
            ErrorDetails = DiagnosticsSanitizer.Sanitize(details);
            Stage = BugReportStage.Error;
        }

        // Отменяет текущую отправку и "списывает" её: её запоздавшие уведомления и ошибки игнорируются.
        private void CancelPending()
        {
            _attempt++;
            try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        }

        private void RequestClose()
        {
            _closing = true;
            CancelPending();
            CloseRequested?.Invoke(Succeeded);
        }

        /// <summary>Вызывается при закрытии окна любым способом.</summary>
        public void OnWindowClosing()
        {
            _closing = true;
            CancelPending();
        }

        public static string DescribeError(BugReportException? ex) => ex?.Kind switch
        {
            BugReportErrorKind.Network =>
                "Не удалось связаться с GitHub. Проверьте подключение к интернету и попробуйте ещё раз.",
            BugReportErrorKind.NotConfigured =>
                "Автоматическая отправка пока не настроена. Нажмите «Отправить через браузер».",
            BugReportErrorKind.AuthExpired =>
                "Время на ввод кода вышло. Нажмите «Повторить» и введите код чуть быстрее.",
            BugReportErrorKind.AuthDenied =>
                "Вход в GitHub не был подтверждён. Нажмите «Повторить» и разрешите доступ либо отправьте отчёт через браузер.",
            BugReportErrorKind.Unauthorized =>
                "GitHub не принял сохранённый вход. Нажмите «Повторить» и войдите заново.",
            BugReportErrorKind.Forbidden =>
                "GitHub не разрешил создать обращение с этого аккаунта. Отправьте отчёт через браузер.",
            BugReportErrorKind.RateLimited =>
                "GitHub временно ограничил число отправок. Подождите несколько минут и повторите.",
            BugReportErrorKind.Rejected =>
                "GitHub не принял обращение. Отправьте отчёт через браузер.",
            BugReportErrorKind.ServerError =>
                "На стороне GitHub сейчас сбой. Попробуйте позже или отправьте отчёт через браузер.",
            _ =>
                "Что-то пошло не так при отправке. Отправьте отчёт через браузер."
        };
    }

    // Progress<T> доставляет события через пул потоков без гарантии порядка, а нам нужен строгий порядок.
    // Сервис вызывает Report уже в контексте окна (await возвращает туда), поэтому достаточно прямого вызова.
    internal sealed class ImmediateProgress : IProgress<BugReportProgress>
    {
        private readonly Action<BugReportProgress> _handler;

        public ImmediateProgress(Action<BugReportProgress> handler) => _handler = handler;

        public void Report(BugReportProgress value) => _handler(value);
    }
}
