using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HQStudio.ViewModels;
using HQStudio.Views;
using HQStudio.Views.Dialogs;

namespace HQStudio.Services.Site
{
    /// <summary>
    /// Служебный режим <c>--render-site-pages папка</c>: рисует страницу «Сайт», диалоги и окно удаления
    /// в PNG на выдуманных данных. Без сети, Docker, реестра и настоящих файлов.
    /// </summary>
    public static class SiteDevRender
    {
        public static bool TryParse(string[] args, out string outputDir)
        {
            outputDir = "";
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "--render-site-pages", StringComparison.OrdinalIgnoreCase))
                {
                    outputDir = args[i + 1];
                    return true;
                }
            }
            return false;
        }

        public static async Task RunAsync(string outputDir)
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                Application.Current.MainWindow?.Close();
                await RenderSitePagesAsync(outputDir);
                await RenderNavSampleAsync(outputDir);
                await RenderDialogsAsync(outputDir);
                await RenderUninstallAsync(outputDir);

                ApplyLightBrushes();
                _suffix = "-light";
                await RenderSitePagesAsync(outputDir);
                await RenderDialogsAsync(outputDir);
                await RenderUninstallAsync(outputDir);
                File.WriteAllText(Path.Combine(outputDir, "done.txt"), "ok");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(outputDir, "render-error.txt"), ex.ToString());
                Environment.Exit(1);
            }
        }

        // ------------------------------------------------------------------ страница

        private static async Task RenderSitePagesAsync(string dir)
        {
            var cases = new (string Name, SiteSnapshot Snapshot, string? Busy, SiteFailure? Failure, double Width, double Height)[]
            {
                ("site-running", Snapshot(Running(true), true), null, null, 1170, 1180),
                ("site-running-narrow", Snapshot(Running(true), true), null, null, 830, 1260),
                ("site-starting", Snapshot(Starting(), false), "Жду, пока сайт ответит", null, 1170, 1180),
                ("site-docker-down", Snapshot(DockerDown(), false), null, null, 1170, 1180),
                ("site-stopped", Snapshot(Stopped(), false), null, null, 1170, 1180),
                ("site-error", Snapshot(Broken(), true), null,
                    new SiteFailure(SiteFailureKind.PortBusy, "Порт занят",
                        "Сайт не смог занять свой порт (порт 8080): его уже использует другая программа. Закройте её или перезагрузите компьютер и попробуйте ещё раз.",
                        "Error response from daemon: driver failed programming external connectivity on endpoint hqstudio-proxy-1:\nBind for 127.0.0.1:8080 failed: port is already allocated"),
                    1170, 1260),
                ("site-not-installed", Snapshot(NotInstalled(), false), null, null, 1170, 620)
            };

            foreach (var item in cases)
            {
                var service = new PreviewService { Snapshot = item.Snapshot };
                var vm = new SiteViewModel(service, new PreviewShell(), new PreviewDialogs(), new PreviewUninstallHost(),
                    new PreviewNotifier());
                vm.ShowPreview(item.Snapshot, item.Busy, item.Busy != null, item.Failure);

                var view = new SiteView { DataContext = vm };
                var host = new Window
                {
                    Width = item.Width,
                    Height = item.Height,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize
                };
                host.SetResourceReference(Control.BackgroundProperty, "BgPrimaryBrush");
                host.Content = new Border { Padding = new Thickness(32), Child = view };
                ShowOffscreen(host);

                if (item.Failure != null && view.FindName("DetailsToggle") is ToggleButton toggle)
                    toggle.IsChecked = true;

                await CaptureAsync(host, Path.Combine(dir, item.Name + ".png"));
                vm.Deactivate();
                host.Close();
            }
        }

        // Пункт «Сайт» рядом с соседними: проверка, что значок рисуется шрифтом Windows.
        private static async Task RenderNavSampleAsync(string dir)
        {
            var panel = new StackPanel { Width = 248, Margin = new Thickness(16) };
            foreach (var text in new[] { "◈  Сайт", "↻  Обновления" })
            {
                var button = new Button { Content = text };
                button.SetResourceReference(FrameworkElement.StyleProperty, "BtnNav");
                panel.Children.Add(button);
            }
            var host = new Window { Width = 280, Height = 130, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize };
            host.SetResourceReference(Control.BackgroundProperty, "BgSecondaryBrush");
            host.Content = panel;
            ShowOffscreen(host);
            await CaptureAsync(host, Path.Combine(dir, "nav-sample.png"));
            host.Close();
        }

        // ------------------------------------------------------------------ диалоги

        private static async Task RenderDialogsAsync(string dir)
        {
            // Ключи: всё сохранено
            await CaptureKeysAsync(dir, "keys-saved", new SiteKeysState(true, true, "hq-studio"), null);

            // Ключи: пусто, имя адреса введено с ошибкой
            await CaptureKeysAsync(dir, "keys-empty", new SiteKeysState(false, false, ""), (vm, _) =>
            {
                vm.Subdomain = "Мой Сайт";
                return Task.CompletedTask;
            });

            // Ключи: идёт применение
            await CaptureKeysAsync(dir, "keys-busy", new SiteKeysState(true, false, ""), async (vm, service) =>
            {
                vm.ChangeTunaCommand.Execute(null);
                vm.TunaInput = "tuna-token-123";
                vm.Subdomain = "hq-studio";
                var gate = new TaskCompletionSource<SiteOperationResult>();
                service.ApplyGate = gate;
                vm.SaveCommand.Execute(null);
                await Task.Delay(300);
            });

            // Ключи: результат с ошибкой
            await CaptureKeysAsync(dir, "keys-result", new SiteKeysState(true, false, ""), async (vm, service) =>
            {
                vm.ChangeTunaCommand.Execute(null);
                vm.TunaInput = "tuna-token-123";
                var gate = new TaskCompletionSource<SiteOperationResult>();
                service.ApplyGate = gate;
                vm.SaveCommand.Execute(null);
                await Task.Delay(200);
                gate.SetResult(SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.NoInternet, "Нет интернета",
                    "Настройки сохранены, но применить их не получилось. Не получилось скачать нужные файлы. Проверьте подключение к интернету и попробуйте ещё раз.",
                    "Error response from daemon: Get \"https://registry-1.docker.io/v2/\": dial tcp: lookup registry-1.docker.io: no such host")));
                await Task.Delay(300);
            });

            // Логи
            var logService = new PreviewService { Snapshot = Snapshot(Running(true), true), LogText = PreviewLog };
            var logsVm = new SiteLogsViewModel(logService, new PreviewShell(), new PreviewNotifier());
            var logs = new SiteLogsDialog(logsVm);
            ShowOffscreen(logs);
            await CaptureAsync(logs, Path.Combine(dir, "logs.png"));
            logs.Close();
        }

        private static async Task CaptureKeysAsync(string dir, string name, SiteKeysState state,
            Func<SiteKeysViewModel, PreviewService, Task>? prepare)
        {
            var service = new PreviewService { Keys = state, Snapshot = Snapshot(Running(true), true) };
            var vm = new SiteKeysViewModel(service, new PreviewShell());
            var dialog = new SiteKeysDialog(vm);
            ShowOffscreen(dialog);
            if (prepare != null)
                await prepare(vm, service);
            await CaptureAsync(dialog, Path.Combine(dir, name + ".png"));
            dialog.Close();
        }

        // ------------------------------------------------------------------ окно удаления

        private static async Task RenderUninstallAsync(string dir)
        {
            await CaptureUninstallAsync(dir, "uninstall-confirm", false, false, async (vm, _) => await Task.CompletedTask);
            await CaptureUninstallAsync(dir, "uninstall-confirm-warning", false, false, (vm, _) =>
            {
                vm.DeleteData = true;
                return Task.CompletedTask;
            });
            await CaptureUninstallAsync(dir, "uninstall-running", false, true, async (vm, release) =>
            {
                vm.StartCommand.Execute(null);
                await Task.Delay(400);
            });
            await CaptureUninstallAsync(dir, "uninstall-done", false, false, async (vm, _) =>
            {
                await vm.RunAsync();
            });
            await CaptureUninstallAsync(dir, "uninstall-done-warning", true, false, async (vm, _) =>
            {
                vm.DeleteData = true;
                await vm.RunAsync();
            });
        }

        private static UninstallViewModel CreatePreviewViewModel(bool dockerDown, Task? gate)
        {
            var runner = new PreviewRunner { DockerDown = dockerDown, Gate = gate };
            var appDir = @"C:\Program Files\HQ Studio";
            var files = new PreviewFiles(Path.Combine(appDir, "HQStudio.exe"));
            var paths = new SitePaths(@"C:\Users\demo\AppData\Local\HQStudio");
            var locations = new UninstallLocations(appDir, appDir, @"C:\Users\demo\AppData\Local\Temp\",
                @"C:\Users\demo\Start Menu\Programs", @"C:\Users\demo\Desktop", paths,
                new[] { @"C:\Windows", @"C:\Program Files" });
            var service = new UninstallService(files, runner, new PreviewLocator(), new PreviewInstallStore(),
                new PreviewRegistry(), locations);
            return new UninstallViewModel(service, appDir);
        }

        /// <summary>Модель для стартового окна режима предпросмотра.</summary>
        public static UninstallViewModel CreateUninstallPreview() => CreatePreviewViewModel(false, null);

        private static async Task CaptureUninstallAsync(string dir, string name, bool dockerDown, bool holdFirstStage,
            Func<UninstallViewModel, TaskCompletionSource, Task> prepare)
        {
            var gate = new TaskCompletionSource();
            var vm = CreatePreviewViewModel(dockerDown, holdFirstStage ? gate.Task : null);
            var window = new UninstallWindow(vm);
            ShowOffscreen(window);
            await prepare(vm, gate);
            await CaptureAsync(window, Path.Combine(dir, name + ".png"));
            gate.TrySetResult();
            await Task.Delay(300);
            window.Close();
        }

        // ------------------------------------------------------------------ рисование

        private static void ShowOffscreen(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -10000;
            window.Top = -10000;
            window.ShowInTaskbar = false;
            window.ShowActivated = false;
            window.Show();
        }

        private static async Task CaptureAsync(Window window, string path)
        {
            await Task.Delay(500);
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

            var width = (int)Math.Ceiling(window.ActualWidth);
            var height = (int)Math.Ceiling(window.ActualHeight);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            // Серая подложка: прозрачные углы окон иначе выглядят чёрными.
            var backdrop = new DrawingVisual();
            using (var dc = backdrop.RenderOpen())
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(70, 70, 78)), null, new Rect(0, 0, width, height));
            bitmap.Render(backdrop);
            bitmap.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.ChangeExtension(path, null) + _suffix + ".png");
            encoder.Save(stream);
        }

        private static string _suffix = "";

        // Копия значений светлой темы из ThemeService: сам ThemeService сохраняет настройки пользователя, здесь это лишнее.
        private static void ApplyLightBrushes()
        {
            var r = Application.Current.Resources;
            SolidColorBrush Rgb(byte v) => new(Color.FromRgb(v, v, v));
            r["BgPrimaryBrush"] = Rgb(250);
            r["BgSecondaryBrush"] = Rgb(245);
            r["BgCardBrush"] = Rgb(255);
            r["BgInputBrush"] = Rgb(245);
            r["BgHoverBrush"] = Rgb(240);
            r["BgDialogBrush"] = Rgb(255);
            r["BgDialogHeaderBrush"] = Rgb(248);
            r["BgOverlayBrush"] = new SolidColorBrush(Color.FromArgb(220, 250, 250, 250));
            r["BorderDefaultBrush"] = Rgb(220);
            r["BorderLightBrush"] = Rgb(200);
            r["TextPrimaryBrush"] = Rgb(26);
            r["TextSecondaryBrush"] = Rgb(80);
            r["TextMutedBrush"] = Rgb(128);
            r["BtnPrimaryBgBrush"] = Rgb(26);
            r["BtnPrimaryFgBrush"] = new SolidColorBrush(Colors.White);
            r["BtnSecondaryBorderBrush"] = Rgb(200);
        }

        // ------------------------------------------------------------------ выдуманные данные

        private static SiteStatusInput Input(IReadOnlyList<ComposeServiceEntry>? entries, bool tunnel,
            SiteDockerState docker = SiteDockerState.Running, bool? health = true, int failures = 0,
            SiteOperation op = SiteOperation.None) =>
            new(true, null, docker, entries, null, tunnel, health, failures, op);

        private static SiteStatusInput Running(bool tunnel) => Input(new[]
        {
            E("db", "running", "healthy", "Up 3 hours (healthy)"),
            E("api", "running", "healthy", "Up 3 hours (healthy)"),
            E("web", "running", "healthy", "Up 3 hours (healthy)"),
            E("proxy", "running", "", "Up 3 hours"),
            E("tuna", "running", "", "Up 3 hours")
        }, tunnel);

        private static SiteStatusInput Starting() => Input(new[]
        {
            E("db", "running", "healthy", "Up 40 seconds (healthy)"),
            E("api", "running", "starting", "Up 12 seconds (health: starting)"),
            E("web", "created", "", "Created"),
        }, false, health: null, op: SiteOperation.Starting);

        private static SiteStatusInput DockerDown() =>
            new(true, null, SiteDockerState.NotRunning, null, null, true, null, 0, SiteOperation.None);

        private static SiteStatusInput Stopped() => Input(new[]
        {
            E("db", "exited", "", "Exited (0) 2 hours ago"),
            E("api", "exited", "", "Exited (0) 2 hours ago"),
            E("web", "exited", "", "Exited (0) 2 hours ago"),
            E("proxy", "exited", "", "Exited (0) 2 hours ago")
        }, false, health: null);

        private static SiteStatusInput Broken() => Input(new[]
        {
            E("db", "running", "healthy", "Up 3 hours (healthy)"),
            E("api", "running", "healthy", "Up 3 hours (healthy)"),
            E("web", "exited", "", "Exited (1) 2 minutes ago", 1),
            E("proxy", "running", "", "Up 3 hours")
        }, true, health: false, failures: 3);

        private static SiteStatusInput NotInstalled() =>
            new(false, null, SiteDockerState.Unknown, null, null, false, null, 0, SiteOperation.None);

        private static ComposeServiceEntry E(string service, string state, string health, string status, int? exit = null) =>
            new(service, state, health, status, exit);

        private static SiteSnapshot Snapshot(SiteStatusInput input, bool hasPublic)
        {
            var services = SiteStatusEvaluator.BuildServices(input);
            var overview = SiteStatusEvaluator.Evaluate(input, services);
            return new SiteSnapshot(input.Installed, input.Docker, overview, services,
                input.Installed ? "1.19.6" : "", input.Installed ? "http://localhost:8080" : "",
                hasPublic ? "https://hq-studio.ru.tuna.am" : null, input.TunnelConfigured, null);
        }

        private const string PreviewLog =
            "api-1  | info: Microsoft.Hosting.Lifetime[14]\n" +
            "api-1  |       Now listening on: http://[::]:5000\n" +
            "api-1  | info: Microsoft.Hosting.Lifetime[0]\n" +
            "api-1  |       Application started. Press Ctrl+C to shut down.\n" +
            "api-1  | info: Microsoft.AspNetCore.Hosting.Diagnostics[1]\n" +
            "api-1  |       Request starting HTTP/1.1 GET http://localhost:5000/api/health - -\n" +
            "api-1  | info: Microsoft.AspNetCore.Hosting.Diagnostics[2]\n" +
            "api-1  |       Request finished HTTP/1.1 GET http://localhost:5000/api/health - 200 - application/json 1.4ms\n" +
            "api-1  | info: HQStudio.API.Controllers.AuthController[0]\n" +
            "api-1  |       Login ok: admin, token=***\n" +
            "api-1  | warn: HQStudio.API.Services.EmailService[0]\n" +
            "api-1  |       Password=***; Host=db;Database=hqstudio\n" +
            "api-1  | info: Microsoft.AspNetCore.Hosting.Diagnostics[1]\n" +
            "api-1  |       Request starting HTTP/1.1 GET http://localhost:5000/api/orders - -\n";
    }

    // ---------------------------------------------------------------------- заглушки для предпросмотра

    internal sealed class PreviewService : ISiteService
    {
        public SiteSnapshot Snapshot { get; set; } = SiteSnapshot.Checking;
        public SiteKeysState Keys { get; set; } = new(false, false, "");
        public string LogText { get; set; } = "";
        public TaskCompletionSource<SiteOperationResult>? ApplyGate { get; set; }

        public Task<SiteSnapshot> RefreshAsync(SiteOperation operation, CancellationToken ct) => Task.FromResult(Snapshot);
        public Task<SiteOperationResult> StartAsync(Action<string>? status, CancellationToken ct) => Task.FromResult(SiteOperationResult.Ok("Сайт запущен."));
        public Task<SiteOperationResult> StopAsync(Action<string>? status, CancellationToken ct) => Task.FromResult(SiteOperationResult.Ok("Сайт остановлен."));
        public Task<SiteOperationResult> RestartAsync(Action<string>? status, CancellationToken ct) => Task.FromResult(SiteOperationResult.Ok("Сайт перезапущен."));
        public Task<SiteOperationResult> StartDockerAsync(Action<string>? status, CancellationToken ct) => Task.FromResult(SiteOperationResult.Ok("Docker запущен."));

        public Task<SiteOperationResult> ApplyKeysAsync(SiteKeysUpdate update, Action<string>? status, CancellationToken ct)
        {
            status?.Invoke("Применяю настройки");
            return ApplyGate?.Task ?? Task.FromResult(SiteOperationResult.Ok("Настройки сохранены и применены."));
        }

        public Task<SiteLogsResult> GetLogsAsync(string service, CancellationToken ct) =>
            Task.FromResult(new SiteLogsResult(true, LogText, null));

        public SiteKeysState? ReadKeysState() => Keys;
    }

    internal sealed class PreviewShell : ISiteShell
    {
        public bool OpenUrl(string url) => true;
        public bool CopyText(string text) => true;
    }

    internal sealed class PreviewNotifier : ISiteNotifier
    {
        public void Success(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    internal sealed class PreviewDialogs : ISiteDialogs
    {
        public void ShowKeys(ISiteService service) { }
        public void ShowLogs(ISiteService service) { }
        public void ShowUpdates() { }
        public bool ConfirmUninstall() => false;
    }

    internal sealed class PreviewUninstallHost : ISiteUninstallHost
    {
        public SiteOperationResult LaunchUninstall() => SiteOperationResult.Ok("");
        public void ShutdownApplication() { }
    }

    internal sealed class PreviewLocator : ISiteDockerLocator
    {
        public string? FindDocker() => "docker";
        public string? FindDockerDesktop() => "Docker Desktop.exe";
    }

    internal sealed class PreviewInstallStore : ISiteInstallStore
    {
        public SiteInstallReadResult Read() => new(
            new SiteInstallInfo(@"C:\Users\demo\AppData\Local\HQStudio\server", "http://localhost:8080", "http://localhost:8080", "ibuildrun/hqstudio"),
            null);
    }

    internal sealed class PreviewRegistry : IUninstallRegistry
    {
        public void DeleteUninstallKey(string keyName) { }
    }

    /// <summary>Файлы в памяти: удаление в предпросмотре ничего не трогает на диске.</summary>
    internal sealed class PreviewFiles : ISiteFiles
    {
        private readonly HashSet<string> _files;

        public PreviewFiles(params string[] files) => _files = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);

        public bool FileExists(string path) => _files.Contains(path) || path.EndsWith(".env", StringComparison.OrdinalIgnoreCase) ||
                                                path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
        public bool DirectoryExists(string path) => true;
        public string ReadAllText(string path) => "HQ_PORT=8080\nTUNA_TOKEN=\n";
        public void WriteAllTextAtomic(string path, string content) { }
        public void DeleteFile(string path) { }
        public void DeleteDirectory(string path) { }
        public void CopyFile(string source, string destination, bool overwrite) { }
        public void CreateDirectory(string path) { }
        public IReadOnlyList<string> ListFiles(string directory) => Array.Empty<string>();
    }

    internal sealed class PreviewRunner : ISiteProcessRunner
    {
        public bool DockerDown { get; set; }
        public Task? Gate { get; set; }

        public async Task<SiteProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, Action<string>? onLine,
            CancellationToken ct)
        {
            if (Gate != null)
                await Gate;
            return DockerDown
                ? new SiteProcessResult(1, "", "error during connect: open //./pipe/docker_engine: The system cannot find the file specified.")
                : new SiteProcessResult(0, "", "");
        }

        public void StartDetached(string fileName, string arguments, string? workingDirectory)
        {
            // Предпросмотр никогда не запускает настоящие процессы.
        }
    }
}
