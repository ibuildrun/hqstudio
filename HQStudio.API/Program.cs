using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HQStudio.API.Data;
using HQStudio.API.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel - use environment variable or default to localhost:5000
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000";
builder.WebHost.UseUrls(urls);

// Database - автоопределение PostgreSQL или SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hqstudio.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
    {
        // PostgreSQL
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
        });
    }
    else
    {
        // SQLite (для локальной разработки и тестов)
        options.UseSqlite(connectionString);
    }
});

// Настройка Npgsql для работы с DateTime
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();

// Rate Limiting - защита от брутфорса и DDoS (отключено для тестов)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRateLimiter(options =>
    {
        // Общий лимит для всех запросов
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));

        // Строгий лимит для авторизации (защита от брутфорса)
        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Лимит для публичных форм (заявки, подписки) - защита от спама
        options.AddFixedWindowLimiter("public-forms", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(5);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Слишком много запросов. Попробуйте позже." }, token);
        };
    });
}

// CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://hqstudio.ru",
                "https://hqstudio.tuna.am")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HQ Studio API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-create database and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
    // Автоматическая миграция: добавление колонки Icon в Services если её нет
    try
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        
        if (connectionString.Contains("Host="))
        {
            // PostgreSQL
            command.CommandText = @"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Services' AND column_name='Icon') THEN
                        ALTER TABLE ""Services"" ADD COLUMN ""Icon"" TEXT NOT NULL DEFAULT '🔧';
                    END IF;
                END $$;";
        }
        else
        {
            // SQLite - проверяем наличие колонки
            command.CommandText = "PRAGMA table_info(Services)";
            var hasIconColumn = false;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(1) == "Icon")
                    {
                        hasIconColumn = true;
                        break;
                    }
                }
            }
            
            if (!hasIconColumn)
            {
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Services ADD COLUMN Icon TEXT NOT NULL DEFAULT '🔧'";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("Добавлена колонка Icon в таблицу Services");
            }
        }
        
        connection.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Миграция Icon: {ex.Message}");
    }
    
    // Команда сброса паролей пользователей (для разработки)
    if (args.Contains("--reset-passwords"))
    {
        Console.WriteLine("Сброс паролей пользователей...");
        var users = db.Users.ToList();
        foreach (var user in users)
        {
            // Устанавливаем пароль равный логину
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Login);
            Console.WriteLine($"  {user.Login} -> пароль: {user.Login}");
        }
        db.SaveChanges();
        Console.WriteLine("Пароли сброшены!");
        return;
    }
    
    // Команда очистки заказов без клиентов
    if (args.Contains("--cleanup-orders"))
    {
        Console.WriteLine("Очистка заказов без клиентов...");
        var ordersWithoutClients = db.Orders
            .Where(o => o.ClientId == 0 || !db.Clients.Any(c => c.Id == o.ClientId))
            .ToList();
        
        Console.WriteLine($"Найдено заказов без клиентов: {ordersWithoutClients.Count}");
        
        if (ordersWithoutClients.Any())
        {
            foreach (var order in ordersWithoutClients)
            {
                Console.WriteLine($"  - Заказ #{order.Id}, ClientId={order.ClientId}");
            }
            
            db.Orders.RemoveRange(ordersWithoutClients);
            db.SaveChanges();
            Console.WriteLine($"Удалено {ordersWithoutClients.Count} заказов.");
        }
        
        return;
    }
    
    // Команда экспорта данных в JSON
    if (args.Contains("--export-data"))
    {
        Console.WriteLine("Экспорт данных из базы...");
        var exportData = new
        {
            Users = db.Users.ToList(),
            Clients = db.Clients.ToList(),
            Services = db.Services.ToList(),
            Orders = db.Orders.ToList(),
            OrderServices = db.OrderServices.ToList(),
            CallbackRequests = db.CallbackRequests.ToList(),
            Subscriptions = db.Subscriptions.ToList(),
            SiteContents = db.SiteContents.ToList(),
            SiteBlocks = db.SiteBlocks.ToList(),
            Testimonials = db.Testimonials.ToList(),
            FaqItems = db.FaqItems.ToList(),
            ShowcaseItems = db.ShowcaseItems.ToList(),
            AppUpdates = db.AppUpdates.ToList(),
            UserSessions = db.UserSessions.ToList(),
            ActivityLogs = db.ActivityLogs.ToList()
        };
        
        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true
        });
        
        var outputPath = "data_export.json";
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Данные экспортированы в {outputPath}");
        Console.WriteLine($"  Users: {exportData.Users.Count}");
        Console.WriteLine($"  Clients: {exportData.Clients.Count}");
        Console.WriteLine($"  Services: {exportData.Services.Count}");
        Console.WriteLine($"  Orders: {exportData.Orders.Count}");
        Console.WriteLine($"  CallbackRequests: {exportData.CallbackRequests.Count}");
        Console.WriteLine($"  Subscriptions: {exportData.Subscriptions.Count}");
        Console.WriteLine($"  ActivityLogs: {exportData.ActivityLogs.Count}");
        return;
    }
    
    // Команда импорта данных из JSON
    if (args.Contains("--import-data"))
    {
        var inputPath = "data_export.json";
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Файл {inputPath} не найден!");
            return;
        }
        
        Console.WriteLine($"Импорт данных из {inputPath}...");
        
        // Сначала создаём все таблицы если их нет
        Console.WriteLine("Создание недостающих таблиц...");
        db.Database.EnsureCreated();
        
        var json = File.ReadAllText(inputPath);
        var importData = System.Text.Json.JsonSerializer.Deserialize<ImportData>(json);
        
        if (importData == null)
        {
            Console.WriteLine("Ошибка чтения данных!");
            return;
        }
        
        // Очищаем таблицы в правильном порядке (из-за FK)
        Console.WriteLine("Очистка существующих данных...");
        try { db.ActivityLogs.RemoveRange(db.ActivityLogs); } catch { }
        try { db.UserSessions.RemoveRange(db.UserSessions); } catch { }
        db.OrderServices.RemoveRange(db.OrderServices);
        db.Orders.RemoveRange(db.Orders);
        db.CallbackRequests.RemoveRange(db.CallbackRequests);
        db.Subscriptions.RemoveRange(db.Subscriptions);
        db.Clients.RemoveRange(db.Clients);
        db.Services.RemoveRange(db.Services);
        db.Users.RemoveRange(db.Users);
        db.SiteContents.RemoveRange(db.SiteContents);
        db.SiteBlocks.RemoveRange(db.SiteBlocks);
        db.Testimonials.RemoveRange(db.Testimonials);
        db.FaqItems.RemoveRange(db.FaqItems);
        db.ShowcaseItems.RemoveRange(db.ShowcaseItems);
        try { db.AppUpdates.RemoveRange(db.AppUpdates); } catch { }
        db.SaveChanges();
        
        // Импортируем данные (отключаем tracking для избежания конфликтов)
        Console.WriteLine("Импорт данных...");
        
        // Отключаем Identity Insert для PostgreSQL и вставляем с явными ID
        if (connectionString.Contains("Host="))
        {
            // Для PostgreSQL используем OVERRIDING SYSTEM VALUE
            if (importData.Users?.Any() == true)
            {
                foreach (var user in importData.Users)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Users\" (\"Id\", \"Login\", \"PasswordHash\", \"Name\", \"Role\", \"IsActive\", \"MustChangePassword\", \"CreatedAt\", \"CanAccessWeb\", \"CanAccessDesktop\", \"WebRole\", \"DesktopRole\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
                        user.Id, user.Login, user.PasswordHash, user.Name, (int)user.Role, user.IsActive, user.MustChangePassword, user.CreatedAt, user.CanAccessWeb, user.CanAccessDesktop, user.WebRole.HasValue ? (int?)user.WebRole : null, user.DesktopRole.HasValue ? (int?)user.DesktopRole : null);
                }
                Console.WriteLine($"  Users: {importData.Users.Count}");
            }
            
            if (importData.Clients?.Any() == true)
            {
                foreach (var client in importData.Clients)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Clients\" (\"Id\", \"Name\", \"Phone\", \"Email\", \"CarModel\", \"LicensePlate\", \"Notes\", \"CreatedAt\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
                        client.Id, client.Name, client.Phone, client.Email, client.CarModel, client.LicensePlate, client.Notes, client.CreatedAt);
                }
                Console.WriteLine($"  Clients: {importData.Clients.Count}");
            }
            
            if (importData.Services?.Any() == true)
            {
                foreach (var service in importData.Services)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Services\" (\"Id\", \"Title\", \"Category\", \"Description\", \"Price\", \"Image\", \"Icon\", \"IsActive\", \"SortOrder\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})",
                        service.Id, service.Title, service.Category, service.Description, service.Price, service.Image, service.Icon, service.IsActive, service.SortOrder);
                }
                Console.WriteLine($"  Services: {importData.Services.Count}");
            }
            
            if (importData.Orders?.Any() == true)
            {
                foreach (var order in importData.Orders)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Orders\" (\"Id\", \"ClientId\", \"Status\", \"TotalPrice\", \"Notes\", \"CreatedAt\", \"CompletedAt\", \"IsDeleted\", \"DeletedAt\", \"DeletedByUserId\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
                        order.Id, order.ClientId, (int)order.Status, order.TotalPrice, order.Notes, order.CreatedAt, order.CompletedAt, order.IsDeleted, order.DeletedAt, order.DeletedByUserId);
                }
                Console.WriteLine($"  Orders: {importData.Orders.Count}");
            }
            
            if (importData.OrderServices?.Any() == true)
            {
                foreach (var os in importData.OrderServices)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"OrderServices\" (\"OrderId\", \"ServiceId\", \"Price\") VALUES ({0}, {1}, {2})",
                        os.OrderId, os.ServiceId, os.Price);
                }
                Console.WriteLine($"  OrderServices: {importData.OrderServices.Count}");
            }
            
            if (importData.CallbackRequests?.Any() == true)
            {
                foreach (var cb in importData.CallbackRequests)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"CallbackRequests\" (\"Id\", \"Name\", \"Phone\", \"CarModel\", \"LicensePlate\", \"Message\", \"Status\", \"Source\", \"SourceDetails\", \"AssignedUserId\", \"CreatedAt\", \"ProcessedAt\", \"CompletedAt\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12})",
                        cb.Id, cb.Name, cb.Phone, cb.CarModel, cb.LicensePlate, cb.Message, (int)cb.Status, (int)cb.Source, cb.SourceDetails, cb.AssignedUserId, cb.CreatedAt, cb.ProcessedAt, cb.CompletedAt);
                }
                Console.WriteLine($"  CallbackRequests: {importData.CallbackRequests.Count}");
            }
            
            if (importData.Subscriptions?.Any() == true)
            {
                foreach (var sub in importData.Subscriptions)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Subscriptions\" (\"Id\", \"Email\", \"CreatedAt\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2})",
                        sub.Id, sub.Email, sub.CreatedAt);
                }
                Console.WriteLine($"  Subscriptions: {importData.Subscriptions.Count}");
            }
            
            if (importData.SiteBlocks?.Any() == true)
            {
                foreach (var block in importData.SiteBlocks)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"SiteBlocks\" (\"Id\", \"BlockId\", \"Name\", \"Enabled\", \"SortOrder\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4})",
                        block.Id, block.BlockId, block.Name, block.Enabled, block.SortOrder);
                }
                Console.WriteLine($"  SiteBlocks: {importData.SiteBlocks.Count}");
            }
            
            if (importData.Testimonials?.Any() == true)
            {
                foreach (var t in importData.Testimonials)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"Testimonials\" (\"Id\", \"Name\", \"Car\", \"Text\", \"IsActive\", \"SortOrder\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                        t.Id, t.Name, t.Car, t.Text, t.IsActive, t.SortOrder);
                }
                Console.WriteLine($"  Testimonials: {importData.Testimonials.Count}");
            }
            
            if (importData.FaqItems?.Any() == true)
            {
                foreach (var f in importData.FaqItems)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"FaqItems\" (\"Id\", \"Question\", \"Answer\", \"IsActive\", \"SortOrder\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4})",
                        f.Id, f.Question, f.Answer, f.IsActive, f.SortOrder);
                }
                Console.WriteLine($"  FaqItems: {importData.FaqItems.Count}");
            }
            
            if (importData.ActivityLogs?.Any() == true)
            {
                foreach (var log in importData.ActivityLogs)
                {
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO \"ActivityLogs\" (\"Id\", \"UserId\", \"UserName\", \"Action\", \"EntityType\", \"EntityId\", \"Details\", \"IpAddress\", \"Source\", \"CreatedAt\") OVERRIDING SYSTEM VALUE VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
                        log.Id, log.UserId, log.UserName, log.Action, log.EntityType, log.EntityId, log.Details, log.IpAddress, log.Source, log.CreatedAt);
                }
                Console.WriteLine($"  ActivityLogs: {importData.ActivityLogs.Count}");
            }
        }
        else
        {
            // Для SQLite используем обычный AddRange
            if (importData.Users?.Any() == true) db.Users.AddRange(importData.Users);
            if (importData.Clients?.Any() == true) db.Clients.AddRange(importData.Clients);
            if (importData.Services?.Any() == true) db.Services.AddRange(importData.Services);
            if (importData.Orders?.Any() == true) db.Orders.AddRange(importData.Orders);
            if (importData.OrderServices?.Any() == true) db.OrderServices.AddRange(importData.OrderServices);
            if (importData.CallbackRequests?.Any() == true) db.CallbackRequests.AddRange(importData.CallbackRequests);
            if (importData.Subscriptions?.Any() == true) db.Subscriptions.AddRange(importData.Subscriptions);
            if (importData.SiteContents?.Any() == true) db.SiteContents.AddRange(importData.SiteContents);
            if (importData.SiteBlocks?.Any() == true) db.SiteBlocks.AddRange(importData.SiteBlocks);
            if (importData.Testimonials?.Any() == true) db.Testimonials.AddRange(importData.Testimonials);
            if (importData.FaqItems?.Any() == true) db.FaqItems.AddRange(importData.FaqItems);
            if (importData.ShowcaseItems?.Any() == true) db.ShowcaseItems.AddRange(importData.ShowcaseItems);
            if (importData.AppUpdates?.Any() == true) db.AppUpdates.AddRange(importData.AppUpdates);
            if (importData.UserSessions?.Any() == true) db.UserSessions.AddRange(importData.UserSessions);
            if (importData.ActivityLogs?.Any() == true) db.ActivityLogs.AddRange(importData.ActivityLogs);
            db.SaveChanges();
        }
        
        // Сбрасываем sequences для PostgreSQL
        if (connectionString.Contains("Host="))
        {
            Console.WriteLine("Сброс sequences для PostgreSQL...");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Users_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Users\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Clients_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Clients\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Services_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Services\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Orders_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Orders\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"CallbackRequests_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"CallbackRequests\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Subscriptions_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Subscriptions\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"SiteContents_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"SiteContents\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"SiteBlocks_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"SiteBlocks\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"Testimonials_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"Testimonials\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"FaqItems_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"FaqItems\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"ShowcaseItems_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"ShowcaseItems\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"AppUpdates_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"AppUpdates\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"UserSessions_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"UserSessions\") + 1, false)");
            db.Database.ExecuteSqlRaw("SELECT setval('\"ActivityLogs_Id_seq\"', (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"ActivityLogs\") + 1, false)");
        }
        
        Console.WriteLine("Импорт завершён!");
        Console.WriteLine($"  Users: {importData.Users?.Count ?? 0}");
        Console.WriteLine($"  Clients: {importData.Clients?.Count ?? 0}");
        Console.WriteLine($"  Services: {importData.Services?.Count ?? 0}");
        Console.WriteLine($"  Orders: {importData.Orders?.Count ?? 0}");
        Console.WriteLine($"  CallbackRequests: {importData.CallbackRequests?.Count ?? 0}");
        Console.WriteLine($"  Subscriptions: {importData.Subscriptions?.Count ?? 0}");
        return;
    }
    
    // В Development создаём тестовые данные и простые пароли
    var isDevelopment = app.Environment.IsDevelopment();
    DbSeeder.Seed(db, isDevelopment);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HQ Studio API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }

