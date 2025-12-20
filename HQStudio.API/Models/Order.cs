namespace HQStudio.API.Models;

public enum OrderStatus
{
    New,
    InProgress,
    Completed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}

public class OrderService
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public decimal Price { get; set; }
}
