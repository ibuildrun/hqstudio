using HQStudio.Models;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
        }

        private void Order_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Order order)
            {
                if (DataContext is OrdersViewModel vm)
                {
                    vm.EditOrder(order);
                }
            }
        }
    }
}
