using HQStudio.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ActivityLogViewModel : BaseViewModel
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;

        public ObservableCollection<ActivityLogEntry> ActivityLogs { get; } = new();
        public ObservableCollection<ActivityUserStat> Users { get; } = new();
        public ObservableCollection<string> Sources { get; } = new() { "Все", "Desktop", "Web", "API" };

        private ActivityLogStats? _stats;
        public ActivityLogStats? Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        private string _selectedSource = "Все";
        public string SelectedSource
        {
            get => _selectedSource;
            set { SetProperty(ref _selectedSource, value); }
        }

        private ActivityUserStat? _selectedUser;
        public ActivityUserStat? SelectedUser
        {
            get => _selectedUser;
            set { SetProperty(ref _selectedUser, value); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { SetProperty(ref _currentPage, value); OnPropertyChanged(nameof(PageInfo)); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { SetProperty(ref _totalPages, value); OnPropertyChanged(nameof(PageInfo)); OnPropertyChanged(nameof(CanGoNext)); }
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

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        public ICommand RefreshCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        public ActivityLogViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
            ApplyFiltersCommand = new RelayCommand(async _ => { CurrentPage = 1; await LoadActivityLogsAsync(); });
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => CanGoPrevious);
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => CanGoNext);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            
            // Проверяем подключение к API
            if (_settings.UseApi)
            {
                await _apiService.CheckConnectionAsync();
            }
            
            IsApiConnected = _settings.UseApi && _apiService.IsConnected;

            if (!IsApiConnected)
            {
                IsLoading = false;
                return;
            }

            await Task.WhenAll(
                LoadStatsAsync(),
                LoadActivityLogsAsync()
            );

            IsLoading = false;
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var stats = await _apiService.GetActivityLogStatsAsync();
                if (stats != null)
                {
                    Stats = stats;

                    Users.Clear();
                    Users.Add(new ActivityUserStat { UserId = 0, UserName = "Все пользователи" });
                    foreach (var user in stats.ByUser)
                    {
                        Users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadStatsAsync error: {ex.Message}");
            }
        }

        private async Task LoadActivityLogsAsync()
        {
            try
            {
                string? source = SelectedSource != "Все" ? SelectedSource : null;
                int? userId = SelectedUser?.UserId > 0 ? SelectedUser.UserId : null;

                var result = await _apiService.GetActivityLogsAsync(CurrentPage, 50, source, userId);
                if (result != null)
                {
                    ActivityLogs.Clear();
                    foreach (var log in result.Items)
                    {
                        ActivityLogs.Add(log);
                    }
                    TotalPages = result.TotalPages > 0 ? result.TotalPages : 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadActivityLogsAsync error: {ex.Message}");
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                OnPropertyChanged(nameof(CanGoPrevious));
                await LoadActivityLogsAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (CanGoNext)
            {
                CurrentPage++;
                OnPropertyChanged(nameof(CanGoPrevious));
                await LoadActivityLogsAsync();
            }
        }
    }
}
