using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class CallbacksControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCallbackRequest("Иван Петров", "+79991234567", "BMW X5", null, "Хочу шумоизоляцию", null, null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/callbacks", request);

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
        var response = await Client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCallbackRequest("", "+79991234567", null, null, null, null, null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyPhone_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCallbackRequest("Тест", "", null, null, null, null, null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsCallbacks()
    {
        // Arrange
        await AuthenticateAsync();
        await Client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Test", "+79990001122", null, null, null, null, null));

        // Act
        var response = await Client.GetAsync("/api/callbacks");

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
        var response = await Client.GetAsync("/api/callbacks/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<CallbackStats>();
        stats.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_WithAuth_UpdatesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Status Test", "+79990001144", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        
        // Act
        var response = await Client.PutAsJsonAsync($"/api/callbacks/{createResult!.Id}/status", RequestStatus.Processing);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAdminAuth_DeletesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Delete Test", "+79990001166", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        
        // Act
        var response = await Client.DeleteAsync($"/api/callbacks/{createResult!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private record MessageResponse(string Message);
    private record MessageWithIdResponse(string Message, int Id);
}
