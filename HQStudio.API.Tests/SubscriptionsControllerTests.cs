using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class SubscriptionsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public SubscriptionsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Subscribe_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new SubscribeRequest($"test{Guid.NewGuid()}@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("подписку");
    }

    [Fact]
    public async Task Subscribe_WithDuplicateEmail_ReturnsAlreadySubscribed()
    {
        // Arrange
        var email = $"duplicate{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest(email));

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result!.Message.Should().Contain("уже подписаны");
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsSubscriptions()
    {
        // Arrange
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest($"auth{Guid.NewGuid()}@example.com"));

        // Act
        var response = await _client.GetAsync("/api/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptions = await response.Content.ReadFromJsonAsync<List<Subscription>>();
        subscriptions.Should().NotBeNull();
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }

    private record MessageResponse(string Message);
}
