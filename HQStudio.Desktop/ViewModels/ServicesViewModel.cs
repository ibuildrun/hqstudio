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
        
        private Service? _selectedService;
        private string _searchText = string.Empty;
        private List<Service> _allServices = new();
        private bool _isLoading;
        private int _totalServices;

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

        public bool ShowEmptyState => !IsLoading && Services.Count == 0;

        public int TotalServices
        {
            get => _totalServices;
            set => SetProperty(ref _totalServices, value);
        }

        public ICommand AddServiceCommand { get; }
        public ICommand EditServiceCommand { get; }
        public ICommand DeleteServiceCommand { get; }
        public ICommand RefreshCommand { get; }

        public ServicesViewModel()
        {
            AddServiceCommand = new RelayCommand(_ => AddServiceAsync());
            EditServiceCommand = new RelayCommand(_ => EditServiceAsync(), _ => SelectedService != null);
            DeleteServiceCommand = new RelayCommand(_ => DeleteServiceAsync(), _ => SelectedService != null);
            RefreshCommand = new RelayCommand(async _ => await LoadServicesAsync());
            _ = LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            
            try
            {
                _allServices.Clear();
                
                if (_settings.UseApi && !_apiService.IsConnected)
                {
                    await _apiService.CheckConnectionAsync();
                }
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var apiServices = await _apiService.GetServicesAsync();
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
                else
                {
                    _allServices = _dataService.Services.ToList();
                }
                
                FilterServices();
            }
            finally
            {
                IsLoading = false;
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

            foreach (var service in orderedList)
            {
                Services.Add(service);
            }
            
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
                        MessageBox.Show("Не удалось создать услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    dialog.Service.Id = _dataService.GetNextId(_dataService.Services);
                    _dataService.Services.Add(dialog.Service);
                    _dataService.SaveData();
                }
                
                await LoadServicesAsync();
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
                            MessageBox.Show("Не удалось обновить услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Нет соединения с сервером", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    _dataService.SaveData();
                }
                
                await LoadServicesAsync();
            }
        }

        private async void DeleteServiceAsync()
        {
            if (SelectedService == null) return;
            
            var result = MessageBox.Show(
                $"Удалить услугу \"{SelectedService.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var success = await _apiService.DeleteServiceAsync(SelectedService.Id);
                    if (!success)
                    {
                        MessageBox.Show("Не удалось удалить услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    _dataService.Services.Remove(SelectedService);
                    _dataService.SaveData();
                }
                
                await LoadServicesAsync();
            }
        }
    }
}
