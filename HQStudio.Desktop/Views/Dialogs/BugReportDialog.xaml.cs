using System.Windows;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class BugReportDialog : Window
    {
        private readonly BugReportViewModel _viewModel;

        public BugReportDialog(Exception? crash = null)
            : this(new BugReportViewModel(crash))
        {
        }

        public BugReportDialog(BugReportViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.CloseRequested += OnCloseRequested;
            Closing += (_, _) => viewModel.OnWindowClosing();
            Loaded += (_, _) =>
            {
                if (viewModel.IsCrashReport) DescriptionBox.Focus();
                else TitleBox.Focus();
            };
        }

        /// <summary>
        /// Показывает диалог отправки отчёта. true - отчёт отправлен (или открыта страница GitHub), false - пользователь отказался.
        /// </summary>
        public static bool? ShowFor(Window? owner, Exception? crash = null)
        {
            var dialog = new BugReportDialog(crash);

            if (owner != null && owner.IsVisible)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
            return dialog._viewModel.Succeeded;
        }

        private void OnCloseRequested(bool succeeded)
        {
            // DialogResult можно выставлять только у окна, открытого через ShowDialog.
            try { DialogResult = succeeded; }
            catch (InvalidOperationException) { Close(); }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !e.Handled)
            {
                e.Handled = true;

                // Случайный Esc не должен стирать уже набранный текст.
                if (_viewModel.IsFormStage && !string.IsNullOrWhiteSpace(_viewModel.Description)) return;

                _viewModel.CancelCommand.Execute(null);
            }
        }
    }
}
