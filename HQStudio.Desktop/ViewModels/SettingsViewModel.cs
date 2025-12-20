using HQStudio.Services;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService = SettingsService.Instance;
        private readonly DataService _dataService = DataService.Instance;
        private readonly UpdateService _updateService = UpdateService.Instance;
        
        private bool _isDarkTheme;
        private bool _showSplash;
        private string _updateStatus = "Нажмите для проверки обновлений";
        private bool _isUpdateAvailable;
        private string _newVersion = "";
        private string _updateReleaseNotes = "";

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    ThemeService.Instance.ApplyTheme(value);
                }
            }
        }

        public bool ShowSplash
        {
            get => _showSplash;
            set
            {
                if (SetProperty(ref _showSplash, value))
                {
                    _settingsService.Settings.ShowSplash = value;
                    _settingsService.SaveSettings();
                }
            }
        }

        public string UpdateStatus
        {
            get => _updateStatus;
            set => SetProperty(ref _updateStatus, value);
        }

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }

        public string NewVersion
        {
            get => _newVersion;
            set => SetProperty(ref _newVersion, value);
        }

        public string UpdateReleaseNotes
        {
            get => _updateReleaseNotes;
            set => SetProperty(ref _updateReleaseNotes, value);
        }

        public string CurrentUserName => _dataService.CurrentUser?.DisplayName ?? "Гость";
        public string CurrentUserRole => _dataService.CurrentUser?.Role == "Admin" ? "Администратор" : "Работник";
        public int TotalClients => _dataService.Clients.Count;
        public int TotalOrders => _dataService.Orders.Count;
        public int TotalServices => _dataService.Services.Count;

        public ICommand ResetDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand InstallUpdateCommand { get; }

        public SettingsViewModel()
        {
            _isDarkTheme = _settingsService.IsDarkTheme;
            _showSplash = _settingsService.Settings.ShowSplash;
            
            ResetDataCommand = new RelayCommand(_ => ResetData());
            ExportDataCommand = new RelayCommand(_ => ExportData());
            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            InstallUpdateCommand = new RelayCommand(async _ => await InstallUpdateAsync());

            _updateService.UpdateStatusChanged += (s, msg) => UpdateStatus = msg;
            _updateService.UpdateAvailable += OnUpdateAvailable;
        }

        private void OnUpdateAvailable(object? sender, UpdateInfo update)
        {
            IsUpdateAvailable = true;
            NewVersion = update.Version;
            UpdateReleaseNotes = update.ReleaseNotes;
        }

        private async Task CheckUpdatesAsync()
        {
            IsUpdateAvailable = false;
            UpdateStatus = "Проверка обновлений...";
            
            var update = await _updateService.CheckForUpdatesAsync();
            
            if (update == null)
            {
                UpdateStatus = $"У вас последняя версия ({_updateService.CurrentVersion})";
            }
        }

        private async Task InstallUpdateAsync()
        {
            var result = MessageBox.Show(
                $"Установить обновление до версии {NewVersion}?\n\nПриложение будет перезапущено.",
                "Установка обновления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _updateService.DownloadAndInstallAsync();
            }
        }

        private void ResetData()
        {
            var result = MessageBox.Show(
                "Сбросить все данные к демо-версии?\nВсе текущие данные будут удалены.",
                "Сброс данных",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _dataService.ResetToDemo();
                MessageBox.Show("Данные сброшены! Перезапустите приложение.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportData()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var hqPath = System.IO.Path.Combine(appDataPath, "HQStudio");
            
            System.Diagnostics.Process.Start("explorer.exe", hqPath);
        }
    }
}
