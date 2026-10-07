using System.IO;
using System.Diagnostics;
using System.Text;

namespace HQStudio.Services.Updates
{
    public sealed class AppUpdateOptions
    {
        public string CurrentExePath { get; init; } = Environment.ProcessPath ?? "";
        public int CurrentProcessId { get; init; } = Environment.ProcessId;
        public string TempRoot { get; init; } = Path.Combine(Path.GetTempPath(), "HQStudio_Update");
        /// <summary>Starts the detached helper script; receives the script path.</summary>
        public Action<string> LaunchHelper { get; init; } = DefaultLaunchHelper;
        public Action ShutdownApplication { get; init; } = DefaultShutdown;

        private static void DefaultLaunchHelper(string scriptPath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static void DefaultShutdown()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;
            app.Dispatcher.Invoke(() => app.Shutdown());
        }
    }

    /// <summary>Downloads the desktop build and swaps the running exe through a helper script after exit.</summary>
    public sealed class AppUpdateService
    {
        private const string ExeName = "HQStudio.exe";

        private readonly ReleaseDownloader _downloader;
        private readonly AppUpdateOptions _options;

        public AppUpdateService(ReleaseDownloader downloader, AppUpdateOptions? options = null)
        {
            _downloader = downloader;
            _options = options ?? new AppUpdateOptions();
        }

        public async Task<UpdateOperationResult> UpdateAsync(ReleaseInfo release,
            IProgress<UpdateProgress>? progress, Action<string>? log, CancellationToken ct)
        {
            string? stageDir = null;
            try
            {
                progress?.Report(new UpdateProgress("Подготовка", 0));

                var exe = ResolveTarget();
                var asset = release.DesktopAsset
                    ?? throw new UpdateException(
                        $"В версии {release.Version} нет файла приложения. Возможно, он ещё загружается, повторите через несколько минут.");

                stageDir = Path.Combine(_options.TempRoot, $"app-{release.Version}");
                ResetDirectory(stageDir);

                var zipPath = Path.Combine(stageDir, asset.Name);
                log?.Invoke($"Скачиваю {asset.Name}...");
                var lastLogged = -1;
                await _downloader.DownloadAsync(asset, zipPath, new SyncProgress<DownloadProgress>(p =>
                {
                    var fraction = p.TotalBytes is > 0 ? (double)p.BytesReceived / p.TotalBytes.Value : 0;
                    var detail = p.TotalBytes is > 0
                        ? $"{ByteSize.Format(p.BytesReceived)} из {ByteSize.Format(p.TotalBytes.Value)}"
                        : ByteSize.Format(p.BytesReceived);
                    progress?.Report(new UpdateProgress("Скачивание новой версии", 2 + fraction * 78, detail));

                    var step = (int)(fraction * 10);
                    if (step != lastLogged)
                    {
                        lastLogged = step;
                        log?.Invoke($"Скачано {detail}");
                    }
                }), ct).ConfigureAwait(false);

                log?.Invoke(asset.Sha256 != null
                    ? "Контрольная сумма совпала."
                    : "Контрольная сумма не опубликована, проверка пропущена.");

                progress?.Report(new UpdateProgress("Распаковка", 82));
                var newExe = Path.Combine(stageDir, ExeName);
                ArchiveExtractor.ExtractSingleFile(zipPath, ExeName, newExe);
                EnsureLooksLikeExecutable(newExe);
                log?.Invoke("Новая версия распакована.");

                progress?.Report(new UpdateProgress("Подготовка к перезапуску", 92));
                var script = WriteHelperScript(exe, newExe, stageDir);
                _options.LaunchHelper(script);
                log?.Invoke("Программа закроется и запустится заново с новой версией.");

                progress?.Report(new UpdateProgress("Перезапуск программы", 100));
                _options.ShutdownApplication();

                return new UpdateOperationResult(true, "Программа перезапускается с новой версией.", RestartPending: true);
            }
            catch (UpdateException ex)
            {
                CleanupQuietly(stageDir);
                return new UpdateOperationResult(false, ex.Message, ex.HelpUrl);
            }
            catch (OperationCanceledException)
            {
                CleanupQuietly(stageDir);
                return new UpdateOperationResult(false, "Обновление отменено. Установленная версия не изменена.");
            }
            catch (Exception ex)
            {
                CleanupQuietly(stageDir);
                return new UpdateOperationResult(false,
                    $"Не удалось обновить приложение: {ex.Message}. Установленная версия не изменена.");
            }
        }

