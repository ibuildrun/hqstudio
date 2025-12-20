using HQStudio.Models;
using HQStudio.Services;
using System.Collections.ObjectModel;

namespace HQStudio.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;

        public ObservableCollection<Service> FeaturedServices { get; } = new();
        public ObservableCollection<ServiceStatistic> ServiceStatistics { get; } = new();

        private int _totalClients;
        private int _totalOrders;
        private int _activeOrders;
        private decimal _totalRevenue;
        private int _newCallbacks;
        private bool _isApiConnected;

        public int TotalClients
        {
            get => _totalClients;
            set => SetProperty(ref _totalClients, value);
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        public int ActiveOrders
        {
            get => _activeOrders;
            set => SetProperty(ref _activeOrders, value);
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public int NewCallbacks
        {
            get => _newCallbacks;
            set => SetProperty(ref _newCallbacks, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        public DashboardViewModel()
        {
            LoadData();
        }

        private async void LoadData()
        {
            // Try API first
            if (_settings.UseApi && _apiService.IsConnected)
            {
                IsApiConnected = true;
                var stats = await _apiService.GetDashboardStatsAsync();
                if (stats != null)
                {
                    TotalClients = stats.TotalClients;
                    TotalOrders = stats.TotalOrders;
                    ActiveOrders = stats.OrdersInProgress;
                    TotalRevenue = stats.MonthlyRevenue;
                    NewCallbacks = stats.NewCallbacks;

                    ServiceStatistics.Clear();
                    foreach (var s in stats.PopularServices)
                    {
                        ServiceStatistics.Add(new ServiceStatistic
                        {
                            Service = new Service { Name = s.Name },
                            OrderCount = s.Count
                        });
                    }
                    return;
                }
            }

            // Fallback to local data
            IsApiConnected = false;
            TotalClients = _dataService.Clients.Count;
            TotalOrders = _dataService.Orders.Count;
            ActiveOrders = _dataService.Orders.Count(o => o.Status != "Завершен");
            TotalRevenue = _dataService.Orders.Where(o => o.Status == "Завершен").Sum(o => o.TotalPrice);

            foreach (var service in _dataService.Services.Where(s => s.IsActive).Take(6))
            {
                FeaturedServices.Add(service);
            }

            LoadServiceStatistics();
        }

        private void LoadServiceStatistics()
        {
            ServiceStatistics.Clear();

            var stats = _dataService.Services
                .Select(service => new ServiceStatistic
                {
                    Service = service,
                    OrderCount = _dataService.Orders.Count(o => o.ServiceIds.Contains(service.Id)),
                    TotalRevenue = _dataService.Orders
                        .Where(o => o.ServiceIds.Contains(service.Id) && o.Status == "Завершен")
                        .Sum(o => o.TotalPrice / Math.Max(o.ServiceIds.Count, 1))
                })
                .Where(s => s.OrderCount > 0)
                .OrderByDescending(s => s.OrderCount)
                .Take(5);

            foreach (var stat in stats)
            {
                ServiceStatistics.Add(stat);
            }
        }
    }
}
