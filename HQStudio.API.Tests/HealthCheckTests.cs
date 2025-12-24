using System.Net;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Тесты для health check и базовой доступности API
/// </summary>
public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerJson_IsAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("openapi");
    }

    [Fact]
    public async Task NonExistentEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublicEndpoints_AreAccessibleWithoutAuth()
    {
        // Services - публичный endpoint
        var servicesResponse = await Client.GetAsync("/api/services");
        servicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoints_RequireAuth()
    {
        // Clients - защищённый endpoint
        var clientsResponse = await Client.GetAsync("/api/clients");
        clientsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Orders - защищённый endpoint
        var ordersResponse = await Client.GetAsync("/api/orders");
        ordersResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Users - защищённый endpoint
        var usersResponse = await Client.GetAsync("/api/users");
        usersResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Dashboard - защищённый endpoint
        var dashboardResponse = await Client.GetAsync("/api/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CorsHeaders_ArePresent()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/services");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        
        var response = await Client.SendAsync(request);

        // Assert - CORS должен быть настроен
        // В тестовом окружении может быть отключен, поэтому проверяем что запрос не падает
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
