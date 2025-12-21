using System.IO;
using System.Text.Json;

namespace HQStudio.Services
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark"; // Dark, Light
        public bool ShowSplash { get; set; } = true;
        public string Language { get; set; } = "ru";
        public string ApiUrl { get; set; } = "http://localhost:5000";
        public bool UseApi { get; set; } = true;
    }

    public class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private readonly string _settingsPath;
        public AppSettings Settings { get; private set; } = new();

        public event Action? ThemeChanged;
        public event Action<string>? ApiUrlChanged;

        private SettingsService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HQStudio");
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }

        public void SaveSettings()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }

        public void SetTheme(string theme)
        {
            Settings.Theme = theme;
            SaveSettings();
            ThemeChanged?.Invoke();
        }

        public bool IsDarkTheme => Settings.Theme == "Dark";
        public string ApiUrl => Settings.ApiUrl;
        public bool UseApi => Settings.UseApi;

        public void SetApiUrl(string url)
        {
            if (Settings.ApiUrl != url)
            {
                Settings.ApiUrl = url;
                SaveSettings();
                ApiUrlChanged?.Invoke(url);
            }
        }

        public void SetUseApi(bool useApi)
        {
            Settings.UseApi = useApi;
            SaveSettings();
        }
    }
}
