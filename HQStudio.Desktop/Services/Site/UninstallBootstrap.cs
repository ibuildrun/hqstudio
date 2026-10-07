using System.IO;
using System.Windows;
using HQStudio.ViewModels;
using HQStudio.Views;

namespace HQStudio.Services.Site
{
    /// <summary>
    /// Ранний запуск особых режимов из App.OnStartup: окно удаления и предпросмотр страниц.
    /// Здесь нет ни API, ни сессии, ни темы, ни главного окна.
    /// </summary>
    public static class UninstallBootstrap
    {
        private static UninstallLaunchArgs? _uninstallArgs;
        private static bool _renderMode;

        /// <returns><c>true</c>, если аргументы обработаны и обычный запуск продолжать не нужно.</returns>
        public static bool TryHandle(Application app, string[] args)
        {
            if (SiteDevRender.TryParse(args, out var outputDir))
            {
                _renderMode = true;
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                UseLaunchWindow(app);
                app.Dispatcher.InvokeAsync(() => SiteDevRender.RunAsync(outputDir));
                return true;
            }

            if (!UninstallArguments.TryParse(args, out var parsed) || parsed == null)
                return false;

            _uninstallArgs = parsed;
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            UseLaunchWindow(app);
            return true;
        }

        // StartupUri нельзя обнулить, поэтому вместо заставки WPF сам создаст окно удаления.
        private static void UseLaunchWindow(Application app) =>
            app.StartupUri = new Uri("Views/UninstallWindow.xaml", UriKind.Relative);

        /// <summary>Модель окна, которое WPF создаёт по StartupUri.</summary>
        public static UninstallViewModel CreateLaunchViewModel()
        {
            if (_renderMode)
                return SiteDevRender.CreateUninstallPreview();

            var appDir = _uninstallArgs?.AppDir ?? Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            return new UninstallViewModel(UninstallService.CreateDefault(appDir), appDir);
        }

        public static void AttachLaunchWindow(Window window)
        {
            if (_renderMode)
            {
                // В режиме предпросмотра стартовое окно только занимает место и уходит за экран.
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -10000;
                window.Top = -10000;
                window.ShowInTaskbar = false;
                window.ShowActivated = false;
                return;
            }

            window.Closed += (_, _) => Environment.Exit(0);
        }
    }
}
