using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

public class OrdersControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAll_WithAuth_ReturnsOrders()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAuth_CreatesOrder()
    {
        // Arrange
        await AuthenticateAsync();
        
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Order Test Client",
            phone = "+7-999-555-66-77"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientDto>();

        var newOrder = new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 15000m,
            notes = "Тестовый заказ"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", newOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateStatus_WithAuth_UpdatesOrderStatus()
    {
        // Arrange
        await AuthenticateAsync();
        
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Status Test Client",
            phone = "+7-999-666-77-88"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientDto>();

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 10000m
        });
        var location = orderResponse.Headers.Location?.ToString();
        var orderId = location?.Split('/').Last();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/orders/{orderId}/status", 1);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAdminRole_DeletesOrder()
    {
        // Arrange
        await AuthenticateAsync();
        
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Delete Order Client",
            phone = "+7-999-888-99-00"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientDto>();

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 3000m
        });
        var location = orderResponse.Headers.Location?.ToString();
        var orderId = location?.Split('/').Last();

        // Act
        var response = await Client.DeleteAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

public class ServiceDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
}
