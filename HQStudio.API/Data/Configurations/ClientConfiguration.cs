using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(c => c.Email)
            .HasMaxLength(200);
        
        builder.Property(c => c.CarModel)
            .HasMaxLength(200);
        
        builder.Property(c => c.LicensePlate)
            .HasMaxLength(20);
        
        builder.Property(c => c.Notes)
            .HasMaxLength(1000);
        
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.CreatedAt);
    }
}
