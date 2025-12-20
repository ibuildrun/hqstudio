using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class UpdatesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public UpdatesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { login = "admin", password = "admin" });
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.Token;
    }

    [Fact]
    public async Task CheckForUpdates_WithoutDesktopHeader_ReturnsOk()
    {
        // API не требует заголовок X-Client-Type для check endpoint
        var response = await _client.GetAsync("/api/updates/check?currentVersion=1.0.0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CheckForUpdates_WithDesktopHeader_ReturnsOk()
    {
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        
        var response = await _client.GetAsync("/api/updates/check?currentVersion=1.0.0");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateCheckResponse>();
        result.Should().NotBeNull();
        result!.CurrentVersion.Should().Be("1.0.0");
    }

    [Fact]
    public async Task GetLatest_WithoutDesktopHeader_ReturnsNotFoundOrOk()
    {
        // API не требует заголовок X-Client-Type строго
        var response = await _client.GetAsync("/api/updates/latest");
        // Может вернуть NotFound если нет обновлений или OK если есть
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatest_WithDesktopHeader_NoUpdates_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        
        var response = await _client.GetAsync("/api/updates/latest");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_WithoutDesktopHeader_ReturnsNotFound()
    {
        // Без заголовка возвращает NotFound для несуществующего обновления
        var response = await _client.GetAsync("/api/updates/download/1");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_NonExistingUpdate_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        
        var response = await _client.GetAsync("/api/updates/download/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllUpdates_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/updates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUpdates_WithAdminAuth_ReturnsOk()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/updates");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateUpdate_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/updates/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateUpdate_NonExisting_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync("/api/updates/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
