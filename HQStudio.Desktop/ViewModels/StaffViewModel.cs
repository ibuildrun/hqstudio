using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class StaffViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private User? _selectedUser;

        public ObservableCollection<User> Users { get; } = new();

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public bool IsAdmin => _dataService.CurrentUser?.Role == "Admin";

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public StaffViewModel()
        {
            AddUserCommand = new RelayCommand(_ => AddUser(), _ => IsAdmin);
            EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null && IsAdmin);
            ToggleActiveCommand = new RelayCommand(_ => ToggleActive(), _ => SelectedUser != null && IsAdmin);
            DeleteUserCommand = new RelayCommand(_ => DeleteUser(), _ => SelectedUser != null && IsAdmin && SelectedUser.Id != _dataService.CurrentUser?.Id);
            LoadUsers();
        }

        private void LoadUsers()
        {
            Users.Clear();
            foreach (var user in _dataService.Users)
            {
                Users.Add(user);
            }
        }

        private void AddUser()
        {
            var dialog = new EditUserDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                // Check if username exists
                if (_dataService.Users.Any(u => u.Username == dialog.User.Username))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                dialog.User.Id = _dataService.GetNextId(_dataService.Users);
                dialog.User.CreatedAt = DateTime.Now;
                _dataService.Users.Add(dialog.User);
                _dataService.SaveData();
                LoadUsers();
            }
        }

        private void EditUser()
        {
            if (SelectedUser == null) return;
            
            var dialog = new EditUserDialog(SelectedUser);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                LoadUsers();
            }
        }

        private void ToggleActive()
        {
            if (SelectedUser == null) return;
            
            // Prevent deactivating yourself
            if (SelectedUser.Id == _dataService.CurrentUser?.Id)
            {
                MessageBox.Show("Нельзя деактивировать свой аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            SelectedUser.IsActive = !SelectedUser.IsActive;
            _dataService.SaveData();
            LoadUsers();
        }

        private void DeleteUser()
        {
            if (SelectedUser == null) return;
            
            if (SelectedUser.Id == _dataService.CurrentUser?.Id)
            {
                MessageBox.Show("Нельзя удалить свой аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"Удалить сотрудника \"{SelectedUser.DisplayName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _dataService.Users.Remove(SelectedUser);
                _dataService.SaveData();
                LoadUsers();
            }
        }
    }
}
