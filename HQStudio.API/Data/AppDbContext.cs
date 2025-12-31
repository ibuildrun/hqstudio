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
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем все конфигурации из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
