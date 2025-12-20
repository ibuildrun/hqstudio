using HQStudio.Services;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private BaseViewModel? _currentView;
        private string _currentViewName = "Dashboard";

        public BaseViewModel? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentViewName
        {
            get => _currentViewName;
            set => SetProperty(ref _currentViewName, value);
        }

        public string UserDisplayName => _dataService.CurrentUser?.DisplayName ?? "Гость";
        public string UserRole => _dataService.CurrentUser?.Role ?? "";

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand? RefreshCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            LogoutCommand = new RelayCommand(_ => Logout());
            RefreshCommand = new RelayCommand(_ => Refresh());
            CurrentView = new DashboardViewModel();
        }

        private void Navigate(object? parameter)
        {
            if (parameter is not string viewName) return;

            CurrentViewName = viewName;
            CurrentView = viewName switch
            {
                "Dashboard" => new DashboardViewModel(),
                "Services" => new ServicesViewModel(),
                "Clients" => new ClientsViewModel(),
                "Orders" => new OrdersViewModel(),
                "Staff" => new StaffViewModel(),
                "Settings" => new SettingsViewModel(),
                _ => CurrentView
            };
        }

        private void Refresh()
        {
            // Перезагружаем текущее представление
            Navigate(CurrentViewName);
        }

        private void Logout()
        {
            _dataService.Logout();
        }
    }
}
