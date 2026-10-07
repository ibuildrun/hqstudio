using HQStudio.Services;
using HQStudio.Services.Updates;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class MainWindow : Window
    {
        private readonly HotkeyService _hotkeyService = HotkeyService.Instance;
        private readonly SystemNotificationService _systemNotificationService = SystemNotificationService.Instance;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeSystemNotifications();
                InitializeNotifications();
                InitializeHotkeys();
                InitializeUpdates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации MainWindow: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private EventHandler? _updateStatusHandler;

        /// <summary>
        /// "Available" badge on the Updates entry and the silent startup check
        /// </summary>
        private void InitializeUpdates()
        {
            // Screenshot mode must not hit the network or show toasts
            if (ScreenshotService.IsScreenshotMode)
                return;

            try
            {
                var coordinator = UpdateCoordinator.Instance;
                _updateStatusHandler = (_, _) => Dispatcher.BeginInvoke(RefreshUpdateBadge);
                coordinator.StatusChanged += _updateStatusHandler;
                RefreshUpdateBadge();

                Loaded += async (_, _) => await coordinator.RunStartupCheckAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeUpdates error: {ex.Message}");
            }
        }

        private void RefreshUpdateBadge()
        {
            UpdateBadge.Visibility = UpdateCoordinator.Instance.Status.AnyUpdateAvailable
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void UpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.UpdateDialog.ShowFor(this);
        }

        /// <summary>
        /// Инициализация системных уведомлений Windows (в трее)
        /// </summary>
        private void InitializeSystemNotifications()
        {
            try
            {
                // Инициализируем сервис системных уведомлений
                _systemNotificationService.Initialize(this);

                // Обработка клика на уведомление - навигация к заявкам
                _systemNotificationService.OnNotificationClicked += (target) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext is MainViewModel vm && target == "Callbacks")
                        {
                            vm.NavigateCommand.Execute("Callbacks");
                        }
                    });
                };

                System.Diagnostics.Debug.WriteLine("SystemNotificationService initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeSystemNotifications error: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация горячих клавиш
        /// </summary>
        private void InitializeHotkeys()
        {
            try
            {
                // Регистрируем обработчики для горячих клавиш
                _hotkeyService.RegisterGlobalHotkeys(this);

                // Ctrl+S - Сохранить (делегируем текущему представлению)
                _hotkeyService.SetActionHandler("Save", ExecuteSaveHotkey, CanExecuteSaveHotkey);

                // Ctrl+N - Создать новый элемент
                _hotkeyService.SetActionHandler("New", ExecuteNewHotkey, CanExecuteNewHotkey);

                // Delete - Удалить выбранный элемент
                _hotkeyService.SetActionHandler("Delete", ExecuteDeleteHotkey, CanExecuteDeleteHotkey);

                // Ctrl+F - Фокус на поиск
                _hotkeyService.SetActionHandler("FocusSearch", ExecuteFocusSearchHotkey, CanExecuteFocusSearchHotkey);

                // Escape - Закрыть диалог (обрабатывается в диалогах)
                _hotkeyService.SetActionHandler("CloseDialog", ExecuteCloseDialogHotkey, CanExecuteCloseDialogHotkey);

                System.Diagnostics.Debug.WriteLine("Hotkeys initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeHotkeys error: {ex.Message}");
            }
        }

        #region Hotkey Handlers

        private bool CanExecuteSaveHotkey()
        {
            // Сохранение доступно если текущее представление поддерживает сохранение
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                // Проверяем, есть ли у текущего представления команда сохранения
                return vm.CurrentView is OrdersViewModel or ClientsViewModel or ServicesViewModel;
            }
            return false;
        }

        private void ExecuteSaveHotkey()
        {
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                // Для заказов, клиентов и услуг - показываем toast что сохранение автоматическое
                ToastService.Instance.ShowInfo("Изменения сохраняются автоматически");
            }
        }

        private bool CanExecuteNewHotkey()
        {
            // Создание нового элемента доступно в списковых представлениях
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                return vm.CurrentView is OrdersViewModel or ClientsViewModel or ServicesViewModel or StaffViewModel;
            }
            return false;
        }

        private void ExecuteNewHotkey()
        {
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                switch (vm.CurrentView)
                {
                    case OrdersViewModel ordersVm:
                        ordersVm.AddOrderCommand.Execute(null);
                        break;
                    case ClientsViewModel clientsVm:
                        clientsVm.AddClientCommand.Execute(null);
                        break;
                    case ServicesViewModel servicesVm:
                        servicesVm.AddServiceCommand.Execute(null);
                        break;
                    case StaffViewModel staffVm:
                        staffVm.AddUserCommand.Execute(null);
                        break;
                }
            }
        }

        private bool CanExecuteDeleteHotkey()
        {
            // Удаление доступно если есть выбранный элемент
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                return vm.CurrentView switch
                {
                    OrdersViewModel ordersVm => ordersVm.SelectedOrder != null,
                    ClientsViewModel clientsVm => clientsVm.SelectedClient != null,
                    ServicesViewModel servicesVm => servicesVm.SelectedService != null,
                    StaffViewModel staffVm => staffVm.SelectedUser != null,
                    _ => false
                };
            }
            return false;
        }

        private void ExecuteDeleteHotkey()
        {
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                switch (vm.CurrentView)
                {
                    case OrdersViewModel ordersVm:
                        ordersVm.DeleteOrderCommand.Execute(null);
                        break;
                    case ClientsViewModel clientsVm:
                        clientsVm.DeleteClientCommand.Execute(null);
                        break;
                    case ServicesViewModel servicesVm:
                        servicesVm.DeleteServiceCommand.Execute(null);
                        break;
                    case StaffViewModel staffVm:
                        staffVm.DeleteUserCommand.Execute(null);
                        break;
                }
            }
        }

        private bool CanExecuteFocusSearchHotkey()
        {
            // Поиск доступен в списковых представлениях
            if (DataContext is MainViewModel vm && vm.CurrentView != null)
            {
                return vm.CurrentView is OrdersViewModel or ClientsViewModel or ServicesViewModel;
            }
            return false;
        }

        private void ExecuteFocusSearchHotkey()
        {
            // Ищем поле поиска в текущем представлении
            var searchBox = FindSearchTextBox();
            if (searchBox != null)
            {
                searchBox.Focus();
                searchBox.SelectAll();
            }
        }

        private TextBox? FindSearchTextBox()
        {
            // Ищем TextBox с именем SearchBox или тегом "SearchBox" в визуальном дереве
            return FindVisualChild<TextBox>(this, tb => 
                tb.Name == "SearchBox" || 
                tb.Tag?.ToString() == "SearchBox" ||
                tb.Name?.Contains("Search") == true);
        }

        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild && (predicate == null || predicate(typedChild)))
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private bool CanExecuteCloseDialogHotkey()
        {
            // Escape закрывает диалоги - проверяем, есть ли открытые диалоги
            return false; // Диалоги обрабатывают Escape самостоятельно
        }

        private void ExecuteCloseDialogHotkey()
        {
            // Диалоги обрабатывают Escape самостоятельно через IsCancel на кнопке
        }

        #endregion

        private void InitializeNotifications()
        {
            try
            {
                // Запускаем polling для уведомлений
                var notifications = NotificationService.Instance;
                
                notifications.OnNewCallback += (name, phone) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            vm.HasNewNotifications = true;
                        }

                        // Показываем системное уведомление если приложение свёрнуто
                        _systemNotificationService.ShowNewCallbackNotification(name, phone);
                    });
                };

                notifications.OnNewOrder += (client, orderId) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            vm.HasNewNotifications = true;
                        }

                        // Показываем системное уведомление если приложение свёрнуто
                        if (int.TryParse(orderId.Replace("#", ""), out int id))
                        {
                            _systemNotificationService.ShowNewOrderNotification(client, id);
                        }
                    });
                };

                // Запускаем polling (каждые 30 секунд)
                notifications.StartPolling(30);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeNotifications error: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            NotificationService.Instance.StopPolling();
            if (_updateStatusHandler != null)
                UpdateCoordinator.Instance.StatusChanged -= _updateStatusHandler;
            _hotkeyService.UnregisterHotkeys();
            _systemNotificationService.Dispose();
            base.OnClosed(e);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestore();
        }

        private void MaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Навигация по разделам через Alt + цифры
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                switch (e.Key)
                {
                    case Key.D1:
                        vm.NavigateCommand.Execute("Dashboard");
                        e.Handled = true;
                        break;
                    case Key.D2:
                        vm.NavigateCommand.Execute("Services");
                        e.Handled = true;
                        break;
                    case Key.D3:
                        vm.NavigateCommand.Execute("Clients");
                        e.Handled = true;
                        break;
                    case Key.D4:
                        vm.NavigateCommand.Execute("Orders");
                        e.Handled = true;
                        break;
                    case Key.D5:
                        vm.NavigateCommand.Execute("Staff");
                        e.Handled = true;
                        break;
                    case Key.D6:
                        vm.NavigateCommand.Execute("Settings");
                        e.Handled = true;
                        break;
                }
            }
            
            // F5 - обновить данные
            if (e.Key == Key.F5)
            {
                vm.RefreshCommand?.Execute(null);
                e.Handled = true;
            }
        }
    }
}
