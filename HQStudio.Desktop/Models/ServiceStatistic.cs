namespace HQStudio.Models
{
    public class ServiceStatistic
    {
        public Service Service { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
