using HQStudio.Services;
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
