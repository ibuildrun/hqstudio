using HQStudio.Models;
using HQStudio.Services;
using System.Reflection;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private BaseViewModel? _currentView;
        private string _currentViewName = "Dashboard";
        private int? _pendingOrderId;
        private bool _hasNewNotifications;

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

        public bool HasNewNotifications
        {
            get => _hasNewNotifications;
            set => SetProperty(ref _hasNewNotifications, value);
        }

        public string UserDisplayName => _dataService.CurrentUser?.DisplayName ?? "Гость";
        public string UserRole => _dataService.CurrentUser?.Role ?? "";

        /// <summary>Страница «Сайт» управляет сервером, поэтому видна только администратору.</summary>
        public bool IsAdmin => AdminAccess.IsAdmin(UserRole);

        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}" : "v1.0";
            }
        }

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand? RefreshCommand { get; }
        public ICommand ClearNotificationsCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            LogoutCommand = new RelayCommand(_ => Logout());
            RefreshCommand = new RelayCommand(_ => Refresh());
            ClearNotificationsCommand = new RelayCommand(_ => HasNewNotifications = false);
            
            try
            {
                CurrentView = CreateDashboardViewModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DashboardViewModel error: {ex}");
                System.Windows.MessageBox.Show($"Ошибка загрузки Dashboard: {ex.Message}", "Ошибка");
            }
        }

        private void Navigate(object? parameter)
        {
            if (parameter is not string viewName) return;
            if (viewName == "Site" && !IsAdmin) return;

            // Для ActiveOrders используем Orders для подсветки меню
            CurrentViewName = viewName == "ActiveOrders" ? "Orders" : viewName;
            try
            {
                CurrentView = viewName switch
                {
                    "Dashboard" => CreateDashboardViewModel(),
                    "Analytics" => new AnalyticsViewModel(),
                    "Services" => new ServicesViewModel(),
                    "Clients" => new ClientsViewModel(),
                    "Orders" => CreateOrdersViewModel(),
                    "ActiveOrders" => CreateOrdersViewModel(filterActive: true),
                    "Callbacks" => CreateCallbacksViewModel(),
                    "Staff" => new StaffViewModel(),
                    "ActivityLog" => new ActivityLogViewModel(),
                    "Settings" => new SettingsViewModel(),
                    "Site" => new SiteViewModel(),
                    _ => CurrentView
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigate error: {ex}");
                System.Windows.MessageBox.Show($"Ошибка навигации: {ex.Message}", "Ошибка");
            }
        }

        private DashboardViewModel CreateDashboardViewModel()
        {
            var vm = new DashboardViewModel();
            vm.NavigateToSection += OnNavigateToSection;
            vm.NavigateToRecentItem += OnNavigateToRecentItem;
            return vm;
        }

        private void OnNavigateToSection(string section)
        {
            Navigate(section);
        }

        private void OnNavigateToRecentItem(RecentItem item)
        {
            // Навигация к элементу в зависимости от типа
            switch (item.Type)
            {
                case RecentItemType.Client:
                    Navigate("Clients");
                    break;
                case RecentItemType.Order:
                    if (item.Id > 0)
                    {
                        _pendingOrderId = item.Id;
                    }
                    Navigate("Orders");
                    break;
                case RecentItemType.Service:
                    Navigate("Services");
                    break;
            }
        }

        private OrdersViewModel CreateOrdersViewModel(bool filterActive = false)
        {
            var vm = new OrdersViewModel(filterActive);
            
            // Если есть отложенный заказ для выделения
            if (_pendingOrderId.HasValue)
            {
                var orderId = _pendingOrderId.Value;
                _pendingOrderId = null;
                
                // Выделяем заказ после загрузки
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OrdersViewModel.IsLoading) && !vm.IsLoading)
                    {
                        vm.SelectedOrder = vm.Orders.FirstOrDefault(o => o.Id == orderId);
                    }
                };
            }
            
            return vm;
        }

        private CallbacksViewModel CreateCallbacksViewModel()
        {
            var vm = new CallbacksViewModel();
            vm.NavigateToOrder += OnNavigateToOrder;
            return vm;
        }

        private void OnNavigateToOrder(int orderId)
        {
            _pendingOrderId = orderId;
            Navigate("Orders");
        }

        public void NavigateToOrder(int orderId)
        {
            _pendingOrderId = orderId;
            Navigate("Orders");
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