        private string ResolveTarget()
        {
            var exe = _options.CurrentExePath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe) ||
                !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(exe), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
                throw new UpdateException("Обновление приложения недоступно при запуске из среды разработки.");

            var dir = Path.GetDirectoryName(exe)!;
            var probe = Path.Combine(dir, $".hqstudio-write-test-{Guid.NewGuid():N}");
            try
            {
                File.WriteAllText(probe, "");
                File.Delete(probe);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                throw new UpdateException(
                    $"Нет прав на запись в папку программы ({dir}). Запустите HQ Studio от имени администратора или переустановите программу.", ex);
            }
            return exe;
        }

        private static void EnsureLooksLikeExecutable(string path)
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 1024 || fs.ReadByte() != 'M' || fs.ReadByte() != 'Z')
                throw new UpdateException("Скачанный файл приложения повреждён. Установленная версия не изменена.");
        }

        private string WriteHelperScript(string currentExe, string newExe, string stageDir)
        {
            Directory.CreateDirectory(_options.TempRoot);
            var script = Path.Combine(_options.TempRoot, $"update-{Guid.NewGuid():N}.cmd");

            static string Esc(string s) => s.Replace("%", "%%");

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");
            sb.AppendLine($"set \"NEWEXE={Esc(newExe)}\"");
            sb.AppendLine($"set \"TARGET={Esc(currentExe)}\"");
            sb.AppendLine($"set \"STAGE={Esc(stageDir)}\"");
            sb.AppendLine($"set \"APPPID={_options.CurrentProcessId}\"");
            sb.AppendLine("set /a WAITS=0");
            sb.AppendLine(":waitexit");
            // Full System32 paths: a Git/MSYS find.exe earlier in PATH would break the wait loop.
            sb.AppendLine("\"%SystemRoot%\\System32\\tasklist.exe\" /FI \"PID eq %APPPID%\" /NH 2>nul | \"%SystemRoot%\\System32\\find.exe\" \"%APPPID%\" >nul");
            sb.AppendLine("if errorlevel 1 goto copyexe");
            sb.AppendLine("set /a WAITS+=1");
            sb.AppendLine("if %WAITS% GEQ 90 goto copyexe");
            sb.AppendLine("\"%SystemRoot%\\System32\\ping.exe\" -n 2 127.0.0.1 >nul");
            sb.AppendLine("goto waitexit");
            sb.AppendLine(":copyexe");
            sb.AppendLine("set /a TRIES=0");
            sb.AppendLine("copy /Y \"%TARGET%\" \"%TARGET%.bak\" >nul 2>&1");
            sb.AppendLine(":copyloop");
            sb.AppendLine("copy /Y \"%NEWEXE%\" \"%TARGET%\" >nul 2>&1");
            sb.AppendLine("if not errorlevel 1 goto launch");
            sb.AppendLine("set /a TRIES+=1");
            sb.AppendLine("if %TRIES% GEQ 20 goto restore");
            sb.AppendLine("\"%SystemRoot%\\System32\\ping.exe\" -n 2 127.0.0.1 >nul");
            sb.AppendLine("goto copyloop");
            sb.AppendLine(":restore");
            sb.AppendLine("if exist \"%TARGET%.bak\" copy /Y \"%TARGET%.bak\" \"%TARGET%\" >nul 2>&1");
            sb.AppendLine(":launch");
            sb.AppendLine("start \"\" \"%TARGET%\"");
            sb.AppendLine("del \"%TARGET%.bak\" >nul 2>&1");
            sb.AppendLine("rmdir /s /q \"%STAGE%\" >nul 2>&1");
            sb.AppendLine("del \"%~f0\"");

            File.WriteAllText(script, sb.ToString(), new UTF8Encoding(false));
            return script;
        }

        private static void ResetDirectory(string dir)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
            Directory.CreateDirectory(dir);
        }

        private static void CleanupQuietly(string? dir)
        {
            if (dir == null) return;
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
