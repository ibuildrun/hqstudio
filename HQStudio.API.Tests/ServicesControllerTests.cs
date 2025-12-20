using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class ServicesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ServicesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsServices()
    {
        // Act
        var response = await _client.GetAsync("/api/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var services = await response.Content.ReadFromJsonAsync<List<Service>>();
        services.Should().NotBeNull();
        services!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithActiveOnly_ReturnsOnlyActiveServices()
    {
        // Act
        var response = await _client.GetAsync("/api/services?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var services = await response.Content.ReadFromJsonAsync<List<Service>>();
        services.Should().NotBeNull();
        services!.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsService()
    {
        // Act
        var response = await _client.GetAsync("/api/services/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var service = await response.Content.ReadFromJsonAsync<Service>();
        service.Should().NotBeNull();
        service!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/services/9999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var service = new Service { Title = "Test", Category = "Test", Description = "Test", Price = "100" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", service);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAuth_CreatesService()
    {
        // Arrange
        await AuthenticateAsync();
        var service = new { Title = "New Service", Category = "Test", Description = "Test Description", Price = "от 5000 ₽" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", service);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Service>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("New Service");
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }
}
