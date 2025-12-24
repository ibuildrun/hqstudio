using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ClientsViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        
        private Client? _selectedClient;
        private string _searchText = string.Empty;
        private List<Client> _allClients = new();
        private bool _isLoading;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalClients;
        private const int PageSize = 20;

        public ObservableCollection<Client> Clients { get; } = new();

        public Client? SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                CurrentPage = 1;
                FilterClients();
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

        public bool ShowEmptyState => !IsLoading && Clients.Count == 0;

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

        public int TotalClients
        {
            get => _totalClients;
            set => SetProperty(ref _totalClients, value);
        }

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
        public bool CanGoPrevious => CurrentPage > 1 && !IsLoading;
        public bool CanGoNext => CurrentPage < TotalPages && !IsLoading;

        public ICommand AddClientCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand DeleteClientCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public ClientsViewModel()
        {
            AddClientCommand = new RelayCommand(_ => AddClientAsync());
            EditClientCommand = new RelayCommand(_ => EditClient(), _ => SelectedClient != null);
            DeleteClientCommand = new RelayCommand(_ => DeleteClient());
            RefreshCommand = new RelayCommand(async _ => await LoadClientsAsync());
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => CanGoPrevious);
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => CanGoNext);
            ExportToExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync(), _ => _allClients.Any());
            _ = LoadClientsAsync();
        }

        private async Task PreviousPageAsync()
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                FilterClients();
            }
            await Task.CompletedTask;
        }

        private async Task NextPageAsync()
        {
            if (CanGoNext)
            {
                CurrentPage++;
                FilterClients();
            }
            await Task.CompletedTask;
        }

        private async Task LoadClientsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            
            try
            {
                _allClients.Clear();
                
                if (_settings.UseApi && !_apiService.IsConnected)
                {
                    await _apiService.CheckConnectionAsync();
                }
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var apiClients = await _apiService.GetClientsAsync();
                    _allClients = apiClients.Select(c => new Client
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Phone = c.Phone,
                        Car = c.CarModel ?? "",
                        CarNumber = c.LicensePlate ?? "",
                        Notes = c.Notes ?? "",
                        CreatedAt = c.CreatedAt
                    }).ToList();
                }
                else
                {
                    _allClients = _dataService.Clients.ToList();
                }
                
                FilterClients();
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        private void FilterClients()
        {
            Clients.Clear();
            
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _allClients
                : _allClients.Where(c =>
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Replace(" ", "").Replace("-", "").Contains(SearchText.Replace(" ", "").Replace("-", ""), StringComparison.OrdinalIgnoreCase) ||
                    c.Car.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CarNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            var orderedList = filtered.OrderByDescending(c => c.CreatedAt).ToList();
            TotalClients = orderedList.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(orderedList.Count / (double)PageSize));
            
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            foreach (var client in orderedList.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            {
                Clients.Add(client);
            }
            
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private async void AddClientAsync()
        {
            var dialog = new EditClientDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var normalizedPhone = dialog.Client.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
                var existingClient = _allClients.FirstOrDefault(c => 
                    c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == normalizedPhone);
                
                if (existingClient != null)
                {
                    ConfirmDialog.ShowInfo(
                        "Дубликат клиента",
                        $"Клиент с таким номером телефона уже существует:\n{existingClient.Name}",
                        ConfirmDialog.DialogType.Warning);
                    return;
                }
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var apiClient = new ApiClient
                    {
                        Name = dialog.Client.Name,
                        Phone = dialog.Client.Phone,
                        CarModel = dialog.Client.Car,
                        LicensePlate = dialog.Client.CarNumber,
                        Notes = dialog.Client.Notes
                    };
                    
                    var (created, error) = await _apiService.CreateClientAsync(apiClient);
                    if (created == null)
                    {
                        ConfirmDialog.ShowInfo("Ошибка", error ?? "Не удалось создать клиента", ConfirmDialog.DialogType.Error);
                        return;
                    }
                }
                else
                {
                    dialog.Client.Id = _dataService.GetNextId(_dataService.Clients);
                    dialog.Client.CreatedAt = DateTime.Now;
                    _dataService.Clients.Add(dialog.Client);
                    _dataService.SaveData();
                }
                
                CurrentPage = 1;
                await LoadClientsAsync();
            }
        }

        private void EditClient()
        {
            if (SelectedClient == null) return;
            
            var dialog = new EditClientDialog(SelectedClient);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                _ = LoadClientsAsync();
            }
        }

        private void DeleteClient()
        {
            if (SelectedClient == null)
            {
                ConfirmDialog.ShowInfo(
                    "Удаление клиента",
                    "Выберите клиента для удаления.\n\nКликните на клиента в списке, чтобы выбрать его.",
                    ConfirmDialog.DialogType.Warning);
                return;
            }
            
            var confirmed = ConfirmDialog.Show(
                "Удалить клиента?",
                $"Вы уверены, что хотите удалить клиента \"{SelectedClient.Name}\"?\n\nЭто действие нельзя отменить.",
                ConfirmDialog.DialogType.Warning,
                "Удалить", "Отмена");
            
            if (confirmed)
            {
                _dataService.Clients.Remove(SelectedClient);
                _dataService.SaveData();
                SelectedClient = null;
                _ = LoadClientsAsync();
            }
        }

        private async Task ExportToExcelAsync()
        {
            if (!_allClients.Any())
            {
                ConfirmDialog.ShowInfo("Информация", "Нет клиентов для экспорта", ConfirmDialog.DialogType.Warning);
                return;
            }
            
            try
            {
                IsLoading = true;
                
                var exportService = new ExcelExportService();
                exportService.ExportClientsToExcel(_allClients.OrderByDescending(c => c.CreatedAt));
            }
            catch (Exception ex)
            {
                ConfirmDialog.ShowInfo("Ошибка", $"Ошибка при экспорте: {ex.Message}", ConfirmDialog.DialogType.Error);
            }
            finally
            {
                IsLoading = false;
            }
            
            await Task.CompletedTask;
        }
    }
}
