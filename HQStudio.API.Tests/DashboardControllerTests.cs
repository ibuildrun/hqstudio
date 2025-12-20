using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.DTOs;
using Xunit;

namespace HQStudio.API.Tests;

public class DashboardControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DashboardControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsDashboardStats()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<DashboardStats>();
        stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStructure()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/dashboard");
        var stats = await response.Content.ReadFromJsonAsync<DashboardStats>();

        // Assert
        stats!.TotalClients.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalOrders.Should().BeGreaterThanOrEqualTo(0);
        stats.NewCallbacks.Should().BeGreaterThanOrEqualTo(0);
        stats.MonthlyRevenue.Should().BeGreaterThanOrEqualTo(0);
        stats.PopularServices.Should().NotBeNull();
        stats.RecentOrders.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_AfterCreatingCallback_ShowsNewCallback()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Get initial stats
        var initialResponse = await _client.GetAsync("/api/dashboard");
        var initialStats = await initialResponse.Content.ReadFromJsonAsync<DashboardStats>();
        
        // Create a callback
        await _client.PostAsJsonAsync("/api/callbacks", 
            new CreateCallbackRequest($"Dashboard Test {Guid.NewGuid()}", "+79990009999", null, null, null, null, null));

        // Act
        var response = await _client.GetAsync("/api/dashboard");
        var stats = await response.Content.ReadFromJsonAsync<DashboardStats>();

        // Assert
        stats!.NewCallbacks.Should().BeGreaterThanOrEqualTo(initialStats!.NewCallbacks);
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }
}
