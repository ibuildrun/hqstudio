using HQStudio.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            if (DataContext is LoginViewModel vm)
            {
                vm.LoginSuccessful += OnLoginSuccessful;
                vm.ClearPassword += OnClearPassword;
            }
            
            // Фокус на поле логина при загрузке
            Loaded += (s, e) => UsernameBox.Focus();
        }

        private void OnLoginSuccessful()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void OnClearPassword()
        {
            PasswordBox.Clear();
            PasswordBox.Focus();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
                {
                    vm.LoginCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void TelegramLink_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl("https://t.me/xxxu7");
        }

        private void VkLink_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl("https://vk.com/xxxuy");
        }

        private void TelegramButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://t.me/xxxu7");
        }

        private void VkButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://vk.com/xxxuy");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
