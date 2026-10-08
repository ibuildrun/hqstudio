using System.Windows;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class SiteGuideDialog : Window
    {
        public SiteGuideDialog(SiteGuideViewModel vm)
        {
            InitializeComponent();
            // На невысоком экране окно не должно вылезать за рабочую область.
            MaxHeight = SystemParameters.WorkArea.Height - 40;
            DataContext = vm;

            vm.ReportBugRequested += () => BugReportDialog.ShowFor(this);
            // Новая глава открывается с начала, а не с места, где остановились в предыдущей.
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SiteGuideViewModel.SelectedSection))
                    ChapterScroll.ScrollToTop();
            };
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
