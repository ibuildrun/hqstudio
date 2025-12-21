using HQStudio.API.Models;

namespace HQStudio.API.Data;

public static class DbSeeder
{
    /// <summary>
    /// Сидинг базы данных
    /// isDevelopment = true: создаёт тестовых пользователей с простыми паролями и тестовые данные
    /// isDevelopment = false: создаёт только admin без пароля (требует установки при первом входе)
    /// </summary>
    public static void Seed(AppDbContext context, bool isDevelopment = false)
    {
        SeedUsers(context, isDevelopment);
        SeedServices(context);
        SeedSiteContent(context);
        
        if (isDevelopment)
        {
            SeedTestData(context);
        }
        
        context.SaveChanges();
    }

    private static void SeedUsers(AppDbContext context, bool isDevelopment)
    {
        if (context.Users.Any()) return;

        if (isDevelopment)
        {
            // Разработка: простые пароли для тестирования
            context.Users.AddRange(
                new User
                {
                    Login = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    Name = "Администратор",
                    Role = UserRole.Admin
                },
                new User
                {
                    Login = "developer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("developer"),
                    Name = "Разработчик",
                    Role = UserRole.Admin
                },
                new User
                {
                    Login = "manager",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager"),
                    Name = "Менеджер Иванов",
                    Role = UserRole.Manager
                },
                new User
                {
                    Login = "editor",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("editor"),
                    Name = "Редактор Петров",
                    Role = UserRole.Editor
                }
            );
        }
        else
        {
            // Продакшн: admin и developer с временными паролями, которые нужно сменить
            var adminPassword = Guid.NewGuid().ToString("N")[..12];
            var devPassword = Guid.NewGuid().ToString("N")[..12];
            
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ПЕРВЫЙ ЗАПУСК - ВРЕМЕННЫЕ ПАРОЛИ ПОЛЬЗОВАТЕЛЕЙ            ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║  Администратор: admin / {adminPassword}                   ║");
            Console.WriteLine($"║  Разработчик: developer / {devPassword}                   ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║  ОБЯЗАТЕЛЬНО СМЕНИТЕ ПАРОЛИ ПОСЛЕ ПЕРВОГО ВХОДА!           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            context.Users.AddRange(
                new User
                {
                    Login = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Name = "Администратор",
                    Role = UserRole.Admin,
                    MustChangePassword = true
                },
                new User
                {
                    Login = "developer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(devPassword),
                    Name = "Разработчик",
                    Role = UserRole.Editor,
                    MustChangePassword = true
                }
            );
        }
    }

    private static void SeedServices(AppDbContext context)
    {
        if (context.Services.Any()) return;

        context.Services.AddRange(
            new Service { Title = "Доводчики дверей", Category = "Комфорт", Description = "Система автоматических доводчиков позволяет без дополнительных усилий закрывать двери.", Price = "от 15 000 ₽", SortOrder = 1 },
            new Service { Title = "Шумоизоляция", Category = "Тишина", Description = "Профессиональная шумоизоляция автомобиля. Полный комплекс работ.", Price = "от 15 000 ₽", SortOrder = 2 },
            new Service { Title = "Антихром", Category = "Стиль", Description = "Антихром на авто методом качественной обтяжки виниловой пленкой.", Price = "от 4 000 ₽", SortOrder = 3 },
            new Service { Title = "Контурная подсветка", Category = "Атмосфера", Description = "Ambient light — способ выделить свой автомобиль и улучшить атмосферу.", Price = "от 16 000 ₽", SortOrder = 4 },
            new Service { Title = "Комплектующие", Category = "Продажа", Description = "Продажа комплектов контурной подсветки Ambient light.", Price = "от 6 000 ₽", SortOrder = 5 }
        );
    }

