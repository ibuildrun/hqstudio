using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class SiteContentConfiguration : IEntityTypeConfiguration<SiteContent>
{
    public void Configure(EntityTypeBuilder<SiteContent> builder)
    {
        builder.HasKey(sc => sc.Id);
        
        builder.Property(sc => sc.Key)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(sc => sc.Value)
            .HasMaxLength(10000);
        
        builder.HasIndex(sc => sc.Key)
            .IsUnique();
    }
}
