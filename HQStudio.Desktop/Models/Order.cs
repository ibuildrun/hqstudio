namespace HQStudio.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Новый";
        public string Notes { get; set; } = string.Empty;
        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }
        
        public string ServicesDisplay => Services.Any() 
            ? string.Join(", ", Services.Select(s => s.Name)) 
            : "Не указаны";
    }
}
