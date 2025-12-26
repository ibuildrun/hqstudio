using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Timer = System.Timers.Timer;

namespace HQStudio.Services
{
    /// <summary>
    /// Тип toast-уведомления
    /// </summary>
    public enum ToastType
    {
        Success,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Модель toast-уведомления
    /// </summary>
    public class ToastNotification : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public Guid Id { get; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public ToastType Type { get; set; }
        public DateTime CreatedAt { get; } = DateTime.Now;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Сервис для отображения toast-уведомлений
    /// </summary>
    public class ToastService : INotifyPropertyChanged
    {
        private static ToastService? _instance;
        public static ToastService Instance => _instance ??= new ToastService();

        private const int AutoDismissMs = 3000;
        private const int MaxNotifications = 5;

        public ObservableCollection<ToastNotification> Notifications { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private ToastService() { }

        /// <summary>
        /// Показать успешное уведомление
        /// </summary>
        public void ShowSuccess(string message)
        {
            Show(message, ToastType.Success);
        }

        /// <summary>
        /// Показать информационное уведомление
        /// </summary>
        public void ShowInfo(string message)
        {
            Show(message, ToastType.Info);
        }

        /// <summary>
        /// Показать предупреждение
        /// </summary>
        public void ShowWarning(string message)
        {
            Show(message, ToastType.Warning);
        }

        /// <summary>
        /// Показать ошибку
        /// </summary>
        public void ShowError(string message)
        {
            Show(message, ToastType.Error);
        }

        /// <summary>
        /// Показать уведомление указанного типа
        /// </summary>
        public void Show(string message, ToastType type)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var notification = new ToastNotification
                {
                    Message = message,
                    Type = type
                };

                // Ограничиваем количество уведомлений
                while (Notifications.Count >= MaxNotifications)
                {
                    Notifications.RemoveAt(0);
                }

                Notifications.Add(notification);

                // Автоскрытие через 3 секунды
                var timer = new Timer(AutoDismissMs);
                timer.Elapsed += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    Dismiss(notification);
                };
                timer.Start();
            });
        }

        /// <summary>
        /// Скрыть уведомление
        /// </summary>
        public void Dismiss(ToastNotification notification)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                notification.IsVisible = false;
                // Небольшая задержка для анимации
                var removeTimer = new Timer(300);
                removeTimer.Elapsed += (s, e) =>
                {
                    removeTimer.Stop();
                    removeTimer.Dispose();
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        Notifications.Remove(notification);
                    });
                };
                removeTimer.Start();
            });
        }

        /// <summary>
        /// Очистить все уведомления
        /// </summary>
        public void ClearAll()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Notifications.Clear();
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
