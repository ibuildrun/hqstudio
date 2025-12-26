using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HQStudio.Services
{
    /// <summary>
    /// Тип недавно просмотренного элемента
    /// </summary>
    public enum RecentItemType
    {
        Client,
        Order,
        Service
    }

    /// <summary>
    /// Модель недавно просмотренного элемента
    /// </summary>
    public class RecentItem : INotifyPropertyChanged
    {
        public RecentItemType Type { get; set; }
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime ViewedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Иконка в зависимости от типа
        /// </summary>
        public string Icon => Type switch
        {
            RecentItemType.Client => "👤",
            RecentItemType.Order => "📋",
            RecentItemType.Service => "🔧",
            _ => "📄"
        };

        /// <summary>
        /// Название типа на русском
        /// </summary>
        public string TypeName => Type switch
        {
            RecentItemType.Client => "Клиент",
            RecentItemType.Order => "Заказ",
            RecentItemType.Service => "Услуга",
            _ => "Элемент"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// Сервис для отслеживания последних просмотренных элементов
    /// </summary>
    public class RecentItemsService : INotifyPropertyChanged
    {
        private static RecentItemsService? _instance;
        public static RecentItemsService Instance => _instance ??= new RecentItemsService();

        private const int MaxRecentItems = 10;
        private readonly string _storagePath;

        /// <summary>
        /// Коллекция недавних элементов (отсортирована по времени просмотра, новые первые)
        /// </summary>
        public ObservableCollection<RecentItem> RecentItems { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private RecentItemsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "HQStudio");
            Directory.CreateDirectory(appDataPath);
            _storagePath = Path.Combine(appDataPath, "recent_items.json");
            LoadFromStorage();
        }

        /// <summary>
        /// Конструктор для тестирования с указанием пути хранения
        /// </summary>
        public RecentItemsService(string storagePath)
        {
            _storagePath = storagePath;
            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            LoadFromStorage();
        }

        /// <summary>
        /// Добавить элемент в список недавних
        /// </summary>
        public void AddRecentItem(RecentItemType type, int id, string title, string subtitle = "")
        {
            // Удаляем существующий элемент с таким же типом и ID
            var existing = RecentItems.FirstOrDefault(r => r.Type == type && r.Id == id);
            if (existing != null)
            {
                RecentItems.Remove(existing);
            }

            // Добавляем новый элемент в начало
            var newItem = new RecentItem
            {
                Type = type,
                Id = id,
                Title = title,
                Subtitle = subtitle,
                ViewedAt = DateTime.Now
            };

            RecentItems.Insert(0, newItem);

            // Ограничиваем количество элементов до 10
            while (RecentItems.Count > MaxRecentItems)
            {
                RecentItems.RemoveAt(RecentItems.Count - 1);
            }

            SaveToStorage();
            OnPropertyChanged(nameof(RecentItems));
        }

        /// <summary>
        /// Добавить клиента в недавние
        /// </summary>
        public void AddRecentClient(int id, string name, string phone = "")
        {
            AddRecentItem(RecentItemType.Client, id, name, phone);
        }

        /// <summary>
        /// Добавить заказ в недавние
        /// </summary>
        public void AddRecentOrder(int id, string clientName, string status = "")
        {
            var title = $"Заказ #{id}";
            var subtitle = string.IsNullOrEmpty(status) ? clientName : $"{clientName} • {status}";
            AddRecentItem(RecentItemType.Order, id, title, subtitle);
        }

        /// <summary>
        /// Добавить услугу в недавние
        /// </summary>
        public void AddRecentService(int id, string name, string category = "")
        {
            AddRecentItem(RecentItemType.Service, id, name, category);
        }

        /// <summary>
        /// Получить недавние элементы определённого типа
        /// </summary>
        public IEnumerable<RecentItem> GetRecentByType(RecentItemType type)
        {
            return RecentItems.Where(r => r.Type == type);
        }

        /// <summary>
        /// Очистить все недавние элементы
        /// </summary>
        public void ClearAll()
        {
            RecentItems.Clear();
            SaveToStorage();
            OnPropertyChanged(nameof(RecentItems));
        }

        /// <summary>
        /// Удалить конкретный элемент
        /// </summary>
        public void RemoveItem(RecentItemType type, int id)
        {
            var item = RecentItems.FirstOrDefault(r => r.Type == type && r.Id == id);
            if (item != null)
            {
                RecentItems.Remove(item);
                SaveToStorage();
                OnPropertyChanged(nameof(RecentItems));
            }
        }

        /// <summary>
        /// Загрузить из хранилища
        /// </summary>
        public void LoadFromStorage()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    var items = JsonSerializer.Deserialize<List<RecentItem>>(json);
                    if (items != null)
                    {
                        RecentItems.Clear();
                        foreach (var item in items.Take(MaxRecentItems))
                        {
                            RecentItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Если не удалось загрузить, начинаем с пустого списка
                RecentItems.Clear();
            }
        }

        /// <summary>
        /// Сохранить в хранилище
        /// </summary>
        public void SaveToStorage()
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    RecentItems.ToList(), 
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception)
            {
                // Игнорируем ошибки сохранения
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
