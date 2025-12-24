using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Тесты полного workflow заказа: создание → обработка → завершение
/// </summary>
public class OrderWorkflowTests : IntegrationTestBase
{
    [Fact]
    public async Task OrderWorkflow_CreateToComplete_WorksCorrectly()
    {
        // Arrange
        await AuthenticateAsync();

        // 1. Создаём клиента
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Workflow Test Client",
            phone = "+7-999-111-00-00",
            carModel = "BMW X5",
            licensePlate = "А111АА777"
        });
        clientResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var client = await clientResponse.Content.ReadFromJsonAsync<WorkflowClientDto>();

        // 2. Получаем услуги
        var servicesResponse = await Client.GetAsync("/api/services");
        var services = await servicesResponse.Content.ReadFromJsonAsync<List<WorkflowServiceDto>>();
        var serviceIds = services!.Take(2).Select(s => s.Id).ToList();

        // 3. Создаём заказ
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = serviceIds,
            totalPrice = 25000m,
            notes = "Тестовый заказ для workflow"
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = orderResponse.Headers.Location?.ToString()?.Split('/').Last();

        // 4. Проверяем статус - должен быть New (0)
        var getOrderResponse = await Client.GetAsync($"/api/orders/{orderId}");
        var order = await getOrderResponse.Content.ReadFromJsonAsync<WorkflowOrderDto>();
        order!.Status.Should().Be(0); // New

        // 5. Переводим в работу (InProgress = 1)
        var inProgressResponse = await Client.PutAsJsonAsync($"/api/orders/{orderId}/status", 1);
        inProgressResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. Проверяем статус
        var checkResponse1 = await Client.GetAsync($"/api/orders/{orderId}");
        var orderInProgress = await checkResponse1.Content.ReadFromJsonAsync<WorkflowOrderDto>();
        orderInProgress!.Status.Should().Be(1); // InProgress

        // 7. Завершаем заказ (Completed = 2)
        var completeResponse = await Client.PutAsJsonAsync($"/api/orders/{orderId}/status", 2);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. Проверяем финальный статус
        var finalResponse = await Client.GetAsync($"/api/orders/{orderId}");
        var completedOrder = await finalResponse.Content.ReadFromJsonAsync<WorkflowOrderDto>();
        completedOrder!.Status.Should().Be(2); // Completed
    }

    [Fact]
    public async Task OrderWorkflow_CancelOrder_WorksCorrectly()
    {
        // Arrange
        await AuthenticateAsync();

        // Создаём клиента
        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Cancel Test Client",
            phone = "+7-999-222-00-00"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<WorkflowClientDto>();

        // Создаём заказ
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 10000m
        });
        var orderId = orderResponse.Headers.Location?.ToString()?.Split('/').Last();

        // Act - отменяем заказ (Cancelled = 3)
        var cancelResponse = await Client.PutAsJsonAsync($"/api/orders/{orderId}/status", 3);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var checkResponse = await Client.GetAsync($"/api/orders/{orderId}");
        var cancelledOrder = await checkResponse.Content.ReadFromJsonAsync<WorkflowOrderDto>();
        cancelledOrder!.Status.Should().Be(3); // Cancelled
    }

    [Fact]
    public async Task OrderWorkflow_GetOrderDetails_WorksCorrectly()
    {
        // Arrange
        await AuthenticateAsync();

        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Get Order Client",
            phone = "+7-999-333-00-00"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<WorkflowClientDto>();

        var orderResponse = await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 5000m,
            notes = "Test notes"
        });
        var orderId = orderResponse.Headers.Location?.ToString()?.Split('/').Last();

        // Act - получаем заказ
        var getResponse = await Client.GetAsync($"/api/orders/{orderId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OrderWorkflow_FilterByStatus_ReturnsOrders()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - получаем заказы со статусом New
        var response = await Client.GetAsync("/api/orders?status=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OrderWorkflow_FilterByClient_ReturnsOrders()
    {
        // Arrange
        await AuthenticateAsync();

        var clientResponse = await Client.PostAsJsonAsync("/api/clients", new
        {
            name = "Filter Test Client",
            phone = "+7-999-444-00-00"
        });
        var client = await clientResponse.Content.ReadFromJsonAsync<WorkflowClientDto>();

        // Создаём заказ для этого клиента
        await Client.PostAsJsonAsync("/api/orders", new
        {
            clientId = client!.Id,
            serviceIds = new List<int>(),
            totalPrice = 1000m
        });

        // Act
        var response = await Client.GetAsync($"/api/orders?clientId={client.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private class WorkflowClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class WorkflowServiceDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    private class WorkflowOrderDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }
}
