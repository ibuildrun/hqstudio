using HQStudio.Models;
using HQStudio.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HQStudio.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly RecentItemsService _recentItemsService = RecentItemsService.Instance;

        public ObservableCollection<Service> FeaturedServices { get; } = new();
        public ObservableCollection<ServiceStatistic> ServiceStatistics { get; } = new();

        /// <summary>
        /// Коллекция недавно просмотренных элементов
        /// </summary>
        public ObservableCollection<RecentItem> RecentItems => _recentItemsService.RecentItems;

        private int _totalClients;
        private int _totalOrders;
        private int _activeOrders;
        private decimal _totalRevenue;
        private int _newCallbacks;
        private bool _isApiConnected;
        private ISeries[] _revenueSeries = Array.Empty<ISeries>();
        private Axis[] _revenueXAxes = Array.Empty<Axis>();
        private Axis[] _revenueYAxes = Array.Empty<Axis>();

        /// <summary>Название текущего месяца для отображения</summary>
        public string CurrentMonthName => DateTime.Today.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));

        /// <summary>Подсказка для карточки выручки</summary>
        public string RevenueTooltip => $"Выручка за {CurrentMonthName}\n(только завершённые заказы)";

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

        public ISeries[] RevenueSeries
        {
            get => _revenueSeries;
            set => SetProperty(ref _revenueSeries, value);
        }

        public Axis[] RevenueXAxes
        {
            get => _revenueXAxes;
            set => SetProperty(ref _revenueXAxes, value);
        }

        public Axis[] RevenueYAxes
        {
            get => _revenueYAxes;
            set => SetProperty(ref _revenueYAxes, value);
        }

        public ICommand GenerateWeeklyReportCommand { get; }
        public ICommand GenerateMonthlyReportCommand { get; }
        public ICommand NavigateToRecentItemCommand { get; }
        public ICommand NavigateToClientsCommand { get; }
        public ICommand NavigateToOrdersCommand { get; }
        public ICommand NavigateToActiveOrdersCommand { get; }
        public ICommand NavigateToRevenueCommand { get; }

        /// <summary>
        /// Событие для навигации к элементу (обрабатывается MainWindow)
        /// </summary>
        public event Action<RecentItem>? NavigateToRecentItem;

        /// <summary>
        /// Событие для навигации к разделу
        /// </summary>
        public event Action<string>? NavigateToSection;

        public DashboardViewModel()
        {
            GenerateWeeklyReportCommand = new RelayCommand(_ => GenerateWeeklyReport());
            GenerateMonthlyReportCommand = new RelayCommand(_ => GenerateMonthlyReport());
            NavigateToRecentItemCommand = new RelayCommand(OnNavigateToRecentItem);
            NavigateToClientsCommand = new RelayCommand(_ => NavigateToSection?.Invoke("Clients"));
            NavigateToOrdersCommand = new RelayCommand(_ => NavigateToSection?.Invoke("Orders"));
            NavigateToActiveOrdersCommand = new RelayCommand(_ => NavigateToSection?.Invoke("ActiveOrders"));
            NavigateToRevenueCommand = new RelayCommand(_ => NavigateToSection?.Invoke("Analytics"));
            LoadData();
        }

        private void OnNavigateToRecentItem(object? parameter)
        {
            if (parameter is RecentItem item)
            {
                NavigateToRecentItem?.Invoke(item);
            }
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
                    
                    LoadRevenueChart();
                    return;
                }
            }

            // Fallback to local data
            IsApiConnected = false;
            TotalClients = _dataService.Clients.Count;
            TotalOrders = _dataService.Orders.Count;
            
            var completedStatus = OrderStatus.Completed.DisplayName;
            var cancelledStatus = OrderStatus.Cancelled.DisplayName;
            
            ActiveOrders = _dataService.Orders.Count(o => o.Status != completedStatus && o.Status != cancelledStatus);
            
            // Выручка за текущий месяц (как в API)
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            TotalRevenue = _dataService.Orders
                .Where(o => o.Status == completedStatus && o.CompletedAt.HasValue && o.CompletedAt.Value >= monthStart)
                .Sum(o => o.TotalPrice);

            foreach (var service in _dataService.Services.Where(s => s.IsActive).Take(6))
            {
                FeaturedServices.Add(service);
            }

            LoadServiceStatistics();
            LoadRevenueChart();
        }

        private void LoadServiceStatistics()
        {
            ServiceStatistics.Clear();
            var completedStatus = OrderStatus.Completed.DisplayName;

            var stats = _dataService.Services
                .Select(service => new ServiceStatistic
                {
                    Service = service,
                    OrderCount = _dataService.Orders.Count(o => o.ServiceIds.Contains(service.Id)),
                    TotalRevenue = _dataService.Orders
                        .Where(o => o.ServiceIds.Contains(service.Id) && o.Status == completedStatus)
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

        private void LoadRevenueChart()
        {
            try
            {
                var days = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-6 + i))
                    .ToList();

                var labels = days.Select(d => d.ToString("dd.MM")).ToArray();
                var hasRealData = _dataService.Orders.Any();
                var completedStatus = OrderStatus.Completed.DisplayName;
                
                var values = days.Select(d =>
                {
                    // Выручка по дате завершения (CompletedAt), не по дате создания
                    var dayRevenue = (double)_dataService.Orders
                        .Where(o => o.Status == completedStatus && 
                                    o.CompletedAt.HasValue && 
                                    o.CompletedAt.Value.Date == d)
                        .Sum(o => o.TotalPrice);
                    
                    // Если нет реальных данных, показываем демо-значения
                    if (!hasRealData)
                    {
                        var random = new Random(d.DayOfYear);
                        return random.Next(8000, 35000);
                    }
                    
                    return dayRevenue;
                }).ToArray();

                RevenueSeries = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = values,
                        Fill = new SolidColorPaint(new SKColor(76, 175, 80)),
                        Stroke = null,
                        MaxBarWidth = 30
                    }
                };

                RevenueXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels,
                        LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                        TextSize = 11
                    }
                };

                RevenueYAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                        TextSize = 11,
                        Labeler = value => $"{value:N0} ₽",
                        MinLimit = 0
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRevenueChart error: {ex.Message}");
            }
        }

        private void GenerateWeeklyReport()
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-7);
                var reportData = BuildReportData(startDate, endDate);
                
                var pdfBytes = ReportService.Instance.GenerateWeeklyReport(reportData);
                
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"HQStudio_Weekly_{endDate:yyyy-MM-dd}",
                    DefaultExt = ".pdf",
                    Filter = "PDF документы|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    ReportService.Instance.SaveReport(pdfBytes, dialog.FileName);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void GenerateMonthlyReport()
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = new DateTime(endDate.Year, endDate.Month, 1);
                var reportData = BuildReportData(startDate, endDate);
                
                var pdfBytes = ReportService.Instance.GenerateMonthlyReport(reportData);
                
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"HQStudio_Monthly_{endDate:yyyy-MM}",
                    DefaultExt = ".pdf",
                    Filter = "PDF документы|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    ReportService.Instance.SaveReport(pdfBytes, dialog.FileName);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private ReportData BuildReportData(DateTime startDate, DateTime endDate)
        {
            var orders = _dataService.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate.AddDays(1))
                .ToList();

            var dailyRevenue = orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Where(o => o.Status == "Завершен").Sum(o => o.TotalPrice)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var serviceStats = _dataService.Services
                .Select(s => new ReportServiceStat
                {
                    ServiceName = s.Name,
                    Count = orders.Count(o => o.ServiceIds.Contains(s.Id)),
                    Revenue = orders
                        .Where(o => o.ServiceIds.Contains(s.Id) && o.Status == "Завершен")
                        .Sum(o => o.TotalPrice / Math.Max(o.ServiceIds.Count, 1))
                })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .ToList();

            var orderSummaries = orders
                .Select(o => new OrderSummary
                {
                    Id = o.Id,
                    ClientName = _dataService.Clients.FirstOrDefault(c => c.Id == o.ClientId)?.Name ?? "—",
                    Date = o.CreatedAt,
                    Status = o.Status,
                    TotalPrice = o.TotalPrice
                })
                .OrderByDescending(o => o.Date)
                .ToList();

            return new ReportData
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.Status == "Завершен"),
                InProgressOrders = orders.Count(o => o.Status == "В работе"),
                TotalRevenue = orders.Where(o => o.Status == "Завершен").Sum(o => o.TotalPrice),
                DailyRevenue = dailyRevenue,
                ServiceStats = serviceStats,
                Orders = orderSummaries
            };
        }
    }
}
