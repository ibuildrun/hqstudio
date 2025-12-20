using Microsoft.EntityFrameworkCore;
using HQStudio.API.Models;

namespace HQStudio.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderService> OrderServices => Set<OrderService>();
    public DbSet<CallbackRequest> CallbackRequests => Set<CallbackRequest>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SiteContent> SiteContents => Set<SiteContent>();
    public DbSet<SiteBlock> SiteBlocks => Set<SiteBlock>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<ShowcaseItem> ShowcaseItems => Set<ShowcaseItem>();
    public DbSet<AppUpdate> AppUpdates => Set<AppUpdate>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderService>()
            .HasKey(os => new { os.OrderId, os.ServiceId });

        modelBuilder.Entity<OrderService>()
            .HasOne(os => os.Order)
            .WithMany(o => o.OrderServices)
            .HasForeignKey(os => os.OrderId);

        modelBuilder.Entity<OrderService>()
            .HasOne(os => os.Service)
            .WithMany(s => s.OrderServices)
            .HasForeignKey(os => os.ServiceId);

        modelBuilder.Entity<SiteContent>()
            .HasIndex(sc => sc.Key)
            .IsUnique();

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();
    }
}
