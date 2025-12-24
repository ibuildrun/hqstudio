using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ServicesViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly ApiCacheService _cache = ApiCacheService.Instance;
        private const string CacheKey = "services";
        
        private Service? _selectedService;
        private string _searchText = string.Empty;
        private List<Service> _allServices = new();
        private bool _isLoading;
        private bool _isApiConnected = true;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalServices;
        private const int PageSize = 10;

        public ObservableCollection<Service> Services { get; } = new();
        
        public Service? SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                CurrentPage = 1;
                FilterServices();
            }
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

        public bool ShowEmptyState => !IsLoading && Services.Count == 0 && IsApiConnected;

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

        public int TotalServices
        {
            get => _totalServices;
            set => SetProperty(ref _totalServices, value);
        }

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
        public bool CanGoPrevious => CurrentPage > 1 && !IsLoading;
        public bool CanGoNext => CurrentPage < TotalPages && !IsLoading;

        public ICommand AddServiceCommand { get; }
        public ICommand EditServiceCommand { get; }
        public ICommand DeleteServiceCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        public ServicesViewModel()
        {
            AddServiceCommand = new RelayCommand(_ => AddServiceAsync());
            EditServiceCommand = new RelayCommand(_ => EditServiceAsync(), _ => SelectedService != null);
            DeleteServiceCommand = new RelayCommand(_ => DeleteServiceAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadServicesAsync(forceRefresh: true));
            PreviousPageCommand = new RelayCommand(_ => PreviousPage());
            NextPageCommand = new RelayCommand(_ => NextPage());
            _ = LoadServicesAsync();
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                FilterServices();
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                FilterServices();
            }
        }

        private async Task LoadServicesAsync(bool forceRefresh = false)
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                if (_settings.UseApi)
                {
                    if (!_apiService.IsConnected)
                    {
                        await _apiService.CheckConnectionAsync();
                    }
                    
                    if (_apiService.IsConnected)
                    {
                        IsApiConnected = true;
                        
                        var apiServices = await _cache.GetOrFetchAsync(
                            CacheKey,
                            async () => await _apiService.GetServicesAsync(),
                            TimeSpan.FromSeconds(30),
                            forceRefresh);
                        
                        if (apiServices != null)
                        {
                            _allServices = apiServices.Select(s => new Service
                            {
                                Id = s.Id,
                                Name = s.Title,
                                Description = s.Description,
                                Category = s.Category,
                                PriceFrom = ParsePrice(s.Price),
                                Icon = string.IsNullOrEmpty(s.Icon) ? "🔧" : s.Icon,
                                IsActive = s.IsActive
                            }).ToList();
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
                    _allServices = _dataService.Services.ToList();
                }
                
                FilterServices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadServicesAsync error: {ex.Message}");
                IsApiConnected = false;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        private decimal ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price)) return 0;
            
            // Убираем всё кроме цифр и точки/запятой
            var cleaned = new string(price.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            cleaned = cleaned.Replace(',', '.');
            
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            
            return 0;
        }

        private void FilterServices()
        {
            Services.Clear();
            
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _allServices
                : _allServices.Where(s => 
                    s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            var orderedList = filtered.OrderBy(s => s.Category).ThenBy(s => s.Name).ToList();
            TotalServices = orderedList.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(orderedList.Count / (double)PageSize));
            
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            foreach (var service in orderedList.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            {
                Services.Add(service);
            }
            
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private async void AddServiceAsync()
        {
            var dialog = new EditServiceDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var apiService = new ApiServiceItem
                    {
                        Title = dialog.Service.Name,
                        Description = dialog.Service.Description,
                        Category = dialog.Service.Category,
                        Price = dialog.Service.PriceFrom > 0 ? $"от {dialog.Service.PriceFrom:N0} ₽" : "",
                        Icon = dialog.Service.Icon,
                        IsActive = dialog.Service.IsActive,
                        SortOrder = _allServices.Count
                    };
                    
                    var created = await _apiService.CreateServiceAsync(apiService);
                    if (created == null)
                    {
                        ConfirmDialog.ShowInfo("Ошибка", "Не удалось создать услугу", ConfirmDialog.DialogType.Error);
                        return;
                    }
                    _cache.Invalidate(CacheKey);
                }
                else
                {
                    dialog.Service.Id = _dataService.GetNextId(_dataService.Services);
                    _dataService.Services.Add(dialog.Service);
                    _dataService.SaveData();
                }
                
                await LoadServicesAsync(forceRefresh: true);
            }
        }

        private async void EditServiceAsync()
        {
            if (SelectedService == null) return;
            
            var dialog = new EditServiceDialog(SelectedService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                if (_settings.UseApi)
                {
                    // Проверяем соединение перед обновлением
                    if (!_apiService.IsConnected)
                    {
                        await _apiService.CheckConnectionAsync();
                    }
                    
                    if (_apiService.IsConnected)
                    {
                        var apiService = new ApiServiceItem
                        {
                            Id = dialog.Service.Id,
                            Title = dialog.Service.Name,
                            Description = dialog.Service.Description,
                            Category = dialog.Service.Category,
                            Price = dialog.Service.PriceFrom > 0 ? $"от {dialog.Service.PriceFrom:N0} ₽" : "",
                            Icon = dialog.Service.Icon,
                            IsActive = dialog.Service.IsActive
                        };
                        
                        var success = await _apiService.UpdateServiceAsync(dialog.Service.Id, apiService);
                        
                        if (!success)
                        {
                            ConfirmDialog.ShowInfo("Ошибка", "Не удалось обновить услугу", ConfirmDialog.DialogType.Error);
                            return;
                        }
                        _cache.Invalidate(CacheKey);
                    }
                    else
                    {
                        ConfirmDialog.ShowInfo("Ошибка", "Нет соединения с сервером", ConfirmDialog.DialogType.Warning);
                        return;
                    }
                }
                else
                {
                    _dataService.SaveData();
                }
                
                await LoadServicesAsync(forceRefresh: true);
            }
        }

        private async void DeleteServiceAsync()
        {
            if (SelectedService == null)
            {
                ConfirmDialog.ShowInfo(
                    "Удаление услуги",
                    "Выберите услугу для удаления.\n\nКликните на услугу в списке, чтобы выбрать её.",
                    ConfirmDialog.DialogType.Warning);
                return;
            }
            
            var confirmed = ConfirmDialog.Show(
                "Удалить услугу?",
                $"Вы уверены, что хотите удалить услугу \"{SelectedService.Name}\"?\n\nЭто действие нельзя отменить.",
                ConfirmDialog.DialogType.Warning,
                "Удалить", "Отмена");
            
            if (confirmed)
            {
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var success = await _apiService.DeleteServiceAsync(SelectedService.Id);
                    if (!success)
                    {
                        ConfirmDialog.ShowInfo("Ошибка", "Не удалось удалить услугу", ConfirmDialog.DialogType.Error);
                        return;
                    }
                    _cache.Invalidate(CacheKey);
                }
                else
                {
                    _dataService.Services.Remove(SelectedService);
                    _dataService.SaveData();
                }
                
                SelectedService = null;
                await LoadServicesAsync(forceRefresh: true);
            }
        }
    }
}
