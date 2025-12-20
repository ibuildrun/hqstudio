using HQStudio.Models;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditUserDialog : Window
    {
        public User User { get; private set; }
        public bool IsNew { get; }

        public EditUserDialog(User? user = null)
        {
            InitializeComponent();
            IsNew = user == null;
            User = user ?? new User();
            
            TitleText.Text = IsNew ? "Новый сотрудник" : "Редактирование сотрудника";
            LoadData();
            
            Loaded += (s, e) => DisplayNameBox.Focus();
        }

        private void LoadData()
        {
            DisplayNameBox.Text = User.DisplayName;
            UsernameBox.Text = User.Username;
            PasswordBox.Password = User.PasswordHash;
            RoleCombo.SelectedIndex = User.Role == "Admin" ? 0 : 1;
            IsActiveCheck.IsChecked = User.IsActive;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
            {
                MessageBox.Show("Введите имя сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DisplayNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            if (IsNew && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            User.DisplayName = DisplayNameBox.Text.Trim();
            User.Username = UsernameBox.Text.Trim().ToLower();
            
            if (!string.IsNullOrEmpty(PasswordBox.Password))
                User.PasswordHash = PasswordBox.Password;
            
            User.Role = RoleCombo.SelectedIndex == 0 ? "Admin" : "Worker";
            User.IsActive = IsActiveCheck.IsChecked ?? true;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Cancel_Click(sender, e);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
