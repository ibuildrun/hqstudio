using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HQStudio.API.Models;

namespace HQStudio.API.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.HasIndex(u => u.Login)
            .IsUnique();
        
        builder.HasIndex(u => u.IsActive);
    }
}
