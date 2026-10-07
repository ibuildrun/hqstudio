using HQStudio.Services;
using HQStudio.Views;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace HQStudio
{
    public partial class App : Application
    {
        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HQStudio", "crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            // Режим удаления (--uninstall) работает без API, сессии, темы и главного окна
            if (Services.Site.UninstallBootstrap.TryHandle(this, e.Args))
            {
                base.OnStartup(e);
                return;
            }

            try
            {
                // Создаём папку для логов
                Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
                Log("=== App Starting ===");
                Log($"Args: {string.Join(" ", e.Args)}");
                Log($"WorkingDir: {Environment.CurrentDirectory}");
                
                base.OnStartup(e);
                
                // Глобальный обработчик ошибок
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                
                Log("Initializing theme...");
                // Initialize theme
                ThemeService.Instance.Initialize();
                Log("Theme initialized");
                
                // Screenshot mode - пропускаем авторизацию и делаем скриншоты
                Log($"Screenshot mode: {ScreenshotService.IsScreenshotMode}");
                if (ScreenshotService.IsScreenshotMode)
                {
                    _ = RunScreenshotModeAsync();
                    return;
                }
                
                // Start data sync service if API is enabled
                Log($"UseApi: {SettingsService.Instance.UseApi}");
                if (SettingsService.Instance.UseApi)
                {
                    Log("Starting API init...");
                    _ = InitializeApiAsync();
                }
                
                Log("Startup complete");
            }
            catch (Exception ex)
            {
                Log($"STARTUP ERROR: {ex}");
                throw;
            }
            
            // Session is started after login in LoginViewModel
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        private async Task InitializeApiAsync()
        {
            // Проверяем подключение к API
            var connected = await ApiService.Instance.CheckConnectionAsync();
            if (connected)
            {
                // Запускаем автосинхронизацию
                DataSyncService.Instance.Start();
            }
        }

        private async Task RunScreenshotModeAsync()
        {
            Console.WriteLine($"Screenshot mode: hidden={ScreenshotService.IsHiddenMode}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // 1. Скриншот окна входа
            var loginWindow = new LoginWindow();
            await ScreenshotService.ShowAndWaitAsync(loginWindow, 500);
            ScreenshotService.CaptureWindow(loginWindow, "01-login.png");
            
            // 2. Открываем главное окно
            var mainWindow = new MainWindow();
            await ScreenshotService.ShowAndWaitAsync(mainWindow, 800);
            loginWindow.Close();
            
            // 3. Скриншот Dashboard
            ScreenshotService.CaptureWindow(mainWindow, "02-dashboard.png");
            
            // 4. Навигация по разделам
            if (mainWindow.DataContext is ViewModels.MainViewModel vm)
            {
                var screens = new[] {
                    ("Orders", "03-orders.png"),
                    ("Clients", "04-clients.png"),
                    ("Services", "05-services.png"),
                    ("Staff", "06-staff.png"),
                    ("Settings", "07-settings.png")
                };
                
                foreach (var (view, filename) in screens)
                {
                    vm.NavigateCommand.Execute(view);
                    await Task.Delay(400);
                    ScreenshotService.CaptureWindow(mainWindow, filename);
                }
                
                // Светлая тема
                ThemeService.Instance.ApplyTheme(false);
                vm.NavigateCommand.Execute("Dashboard");
                await Task.Delay(300);
                ScreenshotService.CaptureWindow(mainWindow, "08-dashboard-light.png");
            }
            
            sw.Stop();
            Console.WriteLine($"Screenshots completed in {sw.ElapsedMilliseconds}ms");
            ScreenshotService.ExitAfterDelay(200);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop data sync
            DataSyncService.Instance.Stop();
            
            // End session when app closes
            SessionService.Instance.EndSessionAsync().Wait();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            try
            {
                Log($"UI Exception: {e.Exception}");
                System.Diagnostics.Debug.WriteLine($"UI Exception: {e.Exception}");

                // Пока открыто окно с вопросом, новые ошибки не плодят новые окна.
                if (!Services.BugReport.CrashPromptGuard.TryEnter()) return;
                try
                {
                    var message = e.Exception.Message.TrimEnd('.');
                    if (message.Length > 200) message = message[..200] + "...";

                    var answer = MessageBox.Show(
                        $"Произошла ошибка: {message}. Отправить отчёт разработчику?",
                        "Ошибка", MessageBoxButton.YesNo, MessageBoxImage.Error);

                    if (answer == MessageBoxResult.Yes)
                        Views.Dialogs.BugReportDialog.ShowFor(MainWindow, e.Exception);
                }
                finally
                {
                    Services.BugReport.CrashPromptGuard.Leave();
                }
            }
            catch (Exception ex)
            {
                Log($"Crash report dialog failed: {ex.Message}");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                Log($"Domain Exception (terminating={e.IsTerminating}): {ex ?? e.ExceptionObject}");
                System.Diagnostics.Debug.WriteLine($"Domain Exception: {ex ?? e.ExceptionObject}");
            }
            catch
            {
                // Обработчик последней надежды не должен падать сам.
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Log($"Task Exception: {e.Exception}");
                System.Diagnostics.Debug.WriteLine($"Task Exception: {e.Exception}");
            }
            catch
            {
                // Логирование не должно мешать пометить исключение как обработанное.
            }
            finally
            {
                e.SetObserved();
            }
        }
    }
}
