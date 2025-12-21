using HQStudio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class CallbacksViewModel : BaseViewModel
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;

        public ObservableCollection<CallbackItem> Callbacks { get; } = new();
        public ObservableCollection<string> StatusFilters { get; } = new() { "Все", "Новые", "В работе", "Завершённые", "Отменённые" };
        public ObservableCollection<string> SourceFilters { get; } = new() { "Все", "Сайт", "Звонок", "Живой приход", "Почта", "Мессенджер" };

        private CallbackItem? _selectedCallback;
        public CallbackItem? SelectedCallback
        {
            get => _selectedCallback;
            set { SetProperty(ref _selectedCallback, value); OnPropertyChanged(nameof(HasSelection)); }
        }

        private string _selectedStatus = "Все";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { SetProperty(ref _selectedStatus, value); }
        }

        private string _selectedSource = "Все";
        public string SelectedSource
        {
            get => _selectedSource;
            set { SetProperty(ref _selectedSource, value); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); FilterCallbacks(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isApiConnected;
        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        private CallbackStats? _stats;
        public CallbackStats? Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        public bool HasSelection => SelectedCallback != null;

        private List<CallbackItem> _allCallbacks = new();

        public ICommand RefreshCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand TakeInWorkCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateClientCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand DeleteCommand { get; }

        public CallbacksViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
            ApplyFiltersCommand = new RelayCommand(_ => FilterCallbacks());
            TakeInWorkCommand = new RelayCommand(async _ => await ChangeStatusAsync("Processing"), _ => SelectedCallback?.Status == "Новая");
            CompleteCommand = new RelayCommand(async _ => await ChangeStatusAsync("Completed"), _ => SelectedCallback?.Status == "В работе");
            CancelCommand = new RelayCommand(async _ => await ChangeStatusAsync("Cancelled"), _ => SelectedCallback != null && SelectedCallback.Status != "Завершена" && SelectedCallback.Status != "Отменена");
            CreateClientCommand = new RelayCommand(async _ => await CreateClientFromCallbackAsync(), _ => SelectedCallback != null);
            CreateOrderCommand = new RelayCommand(_ => CreateOrderFromCallback(), _ => SelectedCallback != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteCallbackAsync(), _ => SelectedCallback != null);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            IsApiConnected = _settings.UseApi && _apiService.IsConnected;

            if (!IsApiConnected)
            {
                IsLoading = false;
                return;
            }

            try
            {
                // Загружаем статистику
                var stats = await _apiService.GetCallbackStatsAsync();
                if (stats != null)
                {
                    Stats = stats;
                }

                // Загружаем заявки
                var callbacks = await _apiService.GetCallbacksAsync();
                _allCallbacks = callbacks.Select(c => new CallbackItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    CarModel = c.CarModel,
                    LicensePlate = c.LicensePlate,
                    Message = c.Message,
                    Status = MapStatus(c.Status),
                    Source = MapSource(c.Source),
                    SourceDetails = c.SourceDetails,
                    CreatedAt = c.CreatedAt,
                    ProcessedAt = c.ProcessedAt,
                    CompletedAt = c.CompletedAt
                }).ToList();

                FilterCallbacks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataAsync error: {ex.Message}");
            }

            IsLoading = false;
        }

        private void FilterCallbacks()
        {
            Callbacks.Clear();

            var filtered = _allCallbacks.AsEnumerable();

            // Фильтр по статусу
            if (SelectedStatus != "Все")
            {
                filtered = filtered.Where(c => c.Status == SelectedStatus.Replace("ые", "ая").Replace("ённые", "ена").Replace("ённые", "ена"));
            }

            // Фильтр по источнику
            if (SelectedSource != "Все")
            {
                filtered = filtered.Where(c => c.Source == SelectedSource);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Phone.Contains(search) ||
                    (c.CarModel?.ToLower().Contains(search) ?? false) ||
                    (c.LicensePlate?.ToLower().Contains(search) ?? false));
            }

            foreach (var callback in filtered.OrderByDescending(c => c.CreatedAt))
            {
                Callbacks.Add(callback);
            }
        }

        private async Task ChangeStatusAsync(string newStatus)
        {
            if (SelectedCallback == null) return;

            var success = await _apiService.UpdateCallbackStatusAsync(SelectedCallback.Id, newStatus);
            if (success)
            {
                await LoadDataAsync();
            }
        }

        private async Task CreateClientFromCallbackAsync()
        {
            if (SelectedCallback == null) return;

            // Проверяем, нет ли уже клиента с таким телефоном
            var existingClients = await _apiService.GetClientsAsync();
            var existing = existingClients.FirstOrDefault(c => c.Phone == SelectedCallback.Phone);

            if (existing != null)
            {
                MessageBox.Show($"Клиент с телефоном {SelectedCallback.Phone} уже существует:\n{existing.Name}",
                    "Клиент найден", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Создать клиента из заявки?\n\nИмя: {SelectedCallback.Name}\nТелефон: {SelectedCallback.Phone}\nАвто: {SelectedCallback.CarModel ?? "Не указано"}\nГосномер: {SelectedCallback.LicensePlate ?? "Не указан"}",
                "Создание клиента",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var client = await _apiService.CreateClientAsync(new ApiClient
                {
                    Name = SelectedCallback.Name,
                    Phone = SelectedCallback.Phone,
                    CarModel = SelectedCallback.CarModel,
                    LicensePlate = SelectedCallback.LicensePlate,
                    Notes = $"Создан из заявки #{SelectedCallback.Id} ({SelectedCallback.Source})"
                });

                if (client != null)
                {
                    MessageBox.Show($"Клиент {client.Name} успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CreateOrderFromCallback()
        {
            if (SelectedCallback == null) return;
            // TODO: Открыть диалог создания заказа с предзаполненными данными
            MessageBox.Show("Функция создания заказа из заявки будет добавлена", "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DeleteCallbackAsync()
        {
            if (SelectedCallback == null) return;

            var result = MessageBox.Show(
                $"Удалить заявку #{SelectedCallback.Id} от {SelectedCallback.Name}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var success = await _apiService.DeleteCallbackAsync(SelectedCallback.Id);
                if (success)
                {
                    await LoadDataAsync();
                }
            }
        }

        private string MapStatus(string apiStatus)
        {
            return apiStatus switch
            {
                "New" => "Новая",
                "Processing" => "В работе",
                "Completed" => "Завершена",
                "Cancelled" => "Отменена",
                _ => apiStatus
            };
        }

        private string MapSource(RequestSource source)
        {
            return source switch
            {
                RequestSource.Website => "Сайт",
                RequestSource.Phone => "Звонок",
                RequestSource.WalkIn => "Живой приход",
                RequestSource.Email => "Почта",
                RequestSource.Messenger => "Мессенджер",
                RequestSource.Referral => "Рекомендация",
                RequestSource.Other => "Другое",
                _ => "Неизвестно"
            };
        }
    }

    public class CallbackItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";
        public string? SourceDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        public string StatusColor => Status switch
        {
            "Новая" => "#FFC107",
            "В работе" => "#2196F3",
            "Завершена" => "#4CAF50",
            "Отменена" => "#F44336",
            _ => "#707070"
        };
        public string SourceIcon => Source switch
        {
            "Сайт" => "🌐",
            "Звонок" => "📞",
            "Живой приход" => "🚶",
            "Почта" => "📧",
            "Мессенджер" => "💬",
            "Рекомендация" => "👥",
            _ => "❓"
        };
    }
}
