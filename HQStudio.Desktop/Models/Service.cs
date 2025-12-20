namespace HQStudio.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal PriceFrom { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
