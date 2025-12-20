using HQStudio.Models;
using HQStudio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditOrderDialog : Window
    {
        private readonly DataService _dataService = DataService.Instance;
        public Order Order { get; private set; }
        public bool IsNew { get; }
        
        private ObservableCollection<Service> _selectedServices = new();
        private List<Service> _allServices = new();

        public EditOrderDialog(Order? order = null)
        {
            InitializeComponent();
            IsNew = order == null;
            Order = order ?? new Order();
            
            TitleText.Text = IsNew ? "Новый заказ" : $"Заказ #{Order.Id}";
            _allServices = _dataService.Services.Where(s => s.IsActive).ToList();
            
            SelectedServicesList.ItemsSource = _selectedServices;
            LoadData();
        }

        private void LoadData()
        {
            // Load clients
            ClientCombo.ItemsSource = _dataService.Clients;
            if (Order.ClientId > 0)
                ClientCombo.SelectedItem = _dataService.Clients.FirstOrDefault(c => c.Id == Order.ClientId);
            else if (_dataService.Clients.Any())
                ClientCombo.SelectedIndex = 0;

            // Load selected services
            _selectedServices.Clear();
            foreach (var serviceId in Order.ServiceIds)
            {
                var service = _dataService.Services.FirstOrDefault(s => s.Id == serviceId);
                if (service != null)
                    _selectedServices.Add(service);
            }

            // Set status
            StatusCombo.SelectedIndex = Order.Status switch
            {
                "В работе" => 1,
                "Завершен" => 2,
                _ => 0
            };

            PriceBox.Text = Order.TotalPrice.ToString();
            NotesBox.Text = Order.Notes;
        }

        private void ServiceSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ServiceSearchBox.Text.Trim().ToLower();
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;
            
            if (string.IsNullOrEmpty(searchText))
            {
                ServicePopup.IsOpen = false;
                return;
            }

            var results = _allServices
                .Where(s => !_selectedServices.Any(sel => sel.Id == s.Id))
                .Where(s => s.Name.ToLower().Contains(searchText) || 
                           s.Category.ToLower().Contains(searchText))
                .Take(10)
                .ToList();

            if (results.Any())
            {
                ServiceSearchResults.ItemsSource = results;
                ServicePopup.IsOpen = true;
            }
            else
            {
                ServicePopup.IsOpen = false;
            }
        }

        private void ServiceSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServiceSearchBox.Text))
                SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ServiceSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServiceSearchBox.Text))
                SearchPlaceholder.Visibility = Visibility.Visible;
            
            // Delay closing popup to allow click
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!ServicePopup.IsMouseOver)
                    ServicePopup.IsOpen = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ServiceItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Service service)
            {
                _selectedServices.Add(service);
                ServiceSearchBox.Text = "";
                ServicePopup.IsOpen = false;
                UpdateAutoPrice();
            }
        }

        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int serviceId)
            {
                var service = _selectedServices.FirstOrDefault(s => s.Id == serviceId);
                if (service != null)
                {
                    _selectedServices.Remove(service);
                    UpdateAutoPrice();
                }
            }
        }

        private void AutoPrice_Click(object sender, RoutedEventArgs e)
        {
            UpdateAutoPrice();
        }

        private void UpdateAutoPrice()
        {
            var total = _selectedServices.Sum(s => s.PriceFrom);
            PriceBox.Text = total.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ClientCombo.SelectedItem is not Client client)
            {
                MessageBox.Show("Выберите клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Order.ClientId = client.Id;
            Order.Client = client;
            
            // Save services
            Order.ServiceIds = _selectedServices.Select(s => s.Id).ToList();
            Order.Services = _selectedServices.ToList();
            
            var statusItem = StatusCombo.SelectedItem as ComboBoxItem;
            var newStatus = statusItem?.Content?.ToString() ?? "Новый";
            
            if (newStatus == "Завершен" && Order.Status != "Завершен")
                Order.CompletedAt = DateTime.Now;
            
            Order.Status = newStatus;
            Order.Notes = NotesBox.Text.Trim();
            
            if (decimal.TryParse(PriceBox.Text, out var price))
                Order.TotalPrice = price;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Cancel_Click(sender, e);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
