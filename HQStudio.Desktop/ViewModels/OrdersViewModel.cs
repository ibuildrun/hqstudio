using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private Order? _selectedOrder;

        public ObservableCollection<Order> Orders { get; } = new();

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public ICommand AddOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }

        public OrdersViewModel()
        {
            AddOrderCommand = new RelayCommand(_ => AddOrder());
            EditOrderCommand = new RelayCommand(_ => EditOrder(), _ => SelectedOrder != null);
            CompleteOrderCommand = new RelayCommand(_ => CompleteOrder(), _ => SelectedOrder != null && SelectedOrder.Status != "Завершен");
            DeleteOrderCommand = new RelayCommand(_ => DeleteOrder(), _ => SelectedOrder != null);
            LoadOrders();
        }

        public void LoadOrders()
        {
            Orders.Clear();
            foreach (var order in _dataService.Orders.OrderByDescending(o => o.CreatedAt))
            {
                Orders.Add(order);
            }
        }

        private void AddOrder()
        {
            if (!_dataService.Clients.Any())
            {
                MessageBox.Show("Сначала добавьте клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditOrderDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                dialog.Order.Id = _dataService.GetNextId(_dataService.Orders);
                dialog.Order.CreatedAt = DateTime.Now;
                _dataService.Orders.Add(dialog.Order);
                _dataService.SaveData();
                LoadOrders();
            }
        }

        public void EditOrder(Order? order = null)
        {
            var orderToEdit = order ?? SelectedOrder;
            if (orderToEdit == null) return;
            
            var dialog = new EditOrderDialog(orderToEdit);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                LoadOrders();
            }
        }

        private void CompleteOrder()
        {
            if (SelectedOrder == null) return;
            SelectedOrder.Status = "Завершен";
            SelectedOrder.CompletedAt = DateTime.Now;
            _dataService.SaveData();
            LoadOrders();
        }

        private void DeleteOrder()
        {
            if (SelectedOrder == null) return;
            
            var result = MessageBox.Show(
                $"Удалить заказ #{SelectedOrder.Id}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _dataService.Orders.Remove(SelectedOrder);
                _dataService.SaveData();
                LoadOrders();
            }
        }
    }
}
