using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HQStudio.Models
{
    public class Order : INotifyPropertyChanged
    {
        private int _id;
        private int _clientId;
        private decimal _totalPrice;
        private DateTime _createdAt = DateTime.Now;
        private DateTime? _completedAt;
        private string _status = "Новый";
        private string _notes = string.Empty;
        private string _clientName = string.Empty;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int ClientId
        {
            get => _clientId;
            set { _clientId = value; OnPropertyChanged(); }
        }

        public Client? Client { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set { _completedAt = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClientDisplay)); }
        }

        public string ServicesDisplay => Services.Any()
            ? string.Join(", ", Services.Select(s => s.Name))
            : "Не указаны";

        public string ClientDisplay => !string.IsNullOrEmpty(ClientName)
            ? ClientName
            : Client?.Name ?? "Клиент не указан";

        public string StatusColor => Status switch
        {
            "Новый" => "#2196F3",
            "В работе" => "#FF9800",
            "Завершен" => "#4CAF50",
            "Отменен" => "#F44336",
            _ => "#9E9E9E"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
