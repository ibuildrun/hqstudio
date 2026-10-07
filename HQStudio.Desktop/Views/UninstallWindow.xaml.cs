using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using HQStudio.Services.Site;
using HQStudio.ViewModels;

namespace HQStudio.Views
{
    public partial class UninstallWindow : Window
    {
        private readonly UninstallViewModel _vm;

        // Окно создаёт WPF по StartupUri из App.xaml.cs (режим --uninstall), поэтому нужен конструктор без параметров.
        public UninstallWindow() : this(UninstallBootstrap.CreateLaunchViewModel())
        {
            UninstallBootstrap.AttachLaunchWindow(this);
        }

        public UninstallWindow(UninstallViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            vm.CloseRequested += (_, _) => Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        // Во время удаления окно закрыть нельзя: прерванное на середине удаление оставит мусор.
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_vm.IsRunning)
                e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
