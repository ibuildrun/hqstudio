using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.Description)
            .HasMaxLength(2000);
        
        builder.Property(s => s.Price)
            .HasMaxLength(100);
        
        builder.Property(s => s.Image)
            .HasMaxLength(500);
        
        builder.Property(s => s.Icon)
            .HasMaxLength(50)
            .HasDefaultValue("🔧");
        
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.SortOrder);
    }
}
