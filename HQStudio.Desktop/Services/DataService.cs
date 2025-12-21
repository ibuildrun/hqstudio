using HQStudio.Models;
using System.IO;
using System.Text.Json;

namespace HQStudio.Services
{
    public class DataService
    {
        private static DataService? _instance;
        public static DataService Instance => _instance ??= new DataService();

        private readonly string _dataPath;
        
        public List<User> Users { get; private set; } = new();
        public List<Client> Clients { get; private set; } = new();
        public List<Service> Services { get; private set; } = new();
        public List<Order> Orders { get; private set; } = new();
        public User? CurrentUser { get; set; }

        private DataService()
        {
            _dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HQStudio");
            Directory.CreateDirectory(_dataPath);
            LoadData();
            InitializeDefaultData();
        }

        private void InitializeDefaultData()
        {
            // Проверяем и добавляем отсутствующих пользователей
            var defaultUsers = new List<User>
            {
                new() { Id = 1, Username = "admin", PasswordHash = "admin", DisplayName = "Павел Игонин", Role = "Admin" },
                new() { Id = 2, Username = "developer", PasswordHash = "developer", DisplayName = "Разработчик", Role = "Admin" },
                new() { Id = 3, Username = "worker", PasswordHash = "worker", DisplayName = "Алексей Смирнов", Role = "Worker" },
                new() { Id = 4, Username = "ivan", PasswordHash = "ivan", DisplayName = "Иван Петров", Role = "Worker" }
            };

            if (!Users.Any())
            {
                Users = defaultUsers;
            }
            else
            {
                // Добавляем отсутствующих пользователей (например, developer)
                foreach (var defaultUser in defaultUsers)
                {
                    if (!Users.Any(u => u.Username == defaultUser.Username))
                    {
                        defaultUser.Id = GetNextId(Users);
                        Users.Add(defaultUser);
                    }
                }
            }

            if (!Services.Any())
            {
                Services = new List<Service>
                {
                    new() { Id = 1, Name = "Доводчики дверей", Category = "Доводчики", PriceFrom = 15000, Icon = "🚪",
                        Description = "Система автоматических доводчиков позволяет без дополнительных усилий закрывать двери – при неполном закрытии их автоматически дотянет механизм." },
                    new() { Id = 2, Name = "Шумоизоляция автомобиля", Category = "Шумоизоляция", PriceFrom = 15000, Icon = "🔇",
                        Description = "Шумоизоляция колёсных арок снаружи, дверей, крыши, пола, багажного отделения." },
                    new() { Id = 3, Name = "Антихром", Category = "Антихром", PriceFrom = 4000, Icon = "🖤",
                        Description = "Антихром на авто методом качественной обтяжки виниловой пленкой, а также окрас с предварительным травлением хрома." },
                    new() { Id = 4, Name = "Контурная подсветка", Category = "Подсветка", PriceFrom = 16000, Icon = "💡",
                        Description = "Контурная подсветка салона — способ выделить свой автомобиль, подчеркнуть статус и улучшить внутреннюю атмосферу." },
                    new() { Id = 5, Name = "Черная контурная подсветка", Category = "Подсветка", PriceFrom = 6000, Icon = "⚫",
                        Description = "Контурная подсветка Ambient light, в наличии черная и белая!" },
                    new() { Id = 6, Name = "Перетяжка потолка", Category = "Салон", PriceFrom = 12000, Icon = "🎨",
                        Description = "Профессиональная перетяжка потолка автомобиля качественными материалами." },
                    new() { Id = 7, Name = "Восстановление гравировок", Category = "Восстановление", PriceFrom = 5000, Icon = "✨",
                        Description = "Восстановление заводских гравировок и нанесение новых." }
                };
            }

            // Добавляем моковых клиентов если их нет
            if (!Clients.Any())
            {
                Clients = new List<Client>
                {
                    new() { Id = 1, Name = "Дмитрий Волков", Phone = "+7-912-345-67-89", Car = "BMW X5 G05", CarNumber = "А777АА86", CreatedAt = DateTime.Now.AddDays(-45), Notes = "Постоянный клиент" },
                    new() { Id = 2, Name = "Андрей Козлов", Phone = "+7-922-111-22-33", Car = "Mercedes-Benz GLE", CarNumber = "В001ВВ86", CreatedAt = DateTime.Now.AddDays(-30), Notes = "VIP клиент" },
                    new() { Id = 3, Name = "Сергей Новиков", Phone = "+7-950-444-55-66", Car = "Audi Q7", CarNumber = "Е555ЕЕ86", CreatedAt = DateTime.Now.AddDays(-20) },
                    new() { Id = 4, Name = "Максим Федоров", Phone = "+7-912-777-88-99", Car = "Toyota Land Cruiser 300", CarNumber = "К100КК86", CreatedAt = DateTime.Now.AddDays(-15) },
                    new() { Id = 5, Name = "Артем Соколов", Phone = "+7-929-333-44-55", Car = "Lexus LX 570", CarNumber = "М200ММ86", CreatedAt = DateTime.Now.AddDays(-10) },
                    new() { Id = 6, Name = "Николай Морозов", Phone = "+7-908-666-77-88", Car = "Porsche Cayenne", CarNumber = "Н300НН86", CreatedAt = DateTime.Now.AddDays(-5) },
                    new() { Id = 7, Name = "Владимир Попов", Phone = "+7-912-999-00-11", Car = "Range Rover Sport", CarNumber = "О400ОО86", CreatedAt = DateTime.Now.AddDays(-3) },
                    new() { Id = 8, Name = "Игорь Лебедев", Phone = "+7-950-222-33-44", Car = "Volkswagen Touareg", CarNumber = "Р500РР86", CreatedAt = DateTime.Now.AddDays(-1) }
                };
            }

            // Добавляем моковые заказы если их нет
            if (!Orders.Any())
            {
                Orders = new List<Order>
                {
                    new() { 
                        Id = 1, 
                        ClientId = 1, 
                        Client = Clients[0],
                        ServiceIds = new List<int> { 1, 2 },
                        TotalPrice = 45000, 
                        Status = "Завершен", 
                        CreatedAt = DateTime.Now.AddDays(-40),
                        CompletedAt = DateTime.Now.AddDays(-38),
                        Notes = "Полная шумоизоляция + доводчики"
                    },
                    new() { 
                        Id = 2, 
                        ClientId = 2, 
                        Client = Clients[1],
                        ServiceIds = new List<int> { 4 },
                        TotalPrice = 32000, 
                        Status = "Завершен", 
                        CreatedAt = DateTime.Now.AddDays(-25),
                        CompletedAt = DateTime.Now.AddDays(-23),
                        Notes = "Контурная подсветка салона"
                    },
                    new() { 
                        Id = 3, 
                        ClientId = 3, 
                        Client = Clients[2],
                        ServiceIds = new List<int> { 2 },
                        TotalPrice = 15000, 
                        Status = "Завершен", 
                        CreatedAt = DateTime.Now.AddDays(-18),
                        CompletedAt = DateTime.Now.AddDays(-17),
                        Notes = "Шумоизоляция дверей"
                    },
                    new() { 
                        Id = 4, 
                        ClientId = 4, 
                        Client = Clients[3],
                        ServiceIds = new List<int> { 3, 6 },
                        TotalPrice = 28000, 
                        Status = "Завершен", 
                        CreatedAt = DateTime.Now.AddDays(-12),
                        CompletedAt = DateTime.Now.AddDays(-10),
                        Notes = "Антихром + перетяжка потолка"
                    },
                    new() { 
                        Id = 5, 
                        ClientId = 5, 
                        Client = Clients[4],
                        ServiceIds = new List<int> { 2, 1 },
                        TotalPrice = 55000, 
                        Status = "В работе", 
                        CreatedAt = DateTime.Now.AddDays(-3),
                        Notes = "Комплексная шумоизоляция всего авто"
                    },
                    new() { 
                        Id = 6, 
                        ClientId = 6, 
                        Client = Clients[5],
                        ServiceIds = new List<int> { 1, 5 },
                        TotalPrice = 22000, 
                        Status = "В работе", 
                        CreatedAt = DateTime.Now.AddDays(-2),
                        Notes = "Доводчики дверей + подсветка"
                    },
                    new() { 
                        Id = 7, 
                        ClientId = 7, 
                        Client = Clients[6],
                        ServiceIds = new List<int> { 4 },
                        TotalPrice = 16000, 
                        Status = "Новый", 
                        CreatedAt = DateTime.Now.AddDays(-1),
                        Notes = "Контурная подсветка"
                    },
                    new() { 
                        Id = 8, 
                        ClientId = 8, 
                        Client = Clients[7],
                        ServiceIds = new List<int> { 3 },
                        TotalPrice = 8000, 
                        Status = "Новый", 
                        CreatedAt = DateTime.Now,
                        Notes = "Антихром решетки радиатора"
                    }
                };
            }

            SaveData();
        }

