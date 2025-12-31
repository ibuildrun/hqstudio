using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs.Dashboard;
using Xunit;

namespace HQStudio.API.Tests;

public class DashboardControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsDashboardStats()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStructure()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/dashboard");
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();

        // Assert
        stats!.TotalClients.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalOrders.Should().BeGreaterThanOrEqualTo(0);
        stats.NewCallbacks.Should().BeGreaterThanOrEqualTo(0);
        stats.MonthlyRevenue.Should().BeGreaterThanOrEqualTo(0);
        stats.PopularServices.Should().NotBeNull();
        stats.RecentOrders.Should().NotBeNull();
    }
}
