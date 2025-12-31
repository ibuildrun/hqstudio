using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

public class ClientsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAll_WithAuth_ReturnsClients()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAuth_CreatesClient()
    {
        // Arrange
        await AuthenticateAsync();
        var newClient = new
        {
            name = "Тестовый Клиент",
            phone = "+7-999-111-22-33",
            email = "test@example.com",
            carModel = "BMW X5",
            licensePlate = "А123БВ777"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/clients", newClient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var client = await response.Content.ReadFromJsonAsync<ClientDto>();
        client.Should().NotBeNull();
        client!.Name.Should().Be("Тестовый Клиент");
    }

    [Fact]
    public async Task Get_WithValidId_ReturnsClient()
    {
        // Arrange
        await AuthenticateAsync();
        
        var createResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Get Test Client",
            phone = "+7-999-222-33-44"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientDto>();

        // Act
        var response = await Client.GetAsync($"/api/clients/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var client = await response.Content.ReadFromJsonAsync<ClientDto>();
        client!.Name.Should().Be("Get Test Client");
    }

    [Fact]
    public async Task Get_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/clients/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithAuth_UpdatesClient()
    {
        // Arrange
        await AuthenticateAsync();
        
        var createResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Update Test",
            phone = "+7-999-333-44-55"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientDto>();

        var updated = new
        {
            id = created!.Id,
            name = "Updated Name",
            phone = "+7-999-333-44-55"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/clients/{created.Id}", updated);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAdminRole_DeletesClient()
    {
        // Arrange
        await AuthenticateAsync();
        
        var createResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Delete Test",
            phone = "+7-999-444-55-66"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClientDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/clients/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? CarModel { get; set; }
    public string? LicensePlate { get; set; }
}
