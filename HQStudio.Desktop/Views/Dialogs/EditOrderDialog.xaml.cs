using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditOrderDialog : Window
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        
        public Order Order { get; private set; }
        public bool IsNew { get; }
        
        private ObservableCollection<Service> _selectedServices = new();
        private List<Service> _allServices = new();
        private List<Client> _allClients = new();
        private Client? _selectedClient;

        public EditOrderDialog(Order? order = null)
        {
            InitializeComponent();
            IsNew = order == null;
            Order = order ?? new Order();
            
            TitleText.Text = IsNew ? "Новый заказ" : $"Заказ #{Order.Id}";
            _allServices = _dataService.Services.Where(s => s.IsActive).ToList();
            
            SelectedServicesList.ItemsSource = _selectedServices;
            
            // Загружаем данные синхронно при открытии
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Load clients from API or local
            if (_settings.UseApi && _apiService.IsConnected)
            {
                var apiClients = await _apiService.GetClientsAsync();
                _allClients = apiClients.Select(c => new Client
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Car = c.CarModel ?? "",
                    CarNumber = c.LicensePlate ?? ""
                }).ToList();
                System.Diagnostics.Debug.WriteLine($"Loaded {_allClients.Count} clients from API");
            }
            else
            {
                _allClients = _dataService.Clients.ToList();
                System.Diagnostics.Debug.WriteLine($"Loaded {_allClients.Count} clients from local");
            }

            // Set selected client if editing
            if (Order.ClientId > 0)
            {
                _selectedClient = _allClients.FirstOrDefault(c => c.Id == Order.ClientId);
                if (_selectedClient != null)
                {
                    ShowSelectedClient(_selectedClient);
                }
            }

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

        private void ClientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ClientSearchBox.Text.Trim().ToLower();
            ClientSearchPlaceholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;
            
            if (_allClients.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No clients loaded yet");
                ClientPopup.IsOpen = false;
                return;
            }
            
            // Если поле пустое - показываем всех клиентов
            if (string.IsNullOrEmpty(searchText))
            {
                ClientSearchResults.ItemsSource = _allClients.Take(10).ToList();
                ClientPopup.IsOpen = true;
                return;
            }

            // Поиск по имени и телефону (нормализуем телефон для поиска)
            var normalizedSearch = searchText.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
            
            var results = _allClients
                .Where(c => 
                    c.Name.ToLower().Contains(searchText) || 
                    c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "").Contains(normalizedSearch) ||
                    (!string.IsNullOrEmpty(c.Car) && c.Car.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(c.CarNumber) && c.CarNumber.ToLower().Contains(searchText)))
                .Take(10)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Search '{searchText}' found {results.Count} clients from {_allClients.Count} total");

            if (results.Any())
            {
                ClientSearchResults.ItemsSource = results;
                ClientPopup.IsOpen = true;
            }
            else
            {
                ClientPopup.IsOpen = false;
            }
        }

        private void ClientSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ClientSearchPlaceholder.Visibility = Visibility.Collapsed;
            
            // Показываем всех клиентов при фокусе если поле пустое
            if (string.IsNullOrEmpty(ClientSearchBox.Text) && _allClients.Count > 0)
            {
                ClientSearchResults.ItemsSource = _allClients.Take(10).ToList();
                ClientPopup.IsOpen = true;
            }
        }

        private void ClientSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ClientSearchBox.Text))
                ClientSearchPlaceholder.Visibility = Visibility.Visible;
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!ClientPopup.IsMouseOver)
                    ClientPopup.IsOpen = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ClientItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Client client)
            {
                _selectedClient = client;
                ShowSelectedClient(client);
                ClientSearchBox.Text = "";
                ClientPopup.IsOpen = false;
            }
        }

        private void ShowSelectedClient(Client client)
        {
            SelectedClientName.Text = client.Name;
            SelectedClientInfo.Text = $"📞 {client.Phone}" + 
                (string.IsNullOrEmpty(client.Car) ? "" : $" • {client.Car}");
            SelectedClientBorder.Visibility = Visibility.Visible;
            ClientSearchBox.Visibility = Visibility.Collapsed;
            ClientSearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ClearClient_Click(object sender, RoutedEventArgs e)
        {
            _selectedClient = null;
            SelectedClientBorder.Visibility = Visibility.Collapsed;
            ClientSearchBox.Visibility = Visibility.Visible;
            ClientSearchBox.Text = "";
            ClientSearchPlaceholder.Visibility = Visibility.Visible;
            ClientSearchBox.Focus();
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

        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            InputValidation.AllowDecimalNumbers(sender, e);
        }

        private void UpdateAutoPrice()
        {
            var total = _selectedServices.Sum(s => s.PriceFrom);
            PriceBox.Text = total.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
            {
                InputValidation.ShowValidationError("Выберите клиента. Это обязательное поле.", ClientSearchBox);
                return;
            }

            if (_selectedServices.Count == 0)
            {
                InputValidation.ShowValidationError("Добавьте хотя бы одну услугу.", ServiceSearchBox);
                return;
            }

            if (!string.IsNullOrWhiteSpace(PriceBox.Text) && !InputValidation.IsValidPrice(PriceBox.Text))
            {
                InputValidation.ShowValidationError("Введите корректную сумму (только цифры)", PriceBox);
                return;
            }

            Order.ClientId = _selectedClient.Id;
            Order.Client = _selectedClient;
            
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
