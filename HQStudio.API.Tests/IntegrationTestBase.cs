using System.Net.Http.Headers;
using System.Net.Http.Json;
using HQStudio.API.DTOs.Auth;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Base class for integration tests using PostgreSQL Testcontainers.
/// Each test class gets its own PostgreSQL container for isolation.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new TestWebApplicationFactory();
        await Factory.InitializeAsync();
        Factory.SeedDatabase();
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Authenticates the client with admin credentials
    /// </summary>
    protected async Task AuthenticateAsync(string login = "admin", string password = "admin")
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(login, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }

    /// <summary>
    /// Adds Desktop client header for bypassing auth on certain endpoints
    /// </summary>
    protected void AddDesktopClientHeader()
    {
        if (!Client.DefaultRequestHeaders.Contains("X-Client-Type"))
        {
            Client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        }
    }
}
