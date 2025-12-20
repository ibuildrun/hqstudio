namespace HQStudio.API.Models;

public class Service
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string? Image { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    
    public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
}
