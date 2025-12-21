using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HQStudio.Models;

namespace HQStudio.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private readonly HttpClient _http;
        private string? _token;
        private readonly string _baseUrl;

        public bool IsConnected { get; private set; }
        public ApiUser? CurrentUser { get; private set; }
        public string BaseUrl => _baseUrl;

        private ApiService()
        {
            _baseUrl = SettingsService.Instance.ApiUrl;
            _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Идентификатор Desktop клиента для снятия ограничений на бекенде
            _http.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        }

        public void SetToken(string token)
        {
            _token = token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearToken()
        {
            _token = null;
            _http.DefaultRequestHeaders.Authorization = null;
            CurrentUser = null;
        }

        // Auth
        public async Task<LoginResult?> LoginAsync(string login, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/auth/login", new { login, password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                    if (result != null)
                    {
                        SetToken(result.Token);
                        CurrentUser = result.User;
                        IsConnected = true;
                    }
                    return result;
                }
            }
            catch { IsConnected = false; }
            return null;
        }

        // Clients
        public async Task<List<ApiClient>> GetClientsAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ApiClient>>("/api/clients");
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<ApiClient?> CreateClientAsync(ApiClient client)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/clients", client);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiClient>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateClientAsync(int id, ApiClient client)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/clients/{id}", client);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/clients/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Services
        public async Task<List<ApiServiceItem>> GetServicesAsync(bool activeOnly = false)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ApiServiceItem>>($"/api/services?activeOnly={activeOnly}");
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<ApiServiceItem?> CreateServiceAsync(ApiServiceItem service)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/services", service);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiServiceItem>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateServiceAsync(int id, ApiServiceItem service)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/services/{id}", service);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/services/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Orders
        public async Task<List<ApiOrder>> GetOrdersAsync(string? status = null)
        {
            try
            {
                var url = "/api/orders" + (status != null ? $"?status={status}" : "");
                var result = await _http.GetFromJsonAsync<List<ApiOrder>>(url);
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<ApiOrder?> CreateOrderAsync(CreateOrderRequest order)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/orders", order);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiOrder>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/orders/{id}/status", status);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/orders/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Callbacks
        public async Task<List<ApiCallback>> GetCallbacksAsync(string? status = null, string? source = null)
        {
            try
            {
                var query = new List<string>();
                if (status != null) query.Add($"status={status}");
                if (source != null) query.Add($"source={source}");
                var url = "/api/callbacks" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
                var result = await _http.GetFromJsonAsync<List<ApiCallback>>(url);
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<ApiCallback?> GetCallbackAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<ApiCallback>($"/api/callbacks/{id}");
            }
            catch { return null; }
        }

        public async Task<ApiCallback?> CreateCallbackAsync(CreateCallbackRequest callback)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/callbacks/manual", callback);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }

        public async Task<ApiCallback?> UpdateCallbackAsync(int id, UpdateCallbackRequest callback)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/callbacks/{id}", callback);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateCallbackStatusAsync(int id, string status)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/callbacks/{id}/status", status);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<CallbackStats?> GetCallbackStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<CallbackStats>("/api/callbacks/stats");
            }
            catch { return null; }
        }

        public async Task<bool> DeleteCallbackAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/callbacks/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Dashboard
        public async Task<DashboardStats?> GetDashboardStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<DashboardStats>("/api/dashboard");
            }
            catch { return null; }
        }

        // Check connection
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/services?activeOnly=true");
                IsConnected = response.IsSuccessStatusCode;
                return IsConnected;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        // Sessions
        public async Task<SessionResult?> StartSessionAsync(string deviceId, string deviceName)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/sessions/start", new { deviceId, deviceName });
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<SessionResult>();
            }
            catch { }
            return null;
        }

        public async Task<HeartbeatResult?> SendHeartbeatAsync(int sessionId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/sessions/heartbeat", new { sessionId });
                if (response.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    return await response.Content.ReadFromJsonAsync<HeartbeatResult>();
                }
            }
            catch { IsConnected = false; }
            return null;
        }

        public async Task EndSessionAsync(int sessionId)
        {
            try
            {
                await _http.PostAsJsonAsync("/api/sessions/end", new { sessionId });
            }
            catch { }
        }

        public async Task<List<ActiveUser>> GetActiveUsersAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ActiveUser>>("/api/sessions/active");
                return result ?? new();
            }
            catch { return new(); }
        }

        // Activity Log
        public async Task<ActivityLogResponse?> GetActivityLogsAsync(int page = 1, int pageSize = 50, string? source = null, int? userId = null)
        {
            try
            {
                var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
                if (!string.IsNullOrEmpty(source)) query.Add($"source={source}");
                if (userId.HasValue) query.Add($"userId={userId}");
                var url = "/api/activitylog?" + string.Join("&", query);
                return await _http.GetFromJsonAsync<ActivityLogResponse>(url);
            }
            catch { return null; }
        }

        public async Task<ActivityLogStats?> GetActivityLogStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<ActivityLogStats>("/api/activitylog/stats");
            }
            catch { return null; }
        }

        public async Task<bool> LogActivityAsync(string action, string? entityType = null, int? entityId = null, string? details = null)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/activitylog", new
                {
                    action,
                    entityType,
                    entityId,
                    details,
                    source = "Desktop"
                });
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Callback with CallbackData (for offline sync)
        public async Task<ApiCallback?> CreateCallbackAsync(CallbackData callback)
        {
            try
            {
                var request = new CreateCallbackRequest
                {
                    Name = callback.Name,
                    Phone = callback.Phone,
                    CarModel = callback.CarModel,
                    LicensePlate = callback.LicensePlate,
                    Message = callback.Message,
                    Source = Enum.TryParse<RequestSource>(callback.Source, out var src) ? src : RequestSource.WalkIn,
                    SourceDetails = callback.SourceDetails
                };
                var response = await _http.PostAsJsonAsync("/api/callbacks/manual", request);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }
    }

    // Session DTOs
    public class SessionResult
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string DeviceId { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class HeartbeatResult
    {
        public bool Success { get; set; }
        public DateTime ServerTime { get; set; }
        public int PendingSync { get; set; }
    }

    public class ActiveUser
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime LastSeen { get; set; }
        public string DeviceName { get; set; } = "";
    }

    public class CallbackData
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string Source { get; set; } = "WalkIn";
        public string? SourceDetails { get; set; }
    }

    // API DTOs
    public class LoginResult
    {
        public string Token { get; set; } = "";
        public ApiUser User { get; set; } = new();
    }

    public class ApiUser
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class ApiClient
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiServiceItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Price { get; set; } = "";
        public string? Image { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class ApiOrder
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public ApiClient? Client { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CreateOrderRequest
    {
        public int ClientId { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }

    public enum RequestSource
    {
        Website,
        Phone,
        WalkIn,
        Email,
        Messenger,
        Referral,
        Other
    }

    public class ApiCallback
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "";
        public RequestSource Source { get; set; }
        public string? SourceDetails { get; set; }
        public int? AssignedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CreateCallbackRequest
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public RequestSource? Source { get; set; }
        public string? SourceDetails { get; set; }
    }

    public class UpdateCallbackRequest
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
        public RequestSource? Source { get; set; }
        public string? SourceDetails { get; set; }
    }

    public class CallbackStats
    {
        public int TotalNew { get; set; }
        public int TotalProcessing { get; set; }
        public int TotalCompleted { get; set; }
        public int TodayCount { get; set; }
        public int WeekCount { get; set; }
        public int MonthCount { get; set; }
        public List<SourceStat> BySource { get; set; } = new();
    }

    public class SourceStat
    {
        public RequestSource Source { get; set; }
        public int Count { get; set; }
    }

    public class DashboardStats
    {
        public int TotalClients { get; set; }
        public int TotalOrders { get; set; }
        public int NewCallbacks { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int OrdersInProgress { get; set; }
        public int CompletedThisMonth { get; set; }
        public int NewSubscribers { get; set; }
        public List<ServiceStat> PopularServices { get; set; } = new();
        public List<RecentOrderStat> RecentOrders { get; set; } = new();
    }

    public class ServiceStat
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class RecentOrderStat
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Activity Log DTOs
    public class ActivityLogResponse
    {
        public List<ActivityLogEntry> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ActivityLogEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string Source { get; set; } = "";
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        public string SourceIcon => Source switch
        {
            "Desktop" => "🖥️",
            "Web" => "🌐",
            "API" => "⚙️",
            _ => "❓"
        };
    }

    public class ActivityLogStats
    {
        public int TotalToday { get; set; }
        public int TotalWeek { get; set; }
        public int TotalAll { get; set; }
        public List<ActivitySourceStat> BySource { get; set; } = new();
        public List<ActivityUserStat> ByUser { get; set; } = new();
    }

    public class ActivitySourceStat
    {
        public string Source { get; set; } = "";
        public int Count { get; set; }
    }

    public class ActivityUserStat
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int Count { get; set; }
    }
}
