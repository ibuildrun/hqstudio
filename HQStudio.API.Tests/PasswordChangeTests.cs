using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using Xunit;

namespace HQStudio.API.Tests;

public class PasswordChangeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public PasswordChangeTests(TestWebApplicationFactory factory)
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

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var request = new ChangePasswordRequest("admin", "newpassword123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore password for other tests
        var newToken = await GetNewAuthToken("admin", "newpassword123");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
        await _client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest("newpassword123", "admin"));
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var request = new ChangePasswordRequest("wrongpassword", "newpassword123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var request = new ChangePasswordRequest("admin", "12345"); // Less than 6 chars

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ChangePasswordRequest("admin", "newpassword123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetNewAuthToken(string login, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(login, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }
}
