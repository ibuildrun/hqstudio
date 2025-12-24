using HQStudio.Services;
using HQStudio.Views;
using System.Windows;

namespace HQStudio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize theme
            ThemeService.Instance.Initialize();
            
            // Screenshot mode - пропускаем авторизацию и делаем скриншоты
            if (ScreenshotService.IsScreenshotMode)
            {
                _ = RunScreenshotModeAsync();
                return;
            }
            
            // Start data sync service if API is enabled
            if (SettingsService.Instance.UseApi)
            {
                _ = InitializeApiAsync();
            }
            
            // Session is started after login in LoginViewModel
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
            Console.WriteLine("Screenshot mode enabled");
            
            // 1. Скриншот окна входа
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            await Task.Delay(1000);
            ScreenshotService.CaptureWindow(loginWindow, "01-login.png");
            
            // 2. Открываем главное окно напрямую (без авторизации)
            var mainWindow = new MainWindow();
            mainWindow.Show();
            loginWindow.Close();
            
            await Task.Delay(1500); // Ждём загрузки данных
            
            // 3. Скриншот Dashboard
            ScreenshotService.CaptureWindow(mainWindow, "02-dashboard.png");
            
            // 4. Навигация по разделам и скриншоты
            if (mainWindow.DataContext is ViewModels.MainViewModel vm)
            {
                // Заказы
                vm.NavigateCommand.Execute("Orders");
                await Task.Delay(1000);
                ScreenshotService.CaptureWindow(mainWindow, "03-orders.png");
                
                // Клиенты
                vm.NavigateCommand.Execute("Clients");
                await Task.Delay(1000);
                ScreenshotService.CaptureWindow(mainWindow, "04-clients.png");
                
                // Услуги
                vm.NavigateCommand.Execute("Services");
                await Task.Delay(1000);
                ScreenshotService.CaptureWindow(mainWindow, "05-services.png");
                
                // Сотрудники
                vm.NavigateCommand.Execute("Staff");
                await Task.Delay(1000);
                ScreenshotService.CaptureWindow(mainWindow, "06-staff.png");
                
                // Настройки
                vm.NavigateCommand.Execute("Settings");
                await Task.Delay(500);
                ScreenshotService.CaptureWindow(mainWindow, "07-settings.png");
                
                // Переключаем тему и делаем скриншот
                ThemeService.Instance.ApplyTheme(false); // Light theme
                await Task.Delay(500);
                
                vm.NavigateCommand.Execute("Dashboard");
                await Task.Delay(500);
                ScreenshotService.CaptureWindow(mainWindow, "08-dashboard-light.png");
            }
            
            Console.WriteLine("Screenshots completed!");
            ScreenshotService.ExitAfterDelay(500);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop data sync
            DataSyncService.Instance.Stop();
            
            // End session when app closes
            SessionService.Instance.EndSessionAsync().Wait();
            base.OnExit(e);
        }
    }
}
