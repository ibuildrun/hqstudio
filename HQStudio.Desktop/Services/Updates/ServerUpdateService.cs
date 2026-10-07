using System.Net.Http;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;

namespace HQStudio.Services.Updates
{
    public sealed class ServerUpdateOptions
    {
        public string DockerExecutable { get; init; } = "docker";
        public string TempRoot { get; init; } = Path.Combine(Path.GetTempPath(), "HQStudio_Update");
        public TimeSpan DockerCheckTimeout { get; init; } = TimeSpan.FromSeconds(30);
        public TimeSpan HealthTimeout { get; init; } = TimeSpan.FromSeconds(120);
        public TimeSpan RollbackHealthTimeout { get; init; } = TimeSpan.FromSeconds(60);
        public TimeSpan HealthPollInterval { get; init; } = TimeSpan.FromSeconds(2);
    }

    /// <summary>Updates the docker based site: new deploy files, new image tag, pull, restart, health check, rollback.</summary>
    public sealed class ServerUpdateService
    {
        public const string BackupFolderName = ".update-backup";

        private readonly IProcessRunner _runner;
        private readonly ReleaseDownloader _downloader;
        private readonly HttpClient _health;
        private readonly ServerUpdateOptions _options;

        public ServerUpdateService(IProcessRunner runner, ReleaseDownloader downloader,
            HttpMessageHandler healthHandler, ServerUpdateOptions? options = null)
        {
            _runner = runner;
            _downloader = downloader;
            _health = new HttpClient(healthHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(5) };
            _options = options ?? new ServerUpdateOptions();
        }

