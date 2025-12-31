using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class OrderServiceConfiguration : IEntityTypeConfiguration<OrderService>
{
    public void Configure(EntityTypeBuilder<OrderService> builder)
    {
        builder.HasKey(os => new { os.OrderId, os.ServiceId });
        
        builder.Property(os => os.Price)
            .HasPrecision(18, 2);
        
        builder.HasOne(os => os.Order)
            .WithMany(o => o.OrderServices)
            .HasForeignKey(os => os.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(os => os.Service)
            .WithMany(s => s.OrderServices)
            .HasForeignKey(os => os.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
