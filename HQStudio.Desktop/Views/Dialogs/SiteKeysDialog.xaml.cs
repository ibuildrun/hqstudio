using System.Windows;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class SiteKeysDialog : Window
    {
        private readonly SiteKeysViewModel _vm;

        public SiteKeysDialog(SiteKeysViewModel vm)
        {
            InitializeComponent();
            // На невысоком экране диалог не должен вылезать за рабочую область.
            MaxHeight = Math.Min(MaxHeight, SystemParameters.WorkArea.Height - 40);
            _vm = vm;
            DataContext = vm;

            // PasswordBox не умеет привязку, поэтому значение переносится вручную.
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SiteKeysViewModel.TunaInput) && TunaPassword.Password != vm.TunaInput)
                    TunaPassword.Password = vm.TunaInput;
            };
        }

        private void TunaPassword_PasswordChanged(object sender, RoutedEventArgs e) =>
            _vm.TunaInput = TunaPassword.Password;

        private void TunaShow_Changed(object sender, RoutedEventArgs e)
        {
            if (TunaShow.IsChecked != true)
                TunaPassword.Password = _vm.TunaInput;
        }

        private void CancelApply_Click(object sender, RoutedEventArgs e) => _vm.Cancel();

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseIfIdle();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CloseIfIdle();
        }

        // Пока идёт применение, окно не закрывается: кнопка «Отмена» останавливает его.
        private void CloseIfIdle()
        {
            if (_vm.IsBusy)
                return;

            DialogResult = _vm.Completed;
            Close();
        }
    }
}
