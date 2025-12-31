using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class SubscriptionsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Subscribe_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new SubscribeRequest($"test{Guid.NewGuid()}@example.com");

        // Act
        var response = await Client.PostAsJsonAsync("/api/subscriptions", request);

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
        await Client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest(email));

        // Act
        var response = await Client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result!.Message.Should().Contain("уже подписаны");
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsSubscriptions()
    {
        // Arrange
        await AuthenticateAsync();
        await Client.PostAsJsonAsync("/api/subscriptions", new SubscribeRequest($"auth{Guid.NewGuid()}@example.com"));

        // Act
        var response = await Client.GetAsync("/api/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptions = await response.Content.ReadFromJsonAsync<List<Subscription>>();
        subscriptions.Should().NotBeNull();
    }

    private record MessageResponse(string Message);
}
