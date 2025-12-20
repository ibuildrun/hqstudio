using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace HQStudio.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunAnimations();
        }

        private async Task RunAnimations()
        {
            // Logo pulse animation
            var logoScaleUp = new DoubleAnimation(1, 1.05, TimeSpan.FromMilliseconds(300));
            var logoScaleDown = new DoubleAnimation(1.05, 1, TimeSpan.FromMilliseconds(300));
            
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, logoScaleUp);
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, logoScaleUp);
            await Task.Delay(300);
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, logoScaleDown);
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, logoScaleDown);

            // Fade in STUDIO text
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500));
            StudioText.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(300);

            // Fade in tagline
            TaglineText.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(200);

            // Show loading text
            LoadingText.BeginAnimation(OpacityProperty, fadeIn);

            // Animate loading bar
            var loadingSteps = new[] 
            { 
                ("Инициализация...", 60),
                ("Загрузка данных...", 120),
                ("Подготовка интерфейса...", 200),
                ("Почти готово...", 280),
                ("Добро пожаловать!", 300)
            };

            foreach (var (text, width) in loadingSteps)
            {
                LoadingText.Text = text;
                var widthAnim = new DoubleAnimation(LoadingBar.Width, width, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                LoadingBar.BeginAnimation(WidthProperty, widthAnim);
                await Task.Delay(500);
            }

            // Final glow pulse
            var glowUp = new DoubleAnimation(50, 80, TimeSpan.FromMilliseconds(300));
            var glowDown = new DoubleAnimation(80, 30, TimeSpan.FromMilliseconds(500));
            LogoGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, glowUp);
            await Task.Delay(300);
            LogoGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, glowDown);
            await Task.Delay(400);

            // Open login window
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
