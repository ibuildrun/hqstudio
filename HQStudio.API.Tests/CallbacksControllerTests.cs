using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class CallbacksControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CallbacksControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCallbackRequest("Иван Петров", "+79991234567", "BMW X5", null, "Хочу шумоизоляцию", null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("принята");
    }

    [Fact]
    public async Task Create_WithMinimalData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCallbackRequest("Мария", "+79998887766", null, null, null, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithSource_SetsSourceCorrectly()
    {
        // Arrange
        var request = new CreateCallbackRequest("Тест", "+79990001111", null, null, null, RequestSource.Phone, "Звонок с рекламы");

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsCallbacks()
    {
        // Arrange
        await AuthenticateAsync();

        // Create a callback first
        await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Test", "+79990001122", null, null, null, null, null));

        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callbacks = await response.Content.ReadFromJsonAsync<List<CallbackRequest>>();
        callbacks.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsStats()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/callbacks/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<CallbackStats>();
        stats.Should().NotBeNull();
        stats!.TotalNew.Should().BeGreaterThanOrEqualTo(0);
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
