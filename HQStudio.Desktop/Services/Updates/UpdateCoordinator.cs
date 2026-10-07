using System.Net.Http;
using System.IO;
using System.Reflection;

namespace HQStudio.Services.Updates
{
    /// <summary>What is installed, what is the latest release and which parts are behind.</summary>
    public sealed record UpdateStatus
    {
        public string InstalledAppVersion { get; init; } = "";
        public bool SiteInstalled { get; init; }
        /// <summary>Set when install.json exists but cannot be used.</summary>
        public string? SiteProblem { get; init; }
        public string? InstalledServerVersion { get; init; }
        public ReleaseInfo? Latest { get; init; }
        /// <summary>False when <see cref="Latest"/> was rebuilt from the remembered tag and has no assets.</summary>
        public bool LatestIsComplete { get; init; }

        public string LatestVersion => Latest?.Version ?? "";
        public bool AppUpdateAvailable => Latest != null && SemVer.IsNewer(Latest.Version, InstalledAppVersion);
        public bool ServerUpdateAvailable =>
            SiteInstalled && Latest != null && SemVer.IsOutdated(InstalledServerVersion, Latest.Version);
        public bool AnyUpdateAvailable => AppUpdateAvailable || ServerUpdateAvailable;
    }

    public sealed record UpdateCheckResult(bool Success, string Message);

    /// <summary>Single entry point for the UI: check, update app, update site, update both.</summary>
    public sealed class UpdateCoordinator
    {
        public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
        private const int MaxLogLines = 2000;
        private const long MaxLogFileBytes = 512 * 1024;

        private static readonly Lazy<UpdateCoordinator> LazyInstance = new(CreateDefault);
        public static UpdateCoordinator Instance => LazyInstance.Value;

        private readonly GitHubReleaseClient _github;
        private readonly AppUpdateService _app;
        private readonly ServerUpdateService _server;
        private readonly Func<InstallInfoReadResult> _readInstall;
        private readonly string _installedAppVersion;
        private readonly UpdateStateStore _store;
        private readonly Func<bool> _autoCheckEnabled;
        private readonly Func<DateTime> _utcNow;
        private readonly Action<string>? _notify;
        private readonly string? _logFilePath;

        private readonly object _logLock = new();
        private readonly List<string> _log = new();
        private int _busy;
        private int _notified;
        private UpdateStatus _status = new();

        public event EventHandler? StatusChanged;
        public event EventHandler<UpdateProgress>? ProgressChanged;
        public event EventHandler<string>? LogAdded;
        public event EventHandler? BusyChanged;

        public UpdateCoordinator(GitHubReleaseClient github, AppUpdateService app, ServerUpdateService server,
            Func<InstallInfoReadResult> readInstall, string installedAppVersion, UpdateStateStore store,
            Func<bool>? autoCheckEnabled = null, Func<DateTime>? utcNow = null, Action<string>? notify = null,
            string? logFilePath = null)
        {
            _github = github;
            _app = app;
            _server = server;
            _readInstall = readInstall;
            _installedAppVersion = installedAppVersion;
            _store = store;
            _autoCheckEnabled = autoCheckEnabled ?? (() => true);
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _notify = notify;
            _logFilePath = logFilePath;
            RefreshLocalState();
        }

        public UpdateStatus Status => _status;
        /// <summary>Latest report of the running (or last) operation, for views created mid-update.</summary>
        public UpdateProgress? LastProgress { get; private set; }
        public bool IsBusy => Volatile.Read(ref _busy) != 0;

        public IReadOnlyList<string> GetLog()
        {
            lock (_logLock) return _log.ToArray();
        }

        public static string GetInstalledAppVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (SemVer.TryParse(info, out var fromInfo))
                return fromInfo.ToString();
            var v = asm.GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
        }

        public static UpdateCoordinator CreateDefault()
        {
            var releases = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            // Health checks go to localhost: never through a system proxy.
            var local = new SocketsHttpHandler { UseProxy = false };

            var downloader = new ReleaseDownloader(releases);
            var localDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio");

            var install = InstallInfoReader.Read();
            var repo = install.Info?.Repo ?? GitHubReleaseClient.DefaultRepo;

            return new UpdateCoordinator(
                new GitHubReleaseClient(releases, repo),
                new AppUpdateService(downloader),
                new ServerUpdateService(new ProcessRunner(), downloader, local,
                    new ServerUpdateOptions { DockerExecutable = DockerLocator.Resolve() }),
                () => InstallInfoReader.Read(),
                GetInstalledAppVersion(),
                new UpdateStateStore(),
                () => UpdateSettingsReader.IsAutoCheckEnabled(),
                notify: message => ToastService.Instance.ShowInfo(message),
                logFilePath: Path.Combine(localDir, "update.log"));
        }

