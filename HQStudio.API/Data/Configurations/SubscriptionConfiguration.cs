using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.HasIndex(s => s.Email)
            .IsUnique();
    }
}
