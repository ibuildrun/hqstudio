using System.ComponentModel;
using System.IO;

namespace HQStudio.Services.Site
{
    public enum UninstallStage
    {
        StopSite,
        Shortcuts,
        Registry,
        SiteFiles,
        Program
    }

    public enum UninstallStageState
    {
        Pending,
        Running,
        Done,
        /// <summary>Этап не понадобился или не получился безопасно, но удаление продолжается.</summary>
        Skipped,
        Failed
    }

    public sealed record UninstallStageUpdate(UninstallStage Stage, UninstallStageState State, string Note);

    public sealed record UninstallResult(bool HasFailures, bool HasWarnings, IReadOnlyList<UninstallStageUpdate> Stages)
    {
        public bool CleanSuccess => !HasFailures && !HasWarnings;
    }

    public sealed record UninstallOptions(bool DeleteData, string AppDir);

    public interface IUninstallRegistry
    {
        /// <summary>Удаляет HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{keyName}; отсутствие ключа не ошибка.</summary>
        void DeleteUninstallKey(string keyName);
    }

    public sealed class WindowsUninstallRegistry : IUninstallRegistry
    {
        public void DeleteUninstallKey(string keyName)
        {
            using var parent = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall", writable: true);
            parent?.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
        }
    }

    /// <summary>Места, которые трогает удаление. Всё собрано в одном месте, чтобы тесты подставляли свои пути.</summary>
    public sealed record UninstallLocations(
        string AppDir,
        string CurrentExeDir,
        string TempRoot,
        string StartMenuPrograms,
        string Desktop,
        SitePaths Paths,
        IReadOnlyList<string> ProtectedFolders)
    {
        public const string UninstallCopyFolder = "HQStudio-Uninstall";
        public const string ShortcutName = "HQ Studio.lnk";
        public const string UninstallKeyName = "HQStudio";
        public const string MainExe = "HQStudio.exe";

        public string UninstallCopyDir => Path.Combine(TempRoot, UninstallCopyFolder);

        public static UninstallLocations FromEnvironment(string appDir)
        {
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? appDir;
            var protectedFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Path.GetTempPath()
            };
            return new UninstallLocations(
                appDir, exeDir, Path.GetTempPath(),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                SitePaths.FromEnvironment(), protectedFolders);
        }
    }

    /// <summary>Строит команды отложенного удаления папок; вынесено отдельно, чтобы строку можно было проверить тестом.</summary>
    public static class UninstallCommands
    {
        /// <summary>
        /// Скрытый cmd ждёт, пока процесс удаления закроется, и повторяет rmdir, пока папки не исчезнут (до ~80 секунд).
        /// </summary>
        public static string BuildDeleteFoldersArguments(IReadOnlyList<string> folders)
        {
            var removals = string.Join(" & ", folders.Select(f => $"rmdir /s /q \"{f}\" 2>nul"));
            var allGone = string.Concat(folders.Select(f => $"if not exist \"{f}\" ")) + "exit";
            return $"/c for /l %i in (1,1,40) do (ping -n 3 127.0.0.1 >nul & {removals} & {allGone})";
        }

        public static string BuildUninstallLaunchArguments(string appDir) => $"--uninstall --app-dir \"{appDir}\"";
    }

    /// <summary>Удаляет HQ Studio по этапам. Сбой одного этапа не останавливает остальные.</summary>
    public sealed class UninstallService
    {
        private readonly ISiteFiles _files;
        private readonly ISiteProcessRunner _runner;
        private readonly ISiteDockerLocator _locator;
        private readonly ISiteInstallStore _install;
        private readonly IUninstallRegistry _registry;
        private readonly UninstallLocations _loc;

        public UninstallService(ISiteFiles files, ISiteProcessRunner runner, ISiteDockerLocator locator,
            ISiteInstallStore install, IUninstallRegistry registry, UninstallLocations locations)
        {
            _files = files;
            _runner = runner;
            _locator = locator;
            _install = install;
            _registry = registry;
            _loc = locations;
        }

        public static IReadOnlyList<(UninstallStage Stage, string Title)> StageTitles { get; } = new[]
        {
            (UninstallStage.StopSite, "Останавливаю и убираю сайт"),
            (UninstallStage.Shortcuts, "Удаляю ярлыки"),
            (UninstallStage.Registry, "Убираю программу из списка установленных"),
            (UninstallStage.SiteFiles, "Удаляю файлы сайта"),
            (UninstallStage.Program, "Удаляю программу")
        };

        public static UninstallService CreateDefault(string appDir)
        {
            var files = new SiteFiles();
            var locations = UninstallLocations.FromEnvironment(appDir);
            return new UninstallService(files, new SiteProcessRunner(), new SiteDockerLocator(),
                new SiteInstallStore(files, locations.Paths.InstallJson), new WindowsUninstallRegistry(), locations);
        }

        public async Task<UninstallResult> RunAsync(UninstallOptions options, Action<UninstallStageUpdate>? report,
            CancellationToken ct = default)
        {
            var finals = new List<UninstallStageUpdate>();

            async Task Step(UninstallStage stage, Func<Task<(UninstallStageState State, string Note)>> work)
            {
                report?.Invoke(new UninstallStageUpdate(stage, UninstallStageState.Running, ""));
                UninstallStageUpdate final;
                try
                {
                    var (state, note) = await work().ConfigureAwait(false);
                    final = new UninstallStageUpdate(stage, state, note);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception
                                               or InvalidOperationException or System.Security.SecurityException)
                {
                    final = new UninstallStageUpdate(stage, UninstallStageState.Failed, ex.Message);
                }
                finals.Add(final);
                report?.Invoke(final);
            }

            // Сведения об установке читаем до удаления install.json: они нужны первому и четвёртому этапам.
            var install = _install.Read().Info;

            await Step(UninstallStage.StopSite, () => StopSiteAsync(install, options, ct)).ConfigureAwait(false);
            await Step(UninstallStage.Shortcuts, () => Task.FromResult(RemoveShortcuts())).ConfigureAwait(false);
            await Step(UninstallStage.Registry, () => Task.FromResult(RemoveRegistry())).ConfigureAwait(false);
            await Step(UninstallStage.SiteFiles, () => Task.FromResult(RemoveSiteFiles(install, options))).ConfigureAwait(false);
            await Step(UninstallStage.Program, () => Task.FromResult(ScheduleProgramRemoval(options))).ConfigureAwait(false);

            return new UninstallResult(
                finals.Any(s => s.State == UninstallStageState.Failed),
                finals.Any(s => s.State == UninstallStageState.Skipped && s.Note.Length > 0),
                finals);
        }

        private async Task<(UninstallStageState, string)> StopSiteAsync(SiteInstallInfo? install, UninstallOptions options,
            CancellationToken ct)
        {
            if (install == null)
                return (UninstallStageState.Skipped, "");

            var docker = _locator.FindDocker();
            if (docker == null)
                return (UninstallStageState.Skipped, DockerSkippedNote(options));

            var envPath = SitePaths.EnvFile(install.ServerDir);
            var tunnel = _files.FileExists(envPath) &&
                         !string.IsNullOrWhiteSpace(SiteEnvFile.GetValue(_files.ReadAllText(envPath), SiteEnvKeys.TunaToken));

            var command = options.DeleteData
                ? new[] { "down", "--remove-orphans", "--volumes" }
                : new[] { "down", "--remove-orphans" };

            SiteProcessResult result;
            try
            {
                result = await _runner.RunAsync(docker, SiteComposeCommand.Build(install.ServerDir, tunnel, command), null, ct)
                    .ConfigureAwait(false);
            }
            catch (Win32Exception)
            {
                return (UninstallStageState.Skipped, DockerSkippedNote(options));
            }

            if (result.ExitCode == 0)
            {
                return (UninstallStageState.Done, options.DeleteData
                    ? "Сайт убран, данные удалены."
                    : "Сайт убран, данные сохранены.");
            }

            return SiteErrorMapper.Classify(result.Combined) == SiteFailureKind.DockerNotRunning
                ? (UninstallStageState.Skipped, DockerSkippedNote(options))
                : (UninstallStageState.Skipped, "Сайт не удалось остановить автоматически. Его контейнеры можно убрать в программе Docker.");
        }

        private static string DockerSkippedNote(UninstallOptions options) => options.DeleteData
            ? "Docker выключен, поэтому сайт и его данные остались в Docker. Их можно убрать в программе Docker Desktop."
            : "Docker выключен, поэтому сайт остался в Docker. Его можно убрать в программе Docker Desktop.";

        private (UninstallStageState, string) RemoveShortcuts()
        {
            var removed = 0;
            foreach (var path in new[]
                     {
                         Path.Combine(_loc.StartMenuPrograms, UninstallLocations.ShortcutName),
                         Path.Combine(_loc.Desktop, UninstallLocations.ShortcutName)
                     })
            {
                if (_files.FileExists(path))
                {
                    _files.DeleteFile(path);
                    removed++;
                }
            }
            return (UninstallStageState.Done, removed == 0 ? "Ярлыков не нашлось." : $"Удалено ярлыков: {removed}.");
        }

        private (UninstallStageState, string) RemoveRegistry()
        {
            _registry.DeleteUninstallKey(UninstallLocations.UninstallKeyName);
            return (UninstallStageState.Done, "");
        }

        private (UninstallStageState, string) RemoveSiteFiles(SiteInstallInfo? install, UninstallOptions options)
        {
            var paths = _loc.Paths;
            var serverDirs = new List<string> { paths.DefaultServerDir };
            if (install != null && IsInside(paths.Root, install.ServerDir) &&
                !SamePath(install.ServerDir, paths.DefaultServerDir))
                serverDirs.Add(install.ServerDir);

            var note = "";
            if (options.DeleteData)
            {
                _files.DeleteFile(paths.EnvBackup);
            }
            else
            {
                // Данные остаются в томе Docker, а пароль к ним лежит в .env: без копии после переустановки их не открыть.
                var env = SitePaths.EnvFile(serverDirs[^1]);
                if (!_files.FileExists(env))
                    env = SitePaths.EnvFile(serverDirs[0]);
                if (_files.FileExists(env))
                {
                    try
                    {
                        _files.CopyFile(env, paths.EnvBackup, overwrite: true);
                        note = "Настройки сайта сохранены вместе с данными.";
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        return (UninstallStageState.Failed,
                            "Не удалось сохранить настройки сайта, поэтому папку сайта я не трогал. " + ex.Message);
                    }
                }
            }

            foreach (var dir in serverDirs)
                _files.DeleteDirectory(dir);
            _files.DeleteFile(paths.InstallJson);
            return (UninstallStageState.Done, note);
        }

        private (UninstallStageState, string) ScheduleProgramRemoval(UninstallOptions options)
        {
            if (!TryNormalize(options.AppDir, out var appDir) || !IsSafeAppDir(appDir))
            {
                return (UninstallStageState.Failed,
                    "Папку программы не удалось определить безопасно. Удалите её вручную через проводник.");
            }

            var folders = new List<string> { appDir };
            if (TryNormalize(_loc.UninstallCopyDir, out var copyDir) && SamePath(_loc.CurrentExeDir, copyDir) &&
                !SamePath(copyDir, appDir))
                folders.Add(copyDir);

            _runner.StartDetached("cmd.exe", UninstallCommands.BuildDeleteFoldersArguments(folders), _loc.TempRoot);
            return (UninstallStageState.Done, "Папка программы исчезнет через несколько секунд после закрытия окна.");
        }

        /// <summary>Папку можно удалять, только если это не системное место и в ней лежит HQStudio.exe.</summary>
        public bool IsSafeAppDir(string appDir)
        {
            if (!TryNormalize(appDir, out var full))
                return false;
            if (full.Contains('"') || full.Contains('%'))
                return false;
            var root = Path.GetPathRoot(full);
            if (root == null || SamePath(root, full))
                return false;
            if (_loc.ProtectedFolders.Any(p => p.Length > 0 && SamePath(p, full)))
                return false;
            return _files.FileExists(Path.Combine(full, UninstallLocations.MainExe));
        }

        private static bool TryNormalize(string path, out string full)
        {
            full = "";
            if (string.IsNullOrWhiteSpace(path))
                return false;
            try
            {
                full = Path.GetFullPath(path).TrimEnd('\\', '/');
                return Path.IsPathRooted(full);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

        private static bool IsInside(string parent, string child)
        {
            if (!TryNormalize(parent, out var p) || !TryNormalize(child, out var c))
                return false;
            return c.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Запускает удаление из копии программы во временной папке: работающий exe сам себя удалить не может.
    /// </summary>
    public sealed class UninstallLauncher
    {
        private readonly ISiteFiles _files;
        private readonly ISiteProcessRunner _runner;
        private readonly string _tempRoot;

        public UninstallLauncher(ISiteFiles files, ISiteProcessRunner runner, string tempRoot)
        {
            _files = files;
            _runner = runner;
            _tempRoot = tempRoot;
        }

        public static UninstallLauncher CreateDefault() =>
            new(new SiteFiles(), new SiteProcessRunner(), Path.GetTempPath());

        /// <summary>Копирует программу и запускает копию с <c>--uninstall</c>. Вызывающий после успеха закрывает приложение.</summary>
        public SiteOperationResult Launch(string exePath)
        {
            try
            {
                var appDir = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(appDir) || !_files.FileExists(exePath))
                {
                    return SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.Other, "Не удалось начать удаление",
                        "Не получилось найти файл программы.", ""));
                }

                var target = Path.Combine(_tempRoot, UninstallLocations.UninstallCopyFolder);
                try
                {
                    _files.DeleteDirectory(target);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Остатки прошлой попытки перезапишем поверх.
                }

                // Обычная публикация - один exe; у сборки из нескольких файлов копируем всю папку программы.
                var multiFile = _files.FileExists(Path.Combine(appDir, "HQStudio.dll"));
                var sources = multiFile
                    ? _files.ListFiles(appDir)
                    : new[] { exePath };
                foreach (var source in sources)
                    _files.CopyFile(source, Path.Combine(target, Path.GetFileName(source)), overwrite: true);

                _runner.StartDetached(Path.Combine(target, Path.GetFileName(exePath)),
                    UninstallCommands.BuildUninstallLaunchArguments(appDir), target);
                return SiteOperationResult.Ok("Запускаю удаление.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
            {
                return SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.Other, "Не удалось начать удаление",
                    "Не получилось подготовить удаление. Закройте лишние программы и попробуйте ещё раз.", ex.Message));
            }
        }
    }

    public sealed record UninstallLaunchArgs(string? AppDir);

    public static class UninstallArguments
    {
        /// <summary>Распознаёт <c>--uninstall [--app-dir путь]</c> (и форму <c>--app-dir=путь</c>).</summary>
        public static bool TryParse(IReadOnlyList<string> args, out UninstallLaunchArgs? result)
        {
            result = null;
            var uninstall = false;
            string? appDir = null;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    uninstall = true;
                }
                else if (string.Equals(arg, "--app-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
                {
                    appDir = args[++i];
                }
                else if (arg.StartsWith("--app-dir=", StringComparison.OrdinalIgnoreCase))
                {
                    appDir = arg["--app-dir=".Length..];
                }
            }

            if (!uninstall)
                return false;

            result = new UninstallLaunchArgs(string.IsNullOrWhiteSpace(appDir) ? null : appDir.Trim().Trim('"'));
            return true;
        }
    }
}
