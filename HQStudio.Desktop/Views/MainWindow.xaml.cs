using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestore();
        }

        private void MaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Навигация по разделам через Alt + цифры
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                switch (e.Key)
                {
                    case Key.D1:
                        vm.NavigateCommand.Execute("Dashboard");
                        e.Handled = true;
                        break;
                    case Key.D2:
                        vm.NavigateCommand.Execute("Services");
                        e.Handled = true;
                        break;
                    case Key.D3:
                        vm.NavigateCommand.Execute("Clients");
                        e.Handled = true;
                        break;
                    case Key.D4:
                        vm.NavigateCommand.Execute("Orders");
                        e.Handled = true;
                        break;
                    case Key.D5:
                        vm.NavigateCommand.Execute("Staff");
                        e.Handled = true;
                        break;
                    case Key.D6:
                        vm.NavigateCommand.Execute("Settings");
                        e.Handled = true;
                        break;
                }
            }
            
            // F5 - обновить данные
            if (e.Key == Key.F5)
            {
                vm.RefreshCommand?.Execute(null);
                e.Handled = true;
            }
        }
    }
}
