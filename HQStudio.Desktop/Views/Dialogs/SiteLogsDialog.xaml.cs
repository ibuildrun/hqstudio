using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class SiteLogsDialog : Window
    {
        private readonly SiteLogsViewModel _vm;

        public SiteLogsDialog(SiteLogsViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Loaded += async (_, _) => await vm.RefreshAsync();
            Closed += (_, _) => vm.Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        // Свежие строки журнала внизу, поэтому после обновления прокручиваем к концу.
        private void LogBox_TextChanged(object sender, TextChangedEventArgs e) => LogBox.ScrollToEnd();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
