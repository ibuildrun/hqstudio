using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Тесты безопасности API
/// </summary>
public class SecurityTests : IntegrationTestBase
{
    [Theory]
    [InlineData("/api/callbacks")]
    [InlineData("/api/clients")]
    [InlineData("/api/orders")]
    [InlineData("/api/subscriptions")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/users")]
    [InlineData("/api/site/blocks")]
    [InlineData("/api/site/testimonials")]
    [InlineData("/api/site/faq")]
    public async Task ProtectedEndpoints_WithoutAuth_ReturnUnauthorized(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicServices_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicSiteData_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/site");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutAuth_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCallback_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new { name = "", phone = "+7-999-123-45-67" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new { email = $"test{Guid.NewGuid()}@example.com" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UsersEndpoint_WithAdminRole_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatesCheck_WithDesktopHeader_ReturnsOk()
    {
        // Arrange
        AddDesktopClientHeader();

        // Act
        var response = await Client.GetAsync("/api/updates/check?currentVersion=1.0.0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class CallbackDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}
