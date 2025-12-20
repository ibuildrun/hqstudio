using HQStudio.Models;
using HQStudio.ViewModels;
using HQStudio.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class ServicesView : UserControl
    {
        public ServicesView()
        {
            InitializeComponent();
        }

        private void Service_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Service service)
            {
                var dialog = new EditServiceDialog(service);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    Services.DataService.Instance.SaveData();
                    if (DataContext is ServicesViewModel vm)
                    {
                        // Refresh
                        var search = vm.SearchText;
                        vm.SearchText = "";
                        vm.SearchText = search;
                    }
                }
            }
        }
    }
}
