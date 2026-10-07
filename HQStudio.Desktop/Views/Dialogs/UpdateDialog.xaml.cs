using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class UpdateDialog : Window
    {
        public UpdateDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is UpdateViewModel vm)
                    await vm.CheckIfNeededAsync();
            };
        }

        public static void ShowFor(Window? owner = null)
        {
            var dialog = new UpdateDialog { Owner = owner ?? Application.Current?.MainWindow };
            dialog.ShowDialog();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e) => LogBox.ScrollToEnd();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