        /// <summary>Re-reads install.json and the installed site version; no network.</summary>
        public void RefreshLocalState()
        {
            var install = _readInstall();
            string? serverVersion = null;
            if (install.Info != null)
                serverVersion = ServerUpdateService.ReadInstalledVersion(install.Info);

            _status = _status with
            {
                InstalledAppVersion = _installedAppVersion,
                SiteInstalled = install.Info != null,
                SiteProblem = install.Problem,
                InstalledServerVersion = serverVersion
            };
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task<UpdateCheckResult> CheckAsync(bool silent = false, CancellationToken ct = default)
        {
            if (!TryEnterBusy())
                return new UpdateCheckResult(false, "Сейчас выполняется обновление, подождите.");

            try
            {
                return await CheckCoreAsync(silent, ct).ConfigureAwait(false);
            }
            finally
            {
                LeaveBusy();
            }
        }

        private async Task<UpdateCheckResult> CheckCoreAsync(bool silent, CancellationToken ct)
        {
            try
            {
                RefreshLocalState();
                if (!silent) AddLog("Проверяю обновления...");
                var latest = await _github.GetLatestAsync(ct).ConfigureAwait(false);

                _status = _status with { Latest = latest, LatestIsComplete = true };
                _store.Save(new UpdateState { LastCheckUtc = _utcNow(), LastLatestTag = latest.Tag });
                StatusChanged?.Invoke(this, EventArgs.Empty);

                var message = DescribeStatus(_status);
                if (!silent) AddLog(message);
                return new UpdateCheckResult(true, message);
            }
            catch (UpdateException ex)
            {
                AddLog($"Проверка не удалась: {ex.Message}");
                return new UpdateCheckResult(false, ex.Message);
            }
            catch (OperationCanceledException)
            {
                return new UpdateCheckResult(false, "Проверка отменена.");
            }
            catch (Exception ex)
            {
                AddLog($"Проверка не удалась: {ex}");
                return new UpdateCheckResult(false, $"Не удалось проверить обновления: {ex.Message}");
            }
        }

        public static string DescribeStatus(UpdateStatus s)
        {
            if (s.Latest == null)
                return "Проверка ещё не выполнялась.";
            if (!s.AnyUpdateAvailable)
                return $"Установлена последняя версия ({s.Latest.Version}).";

            var parts = new List<string>();
            if (s.AppUpdateAvailable) parts.Add($"приложение {s.InstalledAppVersion} -> {s.Latest.Version}");
            if (s.ServerUpdateAvailable)
                parts.Add($"сайт {(string.IsNullOrWhiteSpace(s.InstalledServerVersion) ? "неизвестная версия" : s.InstalledServerVersion)} -> {s.Latest.Version}");
            return "Доступно обновление: " + string.Join(", ", parts) + ".";
        }

        /// <summary>
        /// Silent check for app start: honours the AutoCheckUpdates setting, runs the network check at most
        /// once per <see cref="CheckInterval"/>, and shows a non-blocking toast when something is behind.
        /// </summary>
        public async Task RunStartupCheckAsync(CancellationToken ct = default)
        {
            try
            {
                if (!_autoCheckEnabled())
                    return;

                RefreshLocalState();
                var state = _store.Load();
                var now = _utcNow();

                if (state.LastCheckUtc is DateTime last && now >= last && now - last < CheckInterval)
                {
                    if (SemVer.TryParse(state.LastLatestTag, out var known))
                    {
                        _status = _status with
                        {
                            Latest = new ReleaseInfo { Tag = state.LastLatestTag!, Version = known.ToString() },
                            LatestIsComplete = false
                        };
                        StatusChanged?.Invoke(this, EventArgs.Empty);
                        NotifyIfBehind();
                    }
                    return;
                }

                var result = await CheckAsync(silent: true, ct).ConfigureAwait(false);
                if (result.Success)
                    NotifyIfBehind();
            }
            catch (Exception ex)
            {
                AddLog($"Автоматическая проверка не удалась: {ex.Message}");
            }
        }

        private void NotifyIfBehind()
        {
            var s = _status;
            // One toast per app run, even if the main window is recreated after a re-login.
            if (s.AnyUpdateAvailable && Interlocked.Exchange(ref _notified, 1) == 0)
                _notify?.Invoke($"Доступно обновление {s.LatestVersion}");
        }

        public Task<UpdateOperationResult> UpdateAppAsync(CancellationToken ct = default) =>
            RunOperationAsync(ct, async (release, scale) =>
            {
                if (!SemVer.IsNewer(release.Version, _status.InstalledAppVersion))
                    return new UpdateOperationResult(true, $"Приложение уже последней версии ({_status.InstalledAppVersion}).");

                return await _app.UpdateAsync(release, scale("Приложение", 0, 100), AddLog, ct).ConfigureAwait(false);
            });

        public Task<UpdateOperationResult> UpdateServerAsync(CancellationToken ct = default) =>
            RunOperationAsync(ct, async (release, scale) => await RunServerAsync(release, scale("Сайт", 0, 100), ct).ConfigureAwait(false));

        public Task<UpdateOperationResult> UpdateAllAsync(CancellationToken ct = default) =>
            RunOperationAsync(ct, async (release, scale) =>
            {
                var doServer = _status.ServerUpdateAvailable;
                var doApp = _status.AppUpdateAvailable;
                if (!doServer && !doApp)
                    return new UpdateOperationResult(true, "Всё уже обновлено до последней версии.");

                var serverEnd = doApp ? 70 : 100;
                if (doServer)
                {
                    var serverResult = await RunServerAsync(release, scale("Сайт", 0, serverEnd), ct).ConfigureAwait(false);
                    if (!serverResult.Success)
                    {
                        var skipped = doApp ? " Обновление приложения не выполнялось." : "";
                        return serverResult with { Message = serverResult.Message + skipped };
                    }
                    if (!doApp)
                        return serverResult;
                }

                var appResult = await _app.UpdateAsync(release, scale("Приложение", doServer ? 70 : 0, 100), AddLog, ct)
                    .ConfigureAwait(false);
                if (doServer)
                    return appResult.Success
                        ? appResult with { Message = "Сайт обновлён. " + appResult.Message }
                        : appResult with { Message = "Сайт обновлён, но приложение обновить не удалось. " + appResult.Message };
                return appResult;
            });

        private async Task<UpdateOperationResult> RunServerAsync(ReleaseInfo release, IProgress<UpdateProgress> progress,
            CancellationToken ct)
        {
            var install = _readInstall();
            if (install.Info == null)
                return new UpdateOperationResult(false, install.Problem ?? "Сайт не установлен на этом компьютере.");

            RefreshLocalState();
            if (!_status.ServerUpdateAvailable)
                return new UpdateOperationResult(true, $"Сайт уже последней версии ({_status.InstalledServerVersion}).");

            var result = await _server.UpdateAsync(release, install.Info, progress, AddLog, ct).ConfigureAwait(false);
            RefreshLocalState();
            return result;
        }

        private delegate IProgress<UpdateProgress> ScaleFactory(string prefix, double lo, double hi);

        private async Task<UpdateOperationResult> RunOperationAsync(CancellationToken ct,
            Func<ReleaseInfo, ScaleFactory, Task<UpdateOperationResult>> operation)
        {
            if (!TryEnterBusy())
                return new UpdateOperationResult(false, "Сейчас уже выполняется другое обновление, дождитесь его окончания.");

            try
            {
                var status = _status;
                if (status.Latest == null || !status.LatestIsComplete)
                {
                    var check = await CheckCoreAsync(silent: false, ct).ConfigureAwait(false);
                    if (!check.Success)
                        return new UpdateOperationResult(false, check.Message);
                }

                var release = _status.Latest!;
                RaiseProgress(new UpdateProgress("Подготовка", 0));

                var result = await operation(release, (prefix, lo, hi) => new SyncProgress<UpdateProgress>(p =>
                {
                    var percent = lo + (hi - lo) * Math.Clamp(p.Percent, 0, 100) / 100.0;
                    RaiseProgress(new UpdateProgress($"{prefix}: {p.Stage}", percent, p.Detail));
                })).ConfigureAwait(false);

                if (!result.Success)
                    AddLog("Итог: " + result.Message);
                return result;
            }
            catch (OperationCanceledException)
            {
                return new UpdateOperationResult(false, "Обновление отменено.");
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
                return new UpdateOperationResult(false, $"Непредвиденная ошибка: {ex.Message}");
            }
            finally
            {
                LeaveBusy();
            }
        }

        private void RaiseProgress(UpdateProgress progress)
        {
            LastProgress = progress;
            ProgressChanged?.Invoke(this, progress);
        }

        private bool TryEnterBusy()
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                return false;
            BusyChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void LeaveBusy()
        {
            Volatile.Write(ref _busy, 0);
            BusyChanged?.Invoke(this, EventArgs.Empty);
        }

        private void AddLog(string line)
        {
            var stamped = $"{DateTime.Now:HH:mm:ss}  {line}";
            lock (_logLock)
            {
                _log.Add(stamped);
                if (_log.Count > MaxLogLines)
                    _log.RemoveRange(0, _log.Count - MaxLogLines);

                if (_logFilePath != null)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                        var info = new FileInfo(_logFilePath);
                        if (info.Exists && info.Length > MaxLogFileBytes)
                            info.Delete();
                        File.AppendAllText(_logFilePath, $"{DateTime.Now:yyyy-MM-dd} {stamped}{Environment.NewLine}");
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
            }
            LogAdded?.Invoke(this, stamped);
        }
    }
}
