using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

public class ActivityLogControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ActivityLogControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/activitylog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsActivityLogs()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/activitylog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Page.Should().BeGreaterThan(0);
        result.PageSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/activitylog?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetAll_WithSourceFilter_ReturnsFilteredLogs()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        // Сначала создадим запись с определённым источником
        await client.PostAsJsonAsync("/api/activitylog", new
        {
            action = "Test action",
            source = "Desktop"
        });

        var response = await client.GetAsync("/api/activitylog?source=Desktop");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(x => x.Source == "Desktop");
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedLog()
    {
        var client = await _factory.GetAuthenticatedClientAsync();
        var request = new
        {
            action = "Тестовое действие",
            entityType = "Client",
            entityId = 123,
            details = "Дополнительная информация",
            source = "Web"
        };

        var response = await client.PostAsJsonAsync("/api/activitylog", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Create_WithMinimalData_ReturnsCreatedLog()
    {
        var client = await _factory.GetAuthenticatedClientAsync();
        var request = new { action = "Минимальное действие" };

        var response = await client.PostAsJsonAsync("/api/activitylog", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new { action = "Test action" };

        var response = await client.PostAsJsonAsync("/api/activitylog", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsStats()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/activitylog/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogStats>();
        result.Should().NotBeNull();
        result!.TotalAll.Should().BeGreaterThanOrEqualTo(0);
        result.TotalToday.Should().BeGreaterThanOrEqualTo(0);
        result.TotalWeek.Should().BeGreaterThanOrEqualTo(0);
        result.BySource.Should().NotBeNull();
        result.ByUser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/activitylog/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithUserIdFilter_ReturnsFilteredLogs()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        // Создаём запись от текущего пользователя
        await client.PostAsJsonAsync("/api/activitylog", new { action = "User specific action" });

        // Фильтруем по userId=1 (admin)
        var response = await client.GetAsync("/api/activitylog?userId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(x => x.UserId == 1);
    }

    [Fact]
    public async Task GetAll_OrderedByCreatedAtDescending()
    {
        var client = await _factory.GetAuthenticatedClientAsync();

        // Создаём несколько записей
        await client.PostAsJsonAsync("/api/activitylog", new { action = "Action 1" });
        await Task.Delay(100);
        await client.PostAsJsonAsync("/api/activitylog", new { action = "Action 2" });

        var response = await client.GetAsync("/api/activitylog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Count >= 2)
        {
            result.Items.Should().BeInDescendingOrder(x => x.CreatedAt);
        }
    }

    [Fact]
    public async Task Create_SetsCorrectUserInfo()
    {
        var client = await _factory.GetAuthenticatedClientAsync();
        var request = new { action = "Check user info" };

        await client.PostAsJsonAsync("/api/activitylog", request);

        var response = await client.GetAsync("/api/activitylog?pageSize=1");
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        var lastLog = result.Items.First();
        lastLog.UserId.Should().Be(1); // admin user
        lastLog.UserName.Should().NotBeNullOrEmpty();
    }

    // DTOs for deserialization
    private class ActivityLogResponse
    {
        public List<ActivityLogItem> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private class ActivityLogItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string Source { get; set; } = "";
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class CreateActivityLogResponse
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class ActivityLogStats
    {
        public int TotalToday { get; set; }
        public int TotalWeek { get; set; }
        public int TotalAll { get; set; }
        public List<SourceStat> BySource { get; set; } = new();
        public List<UserStat> ByUser { get; set; } = new();
    }

    private class SourceStat
    {
        public string Source { get; set; } = "";
        public int Count { get; set; }
    }

    private class UserStat
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int Count { get; set; }
    }
}
