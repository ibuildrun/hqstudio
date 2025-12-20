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
        }
    }
}