    private static void SeedSiteContent(AppDbContext context)
    {
        if (!context.SiteBlocks.Any())
        {
            context.SiteBlocks.AddRange(
                new SiteBlock { BlockId = "hero", Name = "Главный экран", Enabled = true, SortOrder = 1 },
                new SiteBlock { BlockId = "services", Name = "Услуги", Enabled = true, SortOrder = 2 },
                new SiteBlock { BlockId = "testimonials", Name = "Отзывы", Enabled = true, SortOrder = 3 },
                new SiteBlock { BlockId = "faq", Name = "FAQ", Enabled = true, SortOrder = 4 },
                new SiteBlock { BlockId = "contact", Name = "Контакты", Enabled = true, SortOrder = 5 }
            );
        }

        if (!context.Testimonials.Any())
        {
            context.Testimonials.AddRange(
                new Testimonial { Name = "Марина", Car = "Audi Q7", Text = "HQ_Studio превратили мою машину в настоящий оазис тишины.", SortOrder = 1 },
                new Testimonial { Name = "Александр", Car = "Range Rover", Text = "Делал антихром и доводчики. Качество на высоте.", SortOrder = 2 },
                new Testimonial { Name = "Екатерина", Car = "Porsche Macan", Text = "Контурная подсветка просто преобразила интерьер!", SortOrder = 3 }
            );
        }

        if (!context.FaqItems.Any())
        {
            context.FaqItems.AddRange(
                new FaqItem { Question = "Сохранится ли дилерская гарантия?", Answer = "Да. Мы работаем согласно техническим регламентам.", SortOrder = 1 },
                new FaqItem { Question = "Как долго длится процесс шумоизоляции?", Answer = "Полный комплекс занимает от 2 до 3 рабочих дней.", SortOrder = 2 }
            );
        }
    }

    /// <summary>
    /// Тестовые данные для разработки - много записей для проверки пагинации и вёрстки
    /// </summary>
    private static void SeedTestData(AppDbContext context)
    {
        SeedTestClients(context);
        SeedTestCallbacks(context);
        SeedTestOrders(context);
        SeedTestSubscriptions(context);
    }

