using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs.Auth;
using Xunit;

namespace HQStudio.API.Tests;

public class PasswordChangeTests : IntegrationTestBase
{
    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new ChangePasswordRequest("admin", "newpassword123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new ChangePasswordRequest("wrongpassword", "newpassword123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new ChangePasswordRequest("admin", "12345");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ChangePasswordRequest("admin", "newpassword123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
