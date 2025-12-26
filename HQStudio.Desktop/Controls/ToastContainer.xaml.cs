using System.Windows;
using System.Windows.Controls;
using HQStudio.Services;

namespace HQStudio.Controls
{
    /// <summary>
    /// Контейнер для отображения toast-уведомлений
    /// </summary>
    public partial class ToastContainer : UserControl
    {
        public ToastContainer()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ToastNotification notification)
            {
                ToastService.Instance.Dismiss(notification);
            }
        }
    }
}
