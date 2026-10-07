using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Reflection;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService = SettingsService.Instance;
        private readonly DataService _dataService = DataService.Instance;

        private bool _isDarkTheme;
        private bool _showSplash;

        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            }
        }

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

        /// <summary>State and commands of the "Updates" section.</summary>
        public UpdateViewModel Updates { get; } = new();

        public string CurrentUserName => _dataService.CurrentUser?.DisplayName ?? "Гость";
        public string CurrentUserRole => _dataService.CurrentUser?.Role == "Admin" ? "Администратор" : "Работник";
        public int TotalClients => _dataService.Clients.Count;
        public int TotalOrders => _dataService.Orders.Count;
        public int TotalServices => _dataService.Services.Count;

        public ICommand ResetDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand OpenUpdateDetailsCommand { get; }

        public SettingsViewModel()
        {
            _isDarkTheme = _settingsService.IsDarkTheme;
            _showSplash = _settingsService.Settings.ShowSplash;

            ResetDataCommand = new RelayCommand(_ => ResetData());
            ExportDataCommand = new RelayCommand(_ => ExportData());
            OpenUpdateDetailsCommand = new RelayCommand(_ => UpdateDialog.ShowFor());
        }

        private void ResetData()
        {
            var result = ConfirmDialog.Show(
                "Сброс данных",
                "Сбросить все данные к демо-версии?\nВсе текущие данные будут удалены.",
                ConfirmDialog.DialogType.Warning,
                "Сбросить", "Отмена");

            if (result)
            {
                _dataService.ResetToDemo();
                ConfirmDialog.ShowInfo("Готово", "Данные сброшены! Перезапустите приложение.", ConfirmDialog.DialogType.Success);
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