        public void LoadData()
        {
            Users = LoadFromFile<List<User>>("users.json") ?? new();
            Clients = LoadFromFile<List<Client>>("clients.json") ?? new();
            Services = LoadFromFile<List<Service>>("services.json") ?? new();
            Orders = LoadFromFile<List<Order>>("orders.json") ?? new();
            
            // Link services to orders
            foreach (var order in Orders)
            {
                order.Client = Clients.FirstOrDefault(c => c.Id == order.ClientId);
                order.Services = Services.Where(s => order.ServiceIds.Contains(s.Id)).ToList();
            }
        }

        public void SaveData()
        {
            SaveToFile("users.json", Users);
            SaveToFile("clients.json", Clients);
            SaveToFile("services.json", Services);
            SaveToFile("orders.json", Orders);
        }

        public void ResetToDemo()
        {
            // Удаляем все файлы данных для сброса к демо-данным
            var files = new[] { "users.json", "clients.json", "services.json", "orders.json" };
            foreach (var file in files)
            {
                var path = Path.Combine(_dataPath, file);
                if (File.Exists(path)) File.Delete(path);
            }
            
            Users.Clear();
            Clients.Clear();
            Services.Clear();
            Orders.Clear();
            
            InitializeDefaultData();
        }

        private T? LoadFromFile<T>(string filename)
        {
            var path = Path.Combine(_dataPath, filename);
            if (!File.Exists(path)) return default;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        private void SaveToFile<T>(string filename, T data)
        {
            var path = Path.Combine(_dataPath, filename);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public bool Login(string username, string password)
        {
            var user = Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == password && u.IsActive);
            if (user != null)
            {
                CurrentUser = user;
                return true;
            }
            return false;
        }

        public void Logout() => CurrentUser = null;

        public int GetNextId<T>(List<T> list) where T : class
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null || !list.Any()) return 1;
            return list.Max(x => (int)prop.GetValue(x)!) + 1;
        }
    }
}
