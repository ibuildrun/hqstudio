using System.Windows;
using System.Windows.Media;

namespace HQStudio.Services
{
    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        public bool IsDark { get; private set; } = true;

        public void ApplyTheme(bool isDark)
        {
            IsDark = isDark;
            var app = Application.Current;

            if (isDark)
            {
                // Dark Theme Colors
                app.Resources["BgPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(10, 10, 10));
                app.Resources["BgSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(13, 13, 13));
                app.Resources["BgCardBrush"] = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                app.Resources["BgInputBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["BgHoverBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                app.Resources["BgIconBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                app.Resources["BgAvatarBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["BgDialogBrush"] = new SolidColorBrush(Color.FromRgb(10, 10, 10));
                app.Resources["BgDialogHeaderBrush"] = new SolidColorBrush(Color.FromRgb(13, 13, 13));
                app.Resources["BgLoginBrush"] = new SolidColorBrush(Color.FromRgb(10, 10, 10));
                app.Resources["BorderLoginBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["BgOverlayBrush"] = new SolidColorBrush(Color.FromArgb(230, 10, 10, 10));
                app.Resources["BorderDefaultBrush"] = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                app.Resources["BorderLightBrush"] = new SolidColorBrush(Color.FromRgb(58, 58, 58));
                app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
                app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(176, 176, 176));
                app.Resources["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(112, 112, 112));
                app.Resources["AccentBrush"] = new SolidColorBrush(Colors.White);
                app.Resources["BtnPrimaryBgBrush"] = new SolidColorBrush(Colors.White);
                app.Resources["BtnPrimaryFgBrush"] = new SolidColorBrush(Color.FromRgb(10, 10, 10));
                app.Resources["BtnSecondaryBorderBrush"] = new SolidColorBrush(Color.FromRgb(58, 58, 58));
            }
            else
            {
                // Light Theme Colors
                app.Resources["BgPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                app.Resources["BgSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                app.Resources["BgCardBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                app.Resources["BgInputBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                app.Resources["BgHoverBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                app.Resources["BgIconBrush"] = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                app.Resources["BgAvatarBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                app.Resources["BgDialogBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                app.Resources["BgDialogHeaderBrush"] = new SolidColorBrush(Color.FromRgb(248, 248, 248));
                app.Resources["BgLoginBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                app.Resources["BorderLoginBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                app.Resources["BgOverlayBrush"] = new SolidColorBrush(Color.FromArgb(220, 250, 250, 250));
                app.Resources["BorderDefaultBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                app.Resources["BorderLightBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                app.Resources["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["BtnPrimaryBgBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                app.Resources["BtnPrimaryFgBrush"] = new SolidColorBrush(Colors.White);
                app.Resources["BtnSecondaryBorderBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }

            // Save setting
            SettingsService.Instance.Settings.Theme = isDark ? "Dark" : "Light";
            SettingsService.Instance.SaveSettings();
        }

        public void Initialize()
        {
            ApplyTheme(SettingsService.Instance.IsDarkTheme);
        }
    }
}