        /// <summary>HQSTUDIO_VERSION from the install's .env, or null when it cannot be read.</summary>
        public static string? ReadInstalledVersion(InstallInfo install)
        {
            try
            {
                return EnvFileEditor.ReadValue(Path.Combine(install.ServerDir, ".env"), EnvFileEditor.VersionKey);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private sealed class Journal
        {
            public byte[] EnvOriginal = Array.Empty<byte>();
            public readonly List<string> Overwritten = new();
            public readonly List<string> Created = new();
        }

        public async Task<UpdateOperationResult> UpdateAsync(ReleaseInfo release, InstallInfo install,
            IProgress<UpdateProgress>? progress, Action<string>? log, CancellationToken ct)
        {
            log ??= _ => { };
            double lastPercent = 0;
            void Report(string stage, double percent, string? detail = null)
            {
                lastPercent = Math.Max(lastPercent, percent);
                progress?.Report(new UpdateProgress(stage, lastPercent, detail));
            }

            var serverDir = Path.GetFullPath(install.ServerDir);
            var composePath = Path.Combine(serverDir, "docker-compose.yml");
            var envPath = Path.Combine(serverDir, ".env");
            var stageDir = Path.Combine(_options.TempRoot, $"server-{release.Version}");
            var journal = new Journal();
            var modified = false;

            try
            {
                Report("Подготовка", 0);

                if (!Directory.Exists(serverDir) || !File.Exists(composePath))
                    throw new UpdateException(
                        $"Не найдена папка сайта ({serverDir}). Установите сайт заново или обратитесь в поддержку.");
                if (!File.Exists(envPath))
                    throw new UpdateException(
                        $"В папке сайта нет файла настроек .env ({serverDir}). Автоматическое обновление невозможно.");

                var asset = release.ServerAsset
                    ?? throw new UpdateException(
                        $"В версии {release.Version} нет файлов сайта. Возможно, они ещё загружаются, повторите через несколько минут.");

                Report("Проверка Docker", 1);
                await EnsureDockerRunningAsync(serverDir, log, ct).ConfigureAwait(false);
                log("Docker работает.");

                if (Directory.Exists(stageDir)) Directory.Delete(stageDir, recursive: true);
                Directory.CreateDirectory(stageDir);

                var zipPath = Path.Combine(stageDir, asset.Name);
                log($"Скачиваю {asset.Name}...");
                var lastStep = -1;
                await _downloader.DownloadAsync(asset, zipPath, new SyncProgress<DownloadProgress>(p =>
                {
                    var fraction = p.TotalBytes is > 0 ? (double)p.BytesReceived / p.TotalBytes.Value : 0;
                    var detail = p.TotalBytes is > 0
                        ? $"{ByteSize.Format(p.BytesReceived)} из {ByteSize.Format(p.TotalBytes.Value)}"
                        : ByteSize.Format(p.BytesReceived);
                    Report("Скачивание файлов сайта", 3 + fraction * 17, detail);
                    var step = (int)(fraction * 4);
                    if (step != lastStep)
                    {
                        lastStep = step;
                        log($"Скачано {detail}");
                    }
                }), ct).ConfigureAwait(false);
                log(asset.Sha256 != null
                    ? "Контрольная сумма совпала."
                    : "Контрольная сумма не опубликована, проверка пропущена.");

                Report("Распаковка", 21);
                var filesDir = Path.Combine(stageDir, "files");
                var staged = ArchiveExtractor.ExtractToDirectory(zipPath, filesDir, IsEnvFile);
                log($"Распаковано файлов: {staged.Count}.");

                // From here on the install is modified, so every failure must roll back.
                Report("Сохранение копии", 23);
                var backupRoot = Path.Combine(serverDir, BackupFolderName);
                if (Directory.Exists(backupRoot)) Directory.Delete(backupRoot, recursive: true);
                Directory.CreateDirectory(Path.Combine(backupRoot, "files"));

                journal.EnvOriginal = File.ReadAllBytes(envPath);
                File.WriteAllBytes(Path.Combine(backupRoot, "env.backup"), journal.EnvOriginal);
                var previousVersion = EnvFileEditor.ReadValue(envPath, EnvFileEditor.VersionKey);
                log($"Предыдущая версия сайта: {previousVersion ?? "не указана"}.");

                modified = true;
                Report("Замена файлов сайта", 24);
                foreach (var relative in staged)
                {
                    var target = Path.Combine(serverDir, relative);
                    if (File.Exists(target))
                    {
                        var saved = Path.Combine(backupRoot, "files", relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(saved)!);
                        File.Copy(target, saved, overwrite: true);
                        journal.Overwritten.Add(relative);
                    }
                    else
                    {
                        journal.Created.Add(relative);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(Path.Combine(filesDir, relative), target, overwrite: true);
                }

                EnvFileEditor.WriteValue(envPath, EnvFileEditor.VersionKey, release.Version);
                log($"Версия сайта в настройках: {release.Version}.");

                // docker compose pull
                Report("Загрузка образов", 26, "Подготовка...");
                var parser = new DockerPullProgressParser();
                var parserLock = new object();
                var pull = await _runner.RunAsync(_options.DockerExecutable, ComposeArgs(serverDir, "pull"), serverDir,
                    line =>
                    {
                        log(line);
                        lock (parserLock)
                        {
                            if (parser.Feed(line))
                            {
                                var detail = parser.LayerCount > 0
                                    ? $"Готово слоёв: {parser.CompletedLayers} из {parser.LayerCount}"
                                    : null;
                                Report("Загрузка образов", 26 + parser.Percent * 0.5, detail);
                            }
                        }
                    }, ct).ConfigureAwait(false);
                if (pull.ExitCode != 0)
                    throw new UpdateException(ExplainDockerFailure(pull, "загрузке образов"));
                Report("Загрузка образов", 76);

                // docker compose up -d
                Report("Запуск новой версии сайта", 78);
                var up = await _runner.RunAsync(_options.DockerExecutable,
                    ComposeArgs(serverDir, "up", "-d", "--remove-orphans"), serverDir, log, ct).ConfigureAwait(false);
                if (up.ExitCode != 0)
                    throw new UpdateException(ExplainDockerFailure(up, "запуске сайта"));
                Report("Запуск новой версии сайта", 85);

                // health
                Report("Проверка работы сайта", 85, "Жду ответа сайта...");
                var healthy = await WaitForHealthAsync(install.ApiUrl, _options.HealthTimeout, log,
                    elapsedFraction => Report("Проверка работы сайта", 85 + elapsedFraction * 14, "Жду ответа сайта..."),
                    ct).ConfigureAwait(false);
                if (!healthy)
                    throw new UpdateException(
                        $"Сайт не ответил на проверку за {(int)_options.HealthTimeout.TotalSeconds} секунд после обновления.");

                CleanupQuietly(stageDir);
                Report("Готово", 100);
                log($"Сайт обновлён до версии {release.Version}.");
                return new UpdateOperationResult(true, $"Сайт обновлён до версии {release.Version}.");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                var reason = ex switch
                {
                    UpdateException ue => ue.Message,
                    OperationCanceledException => "Обновление сайта отменено.",
                    _ => $"Непредвиденная ошибка при обновлении сайта: {ex.Message}"
                };
                var help = (ex as UpdateException)?.HelpUrl;
                log($"Ошибка: {reason}");
                if (ex is not UpdateException)
                    log(ex.ToString());

                CleanupQuietly(stageDir);

                if (!modified)
                    return new UpdateOperationResult(false, reason, help);

                var restored = await RollbackAsync(serverDir, envPath, journal, install, log,
                    (stage, detail) => Report(stage, lastPercent, detail)).ConfigureAwait(false);

                var message = restored
                    ? $"{reason} Прежняя версия сайта восстановлена и работает."
                    : $"{reason} Не удалось автоматически запустить прежнюю версию сайта. Откройте Docker Desktop, проверьте, что контейнеры HQ Studio запущены, и при необходимости повторите обновление. Подробности смотрите в журнале.";
                return new UpdateOperationResult(false, message, help, RolledBack: true);
            }
        }

        private async Task<bool> RollbackAsync(string serverDir, string envPath, Journal journal,
            InstallInfo install, Action<string> log, Action<string, string?> report)
        {
            report("Возврат прежней версии", "Восстанавливаю файлы...");
            log("Возвращаю прежнюю версию сайта...");

            try
            {
                EnvFileEditor.WriteAtomicBytes(envPath, journal.EnvOriginal);
                log("Файл настроек .env восстановлен.");
            }
            catch (Exception ex)
            {
                log($"Не удалось восстановить .env: {ex.Message}");
            }

            var backupFiles = Path.Combine(serverDir, BackupFolderName, "files");
            foreach (var relative in journal.Overwritten)
            {
                try
                {
                    File.Copy(Path.Combine(backupFiles, relative), Path.Combine(serverDir, relative), overwrite: true);
                }
                catch (Exception ex)
                {
                    log($"Не удалось восстановить {relative}: {ex.Message}");
                }
            }
            foreach (var relative in journal.Created)
            {
                try
                {
                    var target = Path.Combine(serverDir, relative);
                    if (File.Exists(target)) File.Delete(target);
                }
                catch (Exception ex)
                {
                    log($"Не удалось удалить {relative}: {ex.Message}");
                }
            }
            log("Файлы сайта восстановлены.");

            try
            {
                report("Возврат прежней версии", "Запускаю прежнюю версию...");
                var up = await _runner.RunAsync(_options.DockerExecutable, ComposeArgs(serverDir, "up", "-d"),
                    serverDir, log, CancellationToken.None).ConfigureAwait(false);
                if (up.ExitCode != 0)
                {
                    log($"Не удалось запустить прежнюю версию: {ExplainDockerFailure(up, "возврате версии")}");
                    return false;
                }

                report("Возврат прежней версии", "Жду ответа сайта...");
                var healthy = await WaitForHealthAsync(install.ApiUrl, _options.RollbackHealthTimeout, log,
                    null, CancellationToken.None).ConfigureAwait(false);
                log(healthy ? "Прежняя версия сайта работает." : "Прежняя версия сайта не отвечает.");
                return healthy;
            }
            catch (Exception ex)
            {
                log($"Ошибка при возврате версии: {ex.Message}");
                return false;
            }
        }

        private async Task EnsureDockerRunningAsync(string serverDir, Action<string> log, CancellationToken ct)
        {
            const string notRunning =
                "Docker сейчас не запущен. Откройте программу Docker Desktop, дождитесь, пока она загрузится (значок кита в трее перестанет мигать), и повторите обновление. " +
                "Если Docker Desktop не установлен, установите его: " + UpdateLinks.DockerInstallUrl;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_options.DockerCheckTimeout);

            ProcessResult result;
            try
            {
                result = await _runner.RunAsync(_options.DockerExecutable, new[] { "version" }, serverDir,
                    null, timeout.Token).ConfigureAwait(false);
            }
            catch (Win32Exception ex)
            {
                throw new UpdateException(
                    "Docker не найден на этом компьютере. Установите Docker Desktop и повторите обновление: " +
                    UpdateLinks.DockerInstallUrl, ex, UpdateLinks.DockerInstallUrl);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new UpdateException(notRunning, helpUrl: UpdateLinks.DockerInstallUrl);
            }

            if (result.ExitCode != 0)
            {
                log(LastNonEmptyLine(result.StandardError) ?? "docker version завершился с ошибкой.");
                throw new UpdateException(notRunning, helpUrl: UpdateLinks.DockerInstallUrl);
            }
        }

        private async Task<bool> WaitForHealthAsync(string apiUrl, TimeSpan timeout, Action<string> log,
            Action<double>? onTick, CancellationToken ct)
        {
            var url = apiUrl.TrimEnd('/') + "/api/health";
            var clock = Stopwatch.StartNew();
            var nextLog = TimeSpan.Zero;

            while (true)
            {
                try
                {
                    using var response = await _health.GetAsync(url, ct).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        log($"Сайт отвечает ({(int)response.StatusCode}).");
                        return true;
                    }
                }
                catch (HttpRequestException) { }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

                if (clock.Elapsed >= timeout)
                    return false;

                if (clock.Elapsed >= nextLog)
                {
                    log($"Жду ответа сайта... ({(int)clock.Elapsed.TotalSeconds} с)");
                    nextLog = clock.Elapsed + TimeSpan.FromSeconds(10);
                }
                onTick?.Invoke(Math.Min(1.0, clock.Elapsed.TotalSeconds / Math.Max(1.0, timeout.TotalSeconds)));

                await Task.Delay(_options.HealthPollInterval, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Same invocation as the installer scripts: fixed project directory and compose file, plus the
        /// "tunnel" profile when a Tuna token is configured (otherwise the tunnel service would be skipped).
        /// </summary>
        internal static IReadOnlyList<string> ComposeArgs(string serverDir, params string[] command)
        {
            var args = new List<string>
            {
                "compose", "--project-directory", serverDir, "-f", Path.Combine(serverDir, "docker-compose.yml")
            };

            string? token = null;
            try
            {
                token = EnvFileEditor.ReadValue(Path.Combine(serverDir, ".env"), "TUNA_TOKEN");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

            if (!string.IsNullOrWhiteSpace(token))
            {
                args.Add("--profile");
                args.Add("tunnel");
            }

            args.AddRange(command);
            return args;
        }

        private static bool IsEnvFile(string relativePath) =>
            string.Equals(relativePath.Replace('\\', '/'), ".env", StringComparison.OrdinalIgnoreCase);

        /// <summary>Maps typical docker failures to a plain Russian explanation.</summary>
        public static string ExplainDockerFailure(ProcessResult result, string step)
        {
            var text = result.StandardError + "\n" + result.StandardOutput;

            bool Has(params string[] needles) =>
                needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

            if (Has("Cannot connect to the Docker daemon", "error during connect", "dockerDesktopLinuxEngine", "docker_engine"))
                return "Docker остановился или не запущен. Откройте Docker Desktop и повторите обновление.";
            if (Has("no space left on device"))
                return "На диске не хватает места для новых образов. Освободите место и повторите обновление.";
            if (Has("manifest unknown", "manifest for", "not found: manifest"))
                return "Образы этой версии сайта ещё не опубликованы. Подождите 10-15 минут после выхода версии и повторите обновление.";
            if (Has("toomanyrequests", "rate limit"))
                return "Хранилище образов временно ограничило скачивание. Подождите около часа или войдите в Docker Desktop под своей учётной записью и повторите.";
            if (Has("unauthorized", "denied"))
                return "Не удалось получить образы: доступ запрещён. Если на компьютере выполнялся вход в ghcr.io, выполните в командной строке «docker logout ghcr.io» и повторите.";
            if (Has("no such host", "dial tcp", "i/o timeout", "TLS handshake timeout", "network is unreachable",
                    "Temporary failure in name resolution", "connection reset", "context deadline exceeded"))
                return "Не удалось скачать образы: нет связи с хранилищем. Проверьте интернет и повторите обновление.";
            if (Has("port is already allocated", "address already in use", "ports are not available"))
                return "Не удалось запустить сайт: нужный порт занят другой программой. Закройте её или перезагрузите компьютер.";

            var last = LastNonEmptyLine(result.StandardError) ?? LastNonEmptyLine(result.StandardOutput);
            return last != null
                ? $"Docker сообщил об ошибке при {step}: {last}"
                : $"Docker завершился с ошибкой при {step} (код {result.ExitCode}).";
        }

        private static string? LastNonEmptyLine(string text) =>
            text.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);

        private static void CleanupQuietly(string dir)
        {
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
