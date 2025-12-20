using HQStudio.Models;
using HQStudio.ViewModels;
using HQStudio.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class StaffView : UserControl
    {
        public StaffView()
        {
            InitializeComponent();
        }

        private void User_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is User user)
            {
                // Check if current user is admin
                if (Services.DataService.Instance.CurrentUser?.Role != "Admin")
                {
                    MessageBox.Show("Только администратор может редактировать сотрудников", "Доступ запрещен", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new EditUserDialog(user);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    Services.DataService.Instance.SaveData();
                    if (DataContext is StaffViewModel vm)
                    {
                        // Refresh by triggering property change
                        vm.Users.Clear();
                        foreach (var u in Services.DataService.Instance.Users)
                            vm.Users.Add(u);
                    }
                }
            }
        }
    }
}
