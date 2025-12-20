using System.Windows;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class UpdateDialog : Window
    {
        public UpdateDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) => await ((UpdateViewModel)DataContext).CheckOnStartupAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var vm = DataContext as UpdateViewModel;
                if (vm == null || !vm.IsMandatory)
                {
                    CloseButton_Click(sender, e);
                }
            }
        }
    }
}
