using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using Xunit;

namespace HQStudio.API.Tests;

public class OrdersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public OrdersControllerTests(TestWebApplicationFactory factory)
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
    public async Task GetAll_WithAuth_ReturnsOrders()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders?status=New");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithAuth_CreatesOrder()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Create client first
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", new
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
        var response = await _client.PostAsJsonAsync("/api/orders", newOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateStatus_WithAuth_UpdatesOrderStatus()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Create client and order
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", new
        {
            name = "Status Test Client",
            phone = "+7-999-666-77-88"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientDto>();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 10000m
        });
        var location = orderResponse.Headers.Location?.ToString();
        var orderId = location?.Split('/').Last();

        // Act - отправляем число (1 = InProgress)
        var response = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status", 1);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_WithValidId_ReturnsOrder()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Get all orders first
        var ordersResponse = await _client.GetAsync("/api/orders");
        ordersResponse.EnsureSuccessStatusCode();
        
        // Create a new order
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", new
        {
            name = "Get Order Client",
            phone = "+7-999-777-88-99"
        });
        clientResponse.EnsureSuccessStatusCode();
        
        // Parse client ID
        using var clientDoc = System.Text.Json.JsonDocument.Parse(await clientResponse.Content.ReadAsStringAsync());
        var clientId = clientDoc.RootElement.GetProperty("id").GetInt32();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            clientId = clientId,
            serviceIds = new List<int>(),
            totalPrice = 5000m
        });
        
        // Assert order was created
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Delete_WithAdminRole_DeletesOrder()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var clientResponse = await _client.PostAsJsonAsync("/api/clients", new
        {
            name = "Delete Order Client",
            phone = "+7-999-888-99-00"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientDto>();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 3000m
        });
        var location = orderResponse.Headers.Location?.ToString();
        var orderId = location?.Split('/').Last();

        // Act
        var response = await _client.DeleteAsync($"/api/orders/{orderId}");

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
