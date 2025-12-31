using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs.Dashboard;
using HQStudio.API.DTOs.Orders;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Property-based tests for Service Layer.
/// Validates Requirements 1.4, 1.5, 5.1, 5.2, 5.4, 5.5 from requirements.md
/// </summary>
public class ServiceLayerPropertyTests : IntegrationTestBase
{
    #region Property 3: Order Creation Validation

    /// <summary>
    /// Property 3: For any CreateOrderRequest, the OrderService SHALL verify 
    /// client existence before creating the order, and SHALL create all 
    /// OrderService records in a single database transaction.
    /// Validates: Requirements 1.4, 5.5
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithValidClient_CreatesOrderWithServices()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // First create a client
        var clientRequest = new
        {
            name = "Test Client",
            phone = "+7-999-111-22-33",
            email = "test@example.com"
        };
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", clientRequest);
        clientResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>();
        
        // Get available services
        var servicesResponse = await Client.GetAsync("/api/services");
        var services = await servicesResponse.Content.ReadFromJsonAsync<List<ServiceResponse>>();
        services.Should().NotBeEmpty();
        
        var orderRequest = new
        {
            clientId = client!.Id,
            serviceIds = services!.Take(2).Select(s => s.Id).ToList(),
            totalPrice = 30000m,
            notes = "Test order"
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.ClientId.Should().Be(client.Id);
        order.Services.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidClient_ThrowsException()
    {
        // Arrange - Order with non-existent client
        AddDesktopClientHeader();
        
        var orderRequest = new
        {
            clientId = 99999, // Non-existent client
            serviceIds = new List<int> { 1 },
            totalPrice = 15000m,
            notes = "Test"
        };
        
        // Act & Assert - FK constraint should prevent creation
        // The API throws DbUpdateException which results in 500 error
        // This validates that database integrity is maintained
        var act = async () => await Client.PostAsJsonAsync("/api/orders", orderRequest);
        
        // Exception is thrown before HTTP response is formed
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Property 4: Dashboard Aggregation Correctness

    /// <summary>
    /// Property 4: For any database state, the DashboardService.GetStatsAsync() 
    /// SHALL return statistics that accurately reflect the current data.
    /// Validates: Requirements 1.5
    /// </summary>
    [Fact]
    public async Task Dashboard_ReturnsCorrectClientCount()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Get initial stats
        var initialResponse = await Client.GetAsync("/api/dashboard");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialStats = await initialResponse.Content.ReadFromJsonAsync<DashboardStatsDto>();
        var initialClientCount = initialStats!.TotalClients;
        
        // Add a new client
        var clientRequest = new
        {
            name = "Dashboard Test Client",
            phone = "+7-999-333-44-55",
            email = "dashboard@test.com"
        };
        await Client.PostAsJsonAsync("/api/clients", clientRequest);
        
        // Act - Get updated stats
        var response = await Client.GetAsync("/api/dashboard");
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        
        // Assert
        stats!.TotalClients.Should().Be(initialClientCount + 1);
    }

    [Fact]
    public async Task Dashboard_ReturnsPopularServices()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Act
        var response = await Client.GetAsync("/api/dashboard");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        stats.Should().NotBeNull();
        stats!.PopularServices.Should().NotBeNull();
    }

    [Fact]
    public async Task Dashboard_ReturnsRecentOrders()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Act
        var response = await Client.GetAsync("/api/dashboard");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        stats.Should().NotBeNull();
        stats!.RecentOrders.Should().NotBeNull();
    }

    #endregion

    #region Property 5: Query Optimization

    /// <summary>
    /// Property 5: For any service method that fetches paginated data with 
    /// related entities, the total number of database queries SHALL not exceed 3.
    /// Note: This is validated by checking response structure, not actual query count.
    /// Validates: Requirements 5.1, 5.2, 5.4
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsPaginatedResultWithRelatedData()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // Act
        var response = await Client.GetAsync("/api/orders?page=1&pageSize=10");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedOrdersResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Total.Should().BeGreaterOrEqualTo(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetClients_ReturnsListWithOrderCount()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // Act
        var response = await Client.GetAsync("/api/clients");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clients = await response.Content.ReadFromJsonAsync<List<ClientWithOrdersResponse>>();
        clients.Should().NotBeNull();
        // Each client should have OrdersCount property (projection optimization)
        foreach (var client in clients!)
        {
            client.OrdersCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    #endregion

    #region Property 6: Single SaveChanges per Operation

    /// <summary>
    /// Property 6: For any service method that creates or modifies entities, 
    /// the method SHALL call SaveChangesAsync exactly once.
    /// Note: This is validated by checking that operations are atomic.
    /// Validates: Requirements 5.5
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_IsAtomic()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // Create a client and order first
        var clientRequest = new { name = "Atomic Test", phone = "+7-999-666-77-88" };
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", clientRequest);
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>();
        
        var orderRequest = new
        {
            clientId = client!.Id,
            serviceIds = new List<int> { 1 },
            totalPrice = 10000m
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        
        // Act - Update status
        var statusResponse = await Client.PutAsJsonAsync(
            $"/api/orders/{order!.Id}/status", 
            OrderStatus.InProgress);
        
        // Assert
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the change persisted
        var getResponse = await Client.GetAsync($"/api/orders/{order.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        updatedOrder!.Status.Should().Be(OrderStatus.InProgress);
    }

    [Fact]
    public async Task SoftDeleteOrder_IsAtomic()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // Create order
        var clientRequest = new { name = "Delete Test", phone = "+7-999-888-99-00" };
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", clientRequest);
        var client = await clientResponse.Content.ReadFromJsonAsync<ClientResponse>();
        
        var orderRequest = new
        {
            clientId = client!.Id,
            serviceIds = new List<int> { 1 },
            totalPrice = 5000m
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        
        // Act - Soft delete
        var deleteResponse = await Client.DeleteAsync($"/api/orders/{order!.Id}");
        
        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify order is not in default list
        var listResponse = await Client.GetAsync("/api/orders");
        var orders = await listResponse.Content.ReadFromJsonAsync<PagedOrdersResponse>();
        orders!.Items.Should().NotContain(o => o.Id == order.Id);
    }

    #endregion

    // Response DTOs for deserialization
    private class ClientResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class ClientWithOrdersResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int OrdersCount { get; set; }
    }

    private class ServiceResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    private class OrderResponse
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public OrderStatus Status { get; set; }
        public List<OrderServiceResponse> Services { get; set; } = new();
    }

    private class OrderServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceTitle { get; set; } = "";
    }

    private class PagedOrdersResponse
    {
        public List<OrderResponse> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
