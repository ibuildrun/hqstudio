using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using Xunit;

namespace HQStudio.API.Tests;

public class SessionsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task StartSession_WithAuth_CreatesSession()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new StartSessionRequest("device-123", "Test PC");

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/start", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionResult>();
        session.Should().NotBeNull();
        session!.Id.Should().BeGreaterThan(0);
        session.DeviceId.Should().Be("device-123");
    }

    [Fact]
    public async Task StartSession_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new StartSessionRequest("device-123", "Test PC");

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/start", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Heartbeat_WithValidSession_ReturnsSuccess()
    {
        // Arrange
        await AuthenticateAsync();
        var startResponse = await Client.PostAsJsonAsync("/api/sessions/start", new StartSessionRequest("device-hb", "Heartbeat Test"));
        var session = await startResponse.Content.ReadFromJsonAsync<SessionResult>();

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/heartbeat", new HeartbeatRequest(session!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HeartbeatResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Heartbeat_WithInvalidSession_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/heartbeat", new HeartbeatRequest(99999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndSession_WithValidSession_EndsSession()
    {
        // Arrange
        await AuthenticateAsync();
        var startResponse = await Client.PostAsJsonAsync("/api/sessions/start", new StartSessionRequest("device-end", "End Test"));
        var session = await startResponse.Content.ReadFromJsonAsync<SessionResult>();

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/end", new EndSessionRequest(session!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveSessions_WithAuth_ReturnsList()
    {
        // Arrange
        await AuthenticateAsync();
        await Client.PostAsJsonAsync("/api/sessions/start", new StartSessionRequest("device-active", "Active Test"));

        // Act
        var response = await Client.GetAsync("/api/sessions/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<ActiveUserDto>>();
        users.Should().NotBeNull();
    }
}

public class SessionResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public int Status { get; set; }
}