// Класс для импорта данных
public class ImportData
{
    public List<HQStudio.API.Models.User>? Users { get; set; }
    public List<HQStudio.API.Models.Client>? Clients { get; set; }
    public List<HQStudio.API.Models.Service>? Services { get; set; }
    public List<HQStudio.API.Models.Order>? Orders { get; set; }
    public List<HQStudio.API.Models.OrderService>? OrderServices { get; set; }
    public List<HQStudio.API.Models.CallbackRequest>? CallbackRequests { get; set; }
    public List<HQStudio.API.Models.Subscription>? Subscriptions { get; set; }
    public List<HQStudio.API.Models.SiteContent>? SiteContents { get; set; }
    public List<HQStudio.API.Models.SiteBlock>? SiteBlocks { get; set; }
    public List<HQStudio.API.Models.Testimonial>? Testimonials { get; set; }
    public List<HQStudio.API.Models.FaqItem>? FaqItems { get; set; }
    public List<HQStudio.API.Models.ShowcaseItem>? ShowcaseItems { get; set; }
    public List<HQStudio.API.Models.AppUpdate>? AppUpdates { get; set; }
    public List<HQStudio.API.Models.UserSession>? UserSessions { get; set; }
    public List<HQStudio.API.Models.ActivityLog>? ActivityLogs { get; set; }
}
