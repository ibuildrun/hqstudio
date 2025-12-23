using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class OrdersViewModel : BaseViewModel, IDisposable
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly DataSyncService _syncService = DataSyncService.Instance;
        
        private static bool _isInitialized;
        private static int _cachedPage = 1;
        private static int _cachedTotalPages = 1;
        private static int _cachedTotal;
        
        private Order? _selectedOrder;
        private bool _isLoading;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalOrders;
        private const int PageSize = 20;

        public ObservableCollection<Order> Orders { get; } = new();

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        public bool ShowEmptyState => !IsLoading && Orders.Count == 0;

        public int CurrentPage
        {
            get => _currentPage;
            set { SetProperty(ref _currentPage, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { SetProperty(ref _totalPages, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
        public bool CanGoPrevious => CurrentPage > 1 && !IsLoading;
        public bool CanGoNext => CurrentPage < TotalPages && !IsLoading;

        public ICommand AddOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrintOrderCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ToggleFilterCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        
        // Filter properties
        private bool _isFilterVisible;
        private string? _selectedStatus;
        private DateTime? _filterDateFrom;
        private DateTime? _filterDateTo;
        private string? _filterClientName;
        
        public bool IsFilterVisible
        {
            get => _isFilterVisible;
            set => SetProperty(ref _isFilterVisible, value);
        }
        
        public string? SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }
        
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set => SetProperty(ref _filterDateFrom, value);
        }
        
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set => SetProperty(ref _filterDateTo, value);
        }
        
        public string? FilterClientName
        {
            get => _filterClientName;
            set => SetProperty(ref _filterClientName, value);
        }
        
        public List<string> StatusOptions { get; } = new() { "Все", "Новый", "В работе", "Завершен", "Отменен" };

        public OrdersViewModel()
        {
            AddOrderCommand = new RelayCommand(_ => AddOrder());
            EditOrderCommand = new RelayCommand(_ => EditOrder(), _ => SelectedOrder != null);
            CompleteOrderCommand = new RelayCommand(_ => CompleteOrder(), _ => SelectedOrder != null && SelectedOrder.Status != "Завершен");
            DeleteOrderCommand = new RelayCommand(_ => DeleteOrderAsync(), _ => SelectedOrder != null);
            RefreshCommand = new RelayCommand(async _ => await ForceRefreshAsync());
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => CanGoPrevious);
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => CanGoNext);
            PrintOrderCommand = new RelayCommand(_ => PrintOrder(), _ => SelectedOrder != null);
            ExportToExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync(), _ => Orders.Any());
            ToggleFilterCommand = new RelayCommand(_ => IsFilterVisible = !IsFilterVisible);
            ApplyFilterCommand = new RelayCommand(async _ => await ApplyFilterAsync());
            ClearFilterCommand = new RelayCommand(async _ => await ClearFilterAsync());
            
            _selectedStatus = "Все";
            
            // Подписываемся на автообновление
            _syncService.OrdersChanged += OnOrdersChanged;
            
            // Восстанавливаем кэшированную страницу
            CurrentPage = _cachedPage;
            TotalPages = _cachedTotalPages;
            TotalOrders = _cachedTotal;
            
            // Загружаем только если ещё не загружали
            if (!_isInitialized || Orders.Count == 0)
            {
                _ = LoadOrdersAsync();
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                await LoadOrdersAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (CanGoNext)
            {
                CurrentPage++;
                await LoadOrdersAsync();
            }
        }

        private async Task ForceRefreshAsync()
        {
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        private async void OnOrdersChanged(object? sender, EventArgs e)
        {
            if (!IsLoading)
            {
                await SyncOrdersAsync();
            }
        }

        public void Dispose()
        {
            _syncService.OrdersChanged -= OnOrdersChanged;
        }

        /// <summary>
        /// Умная синхронизация - обновляет статусы без перезагрузки
        /// </summary>
        private async Task SyncOrdersAsync()
        {
            if (IsLoading) return;
            
            try
            {
                if (!_settings.UseApi || !_apiService.IsConnected)
                    return;

                var response = await _apiService.GetOrdersAsync(CurrentPage, PageSize);
                if (response == null || response.Items.Count == 0) return;

                var existingIds = Orders.Select(o => o.Id).ToHashSet();

                // Обновляем статусы существующих заказов
                foreach (var apiOrder in response.Items.Where(o => existingIds.Contains(o.Id)))
                {
                    var existing = Orders.FirstOrDefault(o => o.Id == apiOrder.Id);
                    if (existing != null)
                    {
                        var newStatus = MapStatus(apiOrder.Status);
                        if (existing.Status != newStatus)
                            existing.Status = newStatus;
                        if (existing.TotalPrice != apiOrder.TotalPrice)
                            existing.TotalPrice = apiOrder.TotalPrice;
                        if (existing.CompletedAt != apiOrder.CompletedAt)
                            existing.CompletedAt = apiOrder.CompletedAt;
                    }
                }

                // Обновляем счётчик если изменился
                if (TotalOrders != response.Total)
                {
                    TotalOrders = response.Total;
                    TotalPages = response.TotalPages;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка заказов с пагинацией
        /// </summary>
        public async Task LoadOrdersAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                var selectedId = SelectedOrder?.Id;
                Orders.Clear();
                
                if (_settings.UseApi && !_apiService.IsConnected)
                {
                    await _apiService.CheckConnectionAsync();
                }
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var response = await _apiService.GetOrdersAsync(CurrentPage, PageSize);
                    if (response != null)
                    {
                        TotalOrders = response.Total;
                        TotalPages = response.TotalPages > 0 ? response.TotalPages : 1;
                        
                        foreach (var apiOrder in response.Items)
                        {
                            var clientName = apiOrder.Client?.Name ?? string.Empty;
                            
                            Orders.Add(new Order
                            {
                                Id = apiOrder.Id,
                                ClientId = apiOrder.ClientId,
                                ClientName = clientName,
                                Client = apiOrder.Client != null ? new Client
                                {
                                    Id = apiOrder.Client.Id,
                                    Name = apiOrder.Client.Name,
                                    Phone = apiOrder.Client.Phone,
                                    Car = apiOrder.Client.CarModel ?? string.Empty,
                                    CarNumber = apiOrder.Client.LicensePlate ?? string.Empty
                                } : null,
                                Status = MapStatus(apiOrder.Status),
                                TotalPrice = apiOrder.TotalPrice,
                                Notes = apiOrder.Notes ?? string.Empty,
                                CreatedAt = apiOrder.CreatedAt,
                                CompletedAt = apiOrder.CompletedAt
                            });
                        }
                        
                        _isInitialized = true;
                        _cachedPage = CurrentPage;
                        _cachedTotalPages = TotalPages;
                        _cachedTotal = TotalOrders;
                    }
                }
                else
                {
                    var localOrders = _dataService.Orders.OrderByDescending(o => o.CreatedAt).ToList();
                    TotalOrders = localOrders.Count;
                    TotalPages = Math.Max(1, (int)Math.Ceiling(localOrders.Count / (double)PageSize));
                    
                    foreach (var order in localOrders.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                    {
                        Orders.Add(order);
                    }
                }
                
                if (selectedId.HasValue)
                {
                    SelectedOrder = Orders.FirstOrDefault(o => o.Id == selectedId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        public void LoadOrders()
        {
            _ = LoadOrdersAsync();
        }

        private string MapStatus(int apiStatus)
        {
            return apiStatus switch
            {
                0 => "Новый",
                1 => "В работе",
                2 => "Завершен",
                3 => "Отменен",
                _ => $"Статус {apiStatus}"
            };
        }

        private async void AddOrder()
        {
            // Проверяем наличие клиентов
            bool hasClients = false;
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                var clients = await _apiService.GetClientsAsync();
                hasClients = clients.Any();
            }
            else
            {
                hasClients = _dataService.Clients.Any();
            }
            
            if (!hasClients)
            {
                MessageBox.Show("Сначала добавьте клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditOrderDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                int? createdOrderId = null;
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    // Создаём заказ через API
                    var request = new CreateOrderRequest
                    {
                        ClientId = dialog.Order.ClientId,
                        ServiceIds = dialog.Order.ServiceIds,
                        TotalPrice = dialog.Order.TotalPrice,
                        Notes = dialog.Order.Notes
                    };
                    
                    var created = await _apiService.CreateOrderAsync(request);
                    if (created == null)
                    {
                        MessageBox.Show("Не удалось создать заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    createdOrderId = created.Id;
                }
                else
                {
                    dialog.Order.Id = _dataService.GetNextId(_dataService.Orders);
                    dialog.Order.CreatedAt = DateTime.Now;
                    _dataService.Orders.Add(dialog.Order);
                    _dataService.SaveData();
                    createdOrderId = dialog.Order.Id;
                }
                
                // Перезагружаем список и переходим на первую страницу
                CurrentPage = 1;
                await LoadOrdersAsync();
                
                // Показываем диалог успеха с возможностью перейти к заказу
                if (createdOrderId.HasValue)
                {
                    var goToOrder = ConfirmDialog.Show(
                        "Заказ создан",
                        $"Заказ #{createdOrderId} успешно создан!\n\nПерейти к заказу?",
                        ConfirmDialog.DialogType.Success,
                        "Перейти", "Закрыть");
                    
                    if (goToOrder)
                    {
                        // Выделяем созданный заказ
                        SelectedOrder = Orders.FirstOrDefault(o => o.Id == createdOrderId);
                    }
                }
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

        private async void CompleteOrder()
        {
            if (SelectedOrder == null) return;
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                await _apiService.UpdateOrderStatusAsync(SelectedOrder.Id, "Completed");
            }
            else
            {
                SelectedOrder.Status = "Завершен";
                SelectedOrder.CompletedAt = DateTime.Now;
                _dataService.SaveData();
            }
            
            await LoadOrdersAsync();
        }

        private async void DeleteOrderAsync()
        {
            if (SelectedOrder == null) return;
            
            var result = MessageBox.Show(
                $"Удалить заказ #{SelectedOrder.Id}?\n\nЗаказ будет помечен как удалённый, но сохранится в базе данных.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var success = await _apiService.DeleteOrderAsync(SelectedOrder.Id);
                    if (success)
                    {
                        await LoadOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    _dataService.Orders.Remove(SelectedOrder);
                    _dataService.SaveData();
                    LoadOrders();
                }
            }
        }
        
        private async Task ApplyFilterAsync()
        {
            CurrentPage = 1;
            await LoadOrdersWithFilterAsync();
        }
        
        private async Task ClearFilterAsync()
        {
            SelectedStatus = "Все";
            FilterDateFrom = null;
            FilterDateTo = null;
            FilterClientName = null;
            CurrentPage = 1;
            await LoadOrdersAsync();
        }
        
        private async Task LoadOrdersWithFilterAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                var selectedId = SelectedOrder?.Id;
                Orders.Clear();
                
                IEnumerable<Order> filteredOrders;
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    // Получаем все заказы для фильтрации на клиенте
                    var allOrders = new List<Order>();
                    var page = 1;
                    const int pageSize = 100;
                    
                    while (true)
                    {
                        var response = await _apiService.GetOrdersAsync(page, pageSize);
                        if (response == null || !response.Items.Any()) break;
                        
                        foreach (var apiOrder in response.Items)
                        {
                            allOrders.Add(new Order
                            {
                                Id = apiOrder.Id,
                                ClientId = apiOrder.ClientId,
                                ClientName = apiOrder.Client?.Name ?? string.Empty,
                                Client = apiOrder.Client != null ? new Client
                                {
                                    Id = apiOrder.Client.Id,
                                    Name = apiOrder.Client.Name,
                                    Phone = apiOrder.Client.Phone,
                                    Car = apiOrder.Client.CarModel ?? string.Empty,
                                    CarNumber = apiOrder.Client.LicensePlate ?? string.Empty
                                } : null,
                                Status = MapStatus(apiOrder.Status),
                                TotalPrice = apiOrder.TotalPrice,
                                Notes = apiOrder.Notes ?? string.Empty,
                                CreatedAt = apiOrder.CreatedAt,
                                CompletedAt = apiOrder.CompletedAt
                            });
                        }
                        
                        if (response.Items.Count < pageSize) break;
                        page++;
                    }
                    
                    filteredOrders = ApplyFilters(allOrders);
                }
                else
                {
                    filteredOrders = ApplyFilters(_dataService.Orders);
                }
                
                var orderedList = filteredOrders.OrderByDescending(o => o.CreatedAt).ToList();
                TotalOrders = orderedList.Count;
                TotalPages = Math.Max(1, (int)Math.Ceiling(orderedList.Count / (double)PageSize));
                
                foreach (var order in orderedList.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                {
                    Orders.Add(order);
                }
                
                if (selectedId.HasValue)
                {
                    SelectedOrder = Orders.FirstOrDefault(o => o.Id == selectedId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering orders: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
        
        private IEnumerable<Order> ApplyFilters(IEnumerable<Order> orders)
        {
            var result = orders.AsEnumerable();
            
            // Фильтр по статусу
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Все")
            {
                result = result.Where(o => o.Status == SelectedStatus);
            }
            
            // Фильтр по дате создания
            if (FilterDateFrom.HasValue)
            {
                result = result.Where(o => o.CreatedAt.Date >= FilterDateFrom.Value.Date);
            }
            
            if (FilterDateTo.HasValue)
            {
                result = result.Where(o => o.CreatedAt.Date <= FilterDateTo.Value.Date);
            }
            
            // Фильтр по клиенту
            if (!string.IsNullOrWhiteSpace(FilterClientName))
            {
                var searchTerm = FilterClientName.ToLower();
                result = result.Where(o => 
                    (o.Client?.Name?.ToLower().Contains(searchTerm) == true) ||
                    (o.ClientName?.ToLower().Contains(searchTerm) == true));
            }
            
            return result;
        }
        
        private void PrintOrder()
        {
            if (SelectedOrder == null) return;
            
            try
            {
                var printService = new PrintService();
                printService.PrintOrder(SelectedOrder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task ExportToExcelAsync()
        {
            if (!Orders.Any())
            {
                MessageBox.Show("Нет заказов для экспорта", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                IsLoading = true;
                
                var allOrders = new List<Order>();
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var page = 1;
                    const int pageSize = 100;
                    
                    while (true)
                    {
                        var response = await _apiService.GetOrdersAsync(page, pageSize);
                        if (response == null || !response.Items.Any()) break;
                        
                        foreach (var apiOrder in response.Items)
                        {
                            allOrders.Add(new Order
                            {
                                Id = apiOrder.Id,
                                ClientId = apiOrder.ClientId,
                                Client = new Client
                                {
                                    Name = apiOrder.Client?.Name ?? "",
                                    Phone = apiOrder.Client?.Phone ?? "",
                                    Car = apiOrder.Client?.CarModel ?? "",
                                    CarNumber = apiOrder.Client?.LicensePlate ?? ""
                                },
                                Status = MapStatus(apiOrder.Status),
                                TotalPrice = apiOrder.TotalPrice,
                                Notes = apiOrder.Notes ?? "",
                                CreatedAt = apiOrder.CreatedAt,
                                CompletedAt = apiOrder.CompletedAt,
                                Services = new List<Service>()
                            });
                        }
                        
                        if (response.Items.Count < pageSize) break;
                        page++;
                    }
                }
                else
                {
                    allOrders = _dataService.Orders.ToList();
                }
                
                var exportService = new ExcelExportService();
                exportService.ExportOrdersToExcel(allOrders.OrderByDescending(o => o.CreatedAt));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
