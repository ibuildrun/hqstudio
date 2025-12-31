using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using Xunit;

namespace HQStudio.API.Tests;

public class UpdatesControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CheckForUpdates_WithDesktopHeader_ReturnsOk()
    {
        AddDesktopClientHeader();
        
        var response = await Client.GetAsync("/api/updates/check?currentVersion=1.0.0");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateCheckResult>();
        result.Should().NotBeNull();
        result!.CurrentVersion.Should().Be("1.0.0");
    }

    [Fact]
    public async Task GetLatest_WithDesktopHeader_NoUpdates_ReturnsNotFound()
    {
        AddDesktopClientHeader();
        
        var response = await Client.GetAsync("/api/updates/latest");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_NonExistingUpdate_ReturnsNotFound()
    {
        AddDesktopClientHeader();
        
        var response = await Client.GetAsync("/api/updates/download/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllUpdates_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/updates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUpdates_WithAdminAuth_ReturnsOk()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/updates");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateUpdate_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync("/api/updates/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateUpdate_NonExisting_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.DeleteAsync("/api/updates/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private class UpdateCheckResult
    {
        public string CurrentVersion { get; set; } = "";
        public bool UpdateAvailable { get; set; }
    }
}
