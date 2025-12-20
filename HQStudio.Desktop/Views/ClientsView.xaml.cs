using HQStudio.Models;
using HQStudio.ViewModels;
using HQStudio.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class ClientsView : UserControl
    {
        public ClientsView()
        {
            InitializeComponent();
        }

        private void Client_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Client client)
            {
                var dialog = new EditClientDialog(client);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    Services.DataService.Instance.SaveData();
                    if (DataContext is ClientsViewModel vm)
                    {
                        var search = vm.SearchText;
                        vm.SearchText = "";
                        vm.SearchText = search;
                    }
                }
            }
        }
    }
}
