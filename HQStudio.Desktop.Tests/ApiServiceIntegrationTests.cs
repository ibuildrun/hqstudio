using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Integration tests for ApiService against running API.
/// Requires API to be running at http://localhost:5000
/// These tests are skipped in CI (Category=Integration)
/// </summary>
[Trait("Category", "Integration")]
public class ApiServiceIntegrationTests : IAsyncLifetime
{
    private readonly ApiServiceTestClient _api;

    public ApiServiceIntegrationTests()
    {
        _api = new ApiServiceTestClient("http://localhost:5000");
    }

    public async Task InitializeAsync()
    {
        // Login before each test that needs auth
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckConnection_WhenApiRunning_ReturnsTrue()
    {
        // Act
        var result = await _api.CheckConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Act
        var result = await _api.LoginAsync("admin", "admin");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.User.Login.Should().Be("admin");
        result.User.Role.Should().Be(0); // Admin = 0
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsNull()
    {
        // Act
        var result = await _api.LoginAsync("admin", "wrongpassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetServices_ReturnsServicesList()
    {
        // Act
        var services = await _api.GetServicesAsync();

        // Assert
        services.Should().NotBeNull();
        services.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetCallbacks_AfterLogin_ReturnsCallbacksList()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull("API должен быть запущен и пользователь admin должен существовать");

        // Act
        var callbacks = await _api.GetCallbacksAsync();

        // Assert
        callbacks.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCallback_WithValidData_ReturnsCallback()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();
        
        var request = new CreateCallbackRequest
        {
            Name = $"Test {Guid.NewGuid():N}",
            Phone = "+79991234567",
            CarModel = "Test Car",
            Source = RequestSource.WalkIn,
            SourceDetails = "Тест из WPF"
        };

        // Act
        var result = await _api.CreateCallbackAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Source.Should().Be(RequestSource.WalkIn);
    }

    [Fact]
    public async Task GetCallbackStats_AfterLogin_ReturnsStats()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();

        // Act
        var stats = await _api.GetCallbackStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats!.TotalNew.Should().BeGreaterThanOrEqualTo(0);
        stats.BySource.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardStats_AfterLogin_ReturnsStats()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();

        // Act
        var stats = await _api.GetDashboardStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats!.TotalClients.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalOrders.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task UpdateCallbackStatus_ChangesStatus()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();
        
        // Create a callback first
        var callback = await _api.CreateCallbackAsync(new CreateCallbackRequest
        {
            Name = $"Status Test {Guid.NewGuid():N}",
            Phone = "+79990001111",
            Source = RequestSource.Phone
        });
        callback.Should().NotBeNull();

        // Act
        var success = await _api.UpdateCallbackStatusAsync(callback!.Id, RequestStatus.Processing);

        // Assert
        success.Should().BeTrue();

        // Verify
        var updated = await _api.GetCallbackAsync(callback.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(RequestStatus.Processing);
    }

    [Fact]
    public async Task GetClients_AfterLogin_ReturnsClientsList()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();

        // Act
        var clients = await _api.GetClientsAsync();

        // Assert
        clients.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsClient()
    {
        // Arrange
        var loginResult = await _api.LoginAsync("admin", "admin");
        loginResult.Should().NotBeNull();
        
        var client = new ApiClient
        {
            Name = $"Test Client {Guid.NewGuid():N}",
            Phone = "+79998887766",
            CarModel = "BMW X5",
            LicensePlate = "A001AA86"
        };

        // Act
        var result = await _api.CreateClientAsync(client);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(client.Name);
    }
}

/// <summary>
/// Test client that mirrors ApiService but is testable
/// </summary>
public class ApiServiceTestClient
{
    private readonly HttpClient _http;
    private string? _token;

    public ApiServiceTestClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/services?activeOnly=true");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

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
                    _token = result.Token;
                    _http.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                }
                return result;
            }
        }
        catch { }
        return null;
    }

    public async Task<List<ApiServiceItem>> GetServicesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ApiServiceItem>>("/api/services") ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<ApiCallback>> GetCallbacksAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ApiCallback>>("/api/callbacks") ?? new();
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

    public async Task<ApiCallback?> CreateCallbackAsync(CreateCallbackRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/callbacks/manual", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ApiCallback>();
        }
        catch { }
        return null;
    }

    public async Task<bool> UpdateCallbackStatusAsync(int id, RequestStatus status)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/callbacks/{id}/status", (int)status);
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

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<DashboardStats>("/api/dashboard");
        }
        catch { return null; }
    }

    public async Task<List<ApiClient>> GetClientsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ApiClient>>("/api/clients") ?? new();
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
}

// DTOs for tests
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
    public int Role { get; set; } // 0 = Admin, 1 = Editor, 2 = Manager
}

public class ApiServiceItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Price { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ApiCallback
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? CarModel { get; set; }
    public RequestStatus Status { get; set; }
    public RequestSource Source { get; set; }
    public string? SourceDetails { get; set; }
    public DateTime CreatedAt { get; set; }
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

public enum RequestSource
{
    Website, Phone, WalkIn, Email, Messenger, Referral, Other
}

public enum RequestStatus
{
    New, Processing, Completed, Cancelled
}

public class CallbackStats
{
    public int TotalNew { get; set; }
    public int TotalProcessing { get; set; }
    public int TotalCompleted { get; set; }
    public int TodayCount { get; set; }
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
}

public class ApiClient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? CarModel { get; set; }
    public string? LicensePlate { get; set; }
    public DateTime CreatedAt { get; set; }
}
