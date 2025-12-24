using HQStudio.Services;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly bool _isFirstLogin;
        private readonly string _currentPassword;

        /// <summary>
        /// Диалог смены пароля
        /// </summary>
        /// <param name="isFirstLogin">Если true - первый вход, текущий пароль не запрашивается</param>
        /// <param name="currentPassword">Текущий пароль (для первого входа)</param>
        public ChangePasswordDialog(bool isFirstLogin = false, string currentPassword = "")
        {
            InitializeComponent();
            _isFirstLogin = isFirstLogin;
            _currentPassword = currentPassword;

            if (isFirstLogin)
            {
                // Скрываем поле текущего пароля и кнопку отмены
                CurrentPasswordPanel.Visibility = Visibility.Collapsed;
                CancelBtn.Visibility = Visibility.Collapsed;
                CloseBtn.Visibility = Visibility.Collapsed;
                SubtitleText.Text = "Для продолжения работы установите новый пароль";
            }
            
            // Устанавливаем фокус на первое поле после загрузки
            Loaded += (s, e) =>
            {
                if (isFirstLogin)
                    NewPasswordBox.Focus();
                else
                    CurrentPasswordBox.Focus();
            };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !_isFirstLogin)
            {
                // Esc закрывает только если не первый вход
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                // Enter сохраняет
                SaveButton_Click(sender, e);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;
            var currentPassword = _isFirstLogin ? _currentPassword : CurrentPasswordBox.Password;

            // Валидация
            if (!_isFirstLogin && string.IsNullOrEmpty(currentPassword))
            {
                ErrorText.Text = "Введите текущий пароль";
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ErrorText.Text = "Введите новый пароль";
                return;
            }

            if (newPassword.Length < 6)
            {
                ErrorText.Text = "Пароль должен быть не менее 6 символов";
                return;
            }

            if (newPassword != confirmPassword)
            {
                ErrorText.Text = "Пароли не совпадают";
                return;
            }

            if (newPassword == currentPassword)
            {
                ErrorText.Text = "Новый пароль должен отличаться от текущего";
                return;
            }

            // Отправляем запрос
            var (success, error) = await _apiService.ChangePasswordAsync(currentPassword, newPassword);

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = error ?? "Не удалось сменить пароль";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