    private static void SeedTestClients(AppDbContext context)
    {
        if (context.Clients.Any()) return;

        var cars = new[] { "BMW X5", "Mercedes GLE", "Audi Q7", "Porsche Cayenne", "Range Rover", "Lexus RX", "Toyota Land Cruiser", "Volkswagen Touareg", "Volvo XC90", "Infiniti QX80" };
        var names = new[] { "Иванов Иван", "Петров Пётр", "Сидоров Сидор", "Козлов Андрей", "Новиков Дмитрий", "Морозов Алексей", "Волков Сергей", "Соколов Михаил", "Лебедев Николай", "Орлов Владимир" };
        var random = new Random(42);

        for (int i = 0; i < 50; i++)
        {
            context.Clients.Add(new Client
            {
                Name = names[i % names.Length] + (i >= names.Length ? $" {i / names.Length + 1}" : ""),
                Phone = $"+7 (9{random.Next(10, 99)}) {random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(10, 99)}",
                Email = $"client{i + 1}@example.com",
                CarModel = cars[i % cars.Length],
                LicensePlate = $"{(char)('А' + random.Next(26))}{random.Next(100, 999)}{(char)('А' + random.Next(26))}{(char)('А' + random.Next(26))}{random.Next(10, 199)}",
                Notes = i % 3 == 0 ? "VIP клиент" : null,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365))
            });
        }
    }

    private static void SeedTestCallbacks(AppDbContext context)
    {
        if (context.CallbackRequests.Any()) return;

        var sources = Enum.GetValues<RequestSource>();
        var statuses = Enum.GetValues<RequestStatus>();
        var cars = new[] { "BMW X5", "Mercedes GLE", "Audi Q7", "Porsche Cayenne", "Range Rover", "Lexus RX", "Toyota Land Cruiser", "Volkswagen Touareg" };
        var names = new[] { "Александр", "Михаил", "Дмитрий", "Андрей", "Сергей", "Николай", "Владимир", "Алексей", "Евгений", "Максим", "Анна", "Мария", "Елена", "Ольга", "Наталья" };
        var messages = new[] 
        { 
            "Интересует шумоизоляция полного комплекса", 
            "Хочу узнать стоимость доводчиков на 4 двери",
            "Нужна консультация по антихрому",
            "Интересует контурная подсветка салона",
            "Хочу записаться на диагностику",
            "Подскажите сроки выполнения работ",
            null,
            "Перезвоните, пожалуйста",
            "Видел вашу работу у друга, хочу так же",
            null
        };
        var random = new Random(42);

        // Создаём 100 заявок для проверки пагинации
        for (int i = 0; i < 100; i++)
        {
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 90)).AddHours(-random.Next(0, 24));
            var status = statuses[random.Next(statuses.Length)];
            
            context.CallbackRequests.Add(new CallbackRequest
            {
                Name = names[random.Next(names.Length)],
                Phone = $"+7 (9{random.Next(10, 99)}) {random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(10, 99)}",
                CarModel = random.Next(3) > 0 ? cars[random.Next(cars.Length)] : null,
                LicensePlate = random.Next(4) > 0 ? $"{(char)('А' + random.Next(26))}{random.Next(100, 999)}{(char)('А' + random.Next(26))}{(char)('А' + random.Next(26))}{random.Next(10, 199)}" : null,
                Message = messages[random.Next(messages.Length)],
                Source = sources[random.Next(sources.Length)],
                SourceDetails = random.Next(5) == 0 ? "Instagram" : null,
                Status = status,
                CreatedAt = createdAt,
                ProcessedAt = status != RequestStatus.New ? createdAt.AddHours(random.Next(1, 48)) : null,
                CompletedAt = status == RequestStatus.Completed ? createdAt.AddDays(random.Next(1, 14)) : null
            });
        }
    }

    private static void SeedTestOrders(AppDbContext context)
    {
        if (context.Orders.Any()) return;
        
        context.SaveChanges(); // Сохраняем клиентов и услуги
        
        var clients = context.Clients.ToList();
        var services = context.Services.ToList();
        if (!clients.Any() || !services.Any()) return;

        var statuses = Enum.GetValues<OrderStatus>();
        var random = new Random(42);

        // Создаём 75 заказов
        for (int i = 0; i < 75; i++)
        {
            var client = clients[random.Next(clients.Count)];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 180));
            var status = statuses[random.Next(statuses.Length)];
            var orderServices = services.OrderBy(_ => random.Next()).Take(random.Next(1, 4)).ToList();
            var totalPrice = orderServices.Count * random.Next(10000, 50000);

            var order = new Order
            {
                ClientId = client.Id,
                Status = status,
                TotalPrice = totalPrice,
                Notes = random.Next(3) == 0 ? "Срочный заказ" : null,
                CreatedAt = createdAt,
                CompletedAt = status == OrderStatus.Completed ? createdAt.AddDays(random.Next(1, 14)) : null
            };
            context.Orders.Add(order);
            context.SaveChanges();

            foreach (var service in orderServices)
            {
                context.OrderServices.Add(new OrderService
                {
                    OrderId = order.Id,
                    ServiceId = service.Id,
                    Price = random.Next(10000, 50000)
                });
            }
        }
    }

    private static void SeedTestSubscriptions(AppDbContext context)
    {
        if (context.Subscriptions.Any()) return;

        var random = new Random(42);
        var domains = new[] { "gmail.com", "yandex.ru", "mail.ru", "outlook.com", "icloud.com" };

        // Создаём 40 подписок
        for (int i = 0; i < 40; i++)
        {
            context.Subscriptions.Add(new Subscription
            {
                Email = $"subscriber{i + 1}@{domains[random.Next(domains.Length)]}",
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 365))
            });
        }
    }
}
