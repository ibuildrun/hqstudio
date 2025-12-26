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
        private readonly ApiCacheService _cache = ApiCacheService.Instance;
        private readonly RecentItemsService _recentItems = RecentItemsService.Instance;
        private const string CacheKeyPrefix = "orders";
        
        private static bool _isInitialized;
        private static int _cachedPage = 1;
        private static int _cachedTotalPages = 1;
        private static int _cachedTotal;
        
        private Order? _selectedOrder;
        private bool _isLoading;
        private bool _isApiConnected;
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

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        public bool ShowEmptyState => !IsLoading && Orders.Count == 0 && IsApiConnected;

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
        public ICommand CancelOrderCommand { get; }
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
        
        public List<string> StatusOptions { get; } = new List<string> { "Все" }
            .Concat(OrderStatus.All.Select(s => s.DisplayName)).ToList();

        public OrdersViewModel() : this(filterActive: false)
        {
        }

        private bool _filterActiveOnly;

        public OrdersViewModel(bool filterActive)
        {
            _filterActiveOnly = filterActive;
            
            AddOrderCommand = new RelayCommand(_ => AddOrder());
            EditOrderCommand = new RelayCommand(_ => EditOrder(), _ => SelectedOrder != null);
            CompleteOrderCommand = new RelayCommand(_ => CompleteOrder());
            CancelOrderCommand = new RelayCommand(_ => CancelOrder());
            DeleteOrderCommand = new RelayCommand(_ => DeleteOrderAsync(), _ => SelectedOrder != null);
            RefreshCommand = new RelayCommand(async _ => await ForceRefreshAsync());
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => CanGoPrevious);
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => CanGoNext);
            PrintOrderCommand = new RelayCommand(_ => PrintOrder());
            ExportToExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync(), _ => Orders.Any());
            ToggleFilterCommand = new RelayCommand(_ => IsFilterVisible = !IsFilterVisible);
            ApplyFilterCommand = new RelayCommand(async _ => await ApplyFilterAsync());
            ClearFilterCommand = new RelayCommand(async _ => await ClearFilterAsync());
            
            _selectedStatus = "Все";
            
            // Подписываемся на автообновление
            _syncService.OrdersChanged += OnOrdersChanged;
            
            // Восстанавливаем кэшированную страницу только если не фильтруем
            if (!filterActive)
            {
                CurrentPage = _cachedPage;
                TotalPages = _cachedTotalPages;
                TotalOrders = _cachedTotal;
            }
            
            // Загружаем заказы
            if (filterActive)
            {
                // Для фильтрации активных - загружаем только активные (не завершённые)
                _ = LoadActiveOrdersAsync();
            }
            else if (!_isInitialized || Orders.Count == 0)
            {
                _ = LoadOrdersAsync();
            }
        }

        /// <summary>
        /// Загрузка только активных заказов (Новый, В работе)
        /// </summary>
        private async Task LoadActiveOrdersAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                Orders.Clear();
                var completedStatus = OrderStatus.Completed.DisplayName;
                var cancelledStatus = OrderStatus.Cancelled.DisplayName;
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    IsApiConnected = true;
                    
                    // Получаем все заказы и фильтруем активные
                    var allOrders = new List<Order>();
                    var page = 1;
                    const int pageSize = 100;
                    
                    while (true)
                    {
                        var response = await _apiService.GetOrdersAsync(page, pageSize);
                        if (response == null || !response.Items.Any()) break;
                        
                        foreach (var apiOrder in response.Items)
                        {
                            var status = MapStatus(apiOrder.Status);
                            // Только активные заказы (не Завершен и не Отменен)
                            if (status != completedStatus && status != cancelledStatus)
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
                                    Status = status,
                                    TotalPrice = apiOrder.TotalPrice,
                                    Notes = apiOrder.Notes ?? string.Empty,
                                    CreatedAt = apiOrder.CreatedAt,
                                    CompletedAt = apiOrder.CompletedAt
                                });
                            }
                        }
                        
                        if (response.Items.Count < pageSize) break;
                        page++;
                    }
                    
                    var orderedList = allOrders.OrderByDescending(o => o.CreatedAt).ToList();
                    TotalOrders = orderedList.Count;
                    TotalPages = Math.Max(1, (int)Math.Ceiling(orderedList.Count / (double)PageSize));
                    
                    foreach (var order in orderedList.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                    {
                        Orders.Add(order);
                    }
                }
                else
                {
                    IsApiConnected = true;
                    var activeOrders = _dataService.Orders
                        .Where(o => o.Status != completedStatus && o.Status != cancelledStatus)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToList();
                    
                    TotalOrders = activeOrders.Count;
                    TotalPages = Math.Max(1, (int)Math.Ceiling(activeOrders.Count / (double)PageSize));
                    
                    foreach (var order in activeOrders.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                    {
                        Orders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active orders: {ex.Message}");
                IsApiConnected = false;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(ShowEmptyState));
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
            _cache.Invalidate(CacheKeyPrefix);
            await LoadOrdersAsync(forceRefresh: true);
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
        public async Task LoadOrdersAsync(bool forceRefresh = false)
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                var selectedId = SelectedOrder?.Id;
                Orders.Clear();
                
                if (_settings.UseApi)
                {
                    if (!_apiService.IsConnected)
                    {
                        await _apiService.CheckConnectionAsync();
                    }
                    
                    if (_apiService.IsConnected)
                    {
                        IsApiConnected = true;
                        
                        var cacheKey = $"{CacheKeyPrefix}_page{CurrentPage}";
                        var response = await _cache.GetOrFetchAsync(
                            cacheKey,
                            async () => await _apiService.GetOrdersAsync(CurrentPage, PageSize),
                            TimeSpan.FromSeconds(20),
                            forceRefresh);
                        
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
                        IsApiConnected = false;
                        return;
                    }
                }
                else
                {
                    IsApiConnected = true;
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
                IsApiConnected = false;
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
            return OrderStatus.FromCode(apiStatus).DisplayName;
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
                ConfirmDialog.ShowInfo("Нет клиентов", "Сначала добавьте клиента в разделе \"Клиенты\".", ConfirmDialog.DialogType.Warning);
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
                        ConfirmDialog.ShowInfo("Ошибка", "Не удалось создать заказ. Попробуйте позже.", ConfirmDialog.DialogType.Error);
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
            
            // Добавляем в недавние просмотренные
            _recentItems.AddRecentOrder(orderToEdit.Id, orderToEdit.ClientDisplay, orderToEdit.Status);
            
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
            if (SelectedOrder == null)
            {
                ConfirmDialog.ShowInfo("Завершение заказа", "Выберите заказ для завершения.\n\nКликните на заказ в списке, чтобы выбрать его.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            if (SelectedOrder.Status == OrderStatus.Completed.DisplayName)
            {
                ConfirmDialog.ShowInfo("Завершение заказа", "Этот заказ уже завершён.", ConfirmDialog.DialogType.Warning);
                return;
            }

            if (SelectedOrder.Status == OrderStatus.Cancelled.DisplayName)
            {
                ConfirmDialog.ShowInfo("Завершение заказа", "Нельзя завершить отменённый заказ.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            var confirmed = ConfirmDialog.Show(
                "Завершить заказ?",
                $"Заказ #{SelectedOrder.Id} будет отмечен как завершённый.\n\nПродолжить?",
                ConfirmDialog.DialogType.Question,
                "Завершить", "Отмена");
            
            if (!confirmed) return;
            
            var orderId = SelectedOrder.Id;
            bool success = false;
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                success = await _apiService.UpdateOrderStatusAsync(SelectedOrder.Id, OrderStatus.Completed.ApiName);
                if (!success)
                {
                    ConfirmDialog.ShowInfo("Ошибка", "Не удалось обновить статус заказа на сервере.\nПопробуйте позже.", ConfirmDialog.DialogType.Error);
                    return;
                }
            }
            else
            {
                SelectedOrder.Status = OrderStatus.Completed.DisplayName;
                SelectedOrder.CompletedAt = DateTime.Now;
                _dataService.SaveData();
                success = true;
            }
            
            await LoadOrdersAsync();
            ConfirmDialog.ShowInfo("Готово", $"Заказ #{orderId} успешно завершён!", ConfirmDialog.DialogType.Success);
        }

        private async void CancelOrder()
        {
            if (SelectedOrder == null)
            {
                ConfirmDialog.ShowInfo("Отмена заказа", "Выберите заказ для отмены.\n\nКликните на заказ в списке, чтобы выбрать его.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            if (SelectedOrder.Status == OrderStatus.Cancelled.DisplayName)
            {
                ConfirmDialog.ShowInfo("Отмена заказа", "Этот заказ уже отменён.", ConfirmDialog.DialogType.Warning);
                return;
            }

            if (SelectedOrder.Status == OrderStatus.Completed.DisplayName)
            {
                ConfirmDialog.ShowInfo("Отмена заказа", "Нельзя отменить завершённый заказ.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            var confirmed = ConfirmDialog.Show(
                "Отменить заказ?",
                $"Заказ #{SelectedOrder.Id} будет отменён.\n\nКлиент отказался или не приехал?",
                ConfirmDialog.DialogType.Warning,
                "Отменить", "Назад");
            
            if (!confirmed) return;
            
            var orderId = SelectedOrder.Id;
            bool success = false;
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                success = await _apiService.UpdateOrderStatusAsync(SelectedOrder.Id, OrderStatus.Cancelled.ApiName);
                if (!success)
                {
                    ConfirmDialog.ShowInfo("Ошибка", "Не удалось обновить статус заказа на сервере.\nПопробуйте позже.", ConfirmDialog.DialogType.Error);
                    return;
                }
            }
            else
            {
                SelectedOrder.Status = OrderStatus.Cancelled.DisplayName;
                _dataService.SaveData();
                success = true;
            }
            
            await LoadOrdersAsync();
            ConfirmDialog.ShowInfo("Готово", $"Заказ #{orderId} отменён.", ConfirmDialog.DialogType.Success);
        }

        private async void DeleteOrderAsync()
        {
            if (SelectedOrder == null)
            {
                ConfirmDialog.ShowInfo("Удаление заказа", "Выберите заказ для удаления.\n\nКликните на заказ в списке, чтобы выбрать его.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            var confirmed = ConfirmDialog.Show(
                "Удалить заказ?",
                $"Заказ #{SelectedOrder.Id} будет удалён.\n\nЭто действие нельзя отменить.",
                ConfirmDialog.DialogType.Warning,
                "Удалить", "Отмена");
            
            if (confirmed)
            {
                var orderId = SelectedOrder.Id;
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var success = await _apiService.DeleteOrderAsync(SelectedOrder.Id);
                    if (success)
                    {
                        await LoadOrdersAsync();
                        ConfirmDialog.ShowInfo("Готово", $"Заказ #{orderId} удалён.", ConfirmDialog.DialogType.Success);
                    }
                    else
                    {
                        ConfirmDialog.ShowInfo("Ошибка", "Не удалось удалить заказ. Попробуйте позже.", ConfirmDialog.DialogType.Error);
                    }
                }
                else
                {
                    _dataService.Orders.Remove(SelectedOrder);
                    _dataService.SaveData();
                    LoadOrders();
                    ConfirmDialog.ShowInfo("Готово", $"Заказ #{orderId} удалён.", ConfirmDialog.DialogType.Success);
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
            if (SelectedOrder == null)
            {
                ConfirmDialog.ShowInfo("Печать", "Выберите заказ для печати.\n\nКликните на заказ в списке, чтобы выбрать его.", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            try
            {
                var printService = new PrintService();
                printService.PrintOrder(SelectedOrder);
            }
            catch (Exception ex)
            {
                ConfirmDialog.ShowInfo("Ошибка печати", $"Не удалось напечатать заказ:\n{ex.Message}", ConfirmDialog.DialogType.Error);
            }
        }
        
        private async Task ExportToExcelAsync()
        {
            if (!Orders.Any())
            {
                ConfirmDialog.ShowInfo("Экспорт", "Нет заказов для экспорта.", ConfirmDialog.DialogType.Warning);
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
                ConfirmDialog.ShowInfo("Ошибка экспорта", $"Не удалось экспортировать заказы:\n{ex.Message}", ConfirmDialog.DialogType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
