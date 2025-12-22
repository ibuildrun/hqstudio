using System.Timers;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис автоматической синхронизации данных между клиентами
    /// </summary>
    public class DataSyncService
    {
        private static DataSyncService? _instance;
        public static DataSyncService Instance => _instance ??= new DataSyncService();

        private readonly System.Timers.Timer _syncTimer;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        
        private bool _isEnabled;

        /// <summary>
        /// Событие для обновления заказов
        /// </summary>
        public event EventHandler? OrdersChanged;
        
        /// <summary>
        /// Событие для обновления клиентов
        /// </summary>
        public event EventHandler? ClientsChanged;
        
        /// <summary>
        /// Событие для обновления заявок
        /// </summary>
        public event EventHandler? CallbacksChanged;

        private DataSyncService()
        {
            _syncTimer = new System.Timers.Timer(30000); // 30 секунд - не слишком часто
            _syncTimer.Elapsed += OnSyncTimerElapsed;
            _syncTimer.AutoReset = true;
        }

        /// <summary>
        /// Запустить автосинхронизацию
        /// </summary>
        public void Start()
        {
            if (_settings.UseApi && _apiService.IsConnected)
            {
                _isEnabled = true;
                _syncTimer.Start();
                System.Diagnostics.Debug.WriteLine("DataSyncService started");
            }
        }

        /// <summary>
        /// Остановить автосинхронизацию
        /// </summary>
        public void Stop()
        {
            _isEnabled = false;
            _syncTimer.Stop();
            System.Diagnostics.Debug.WriteLine("DataSyncService stopped");
        }

        private void OnSyncTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isEnabled || !_settings.UseApi || !_apiService.IsConnected)
                return;

            // Уведомляем подписчиков о необходимости обновить данные
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                OrdersChanged?.Invoke(this, EventArgs.Empty);
                ClientsChanged?.Invoke(this, EventArgs.Empty);
                CallbacksChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Принудительно запросить обновление всех данных
        /// </summary>
        public void RequestFullSync()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                OrdersChanged?.Invoke(this, EventArgs.Empty);
                ClientsChanged?.Invoke(this, EventArgs.Empty);
                CallbacksChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
