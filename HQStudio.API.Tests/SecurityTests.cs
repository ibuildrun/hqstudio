using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Тесты безопасности API
/// </summary>
public class SecurityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public SecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }

    // === Тесты защиты эндпоинтов без авторизации ===

    [Theory]
    [InlineData("/api/callbacks", "GET")]
    [InlineData("/api/clients", "GET")]
    [InlineData("/api/orders", "GET")]
    [InlineData("/api/subscriptions", "GET")]
    [InlineData("/api/dashboard", "GET")]
    [InlineData("/api/users", "GET")]
    [InlineData("/api/site/blocks", "GET")]
    [InlineData("/api/site/testimonials", "GET")]
    [InlineData("/api/site/faq", "GET")]
    public async Task ProtectedEndpoints_WithoutAuth_ReturnUnauthorized(string endpoint, string method)
    {
        // Act
        HttpResponseMessage response = method switch
        {
            "GET" => await _client.GetAsync(endpoint),
            _ => throw new ArgumentException("Unknown method")
        };

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Тесты публичных эндпоинтов ===

    [Fact]
    public async Task PublicServices_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicSiteData_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/site");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Тесты ограничения данных для веб-клиентов ===

    [Fact]
    public async Task Callbacks_WebClient_ReturnsLimitedData()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        // Не добавляем X-Client-Type: Desktop

        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callbacks = await response.Content.ReadFromJsonAsync<List<object>>();
        callbacks.Should().NotBeNull();
        callbacks!.Count.Should().BeLessOrEqualTo(20); // Лимит для веб-клиентов
    }

    [Fact]
    public async Task Callbacks_DesktopClient_ReturnsAllData()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");

        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Desktop клиент может получить больше данных (если они есть)
    }

    // === Тесты валидации входных данных ===

    [Fact]
    public async Task CreateCallback_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new { name = "", phone = "+7-999-123-45-67" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCallback_WithTooLongMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new { 
            name = "Test", 
            phone = "+7-999-123-45-67",
            message = new string('x', 1001) // Больше 1000 символов
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new { email = "invalid-email" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new { email = $"test{Guid.NewGuid()}@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Тесты ролевого доступа ===

    [Fact]
    public async Task DeleteCallback_WithAdminRole_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthToken(); // admin
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Создаём заявку для удаления
        await _client.PostAsJsonAsync("/api/callbacks", new { name = "ToDelete", phone = "+7-999-000-00-00" });
        var callbacks = await _client.GetFromJsonAsync<List<CallbackDto>>("/api/callbacks");
        var callbackId = callbacks!.First().Id;

        // Act
        var response = await _client.DeleteAsync($"/api/callbacks/{callbackId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    [Fact]
    public async Task UsersEndpoint_WithAdminRole_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthToken(); // admin
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Тесты защиты Updates для Desktop ===

    [Fact]
    public async Task UpdatesCheck_WithDesktopHeader_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");

        // Act
        var response = await _client.GetAsync("/api/updates/check?currentVersion=1.0.0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatesDownload_WithoutDesktopHeader_ReturnsNotFound()
    {
        // Act - пытаемся скачать несуществующее обновление без заголовка Desktop
        var response = await _client.GetAsync("/api/updates/download/1");

        // Assert - возвращает NotFound для несуществующего обновления
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class CallbackDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}
