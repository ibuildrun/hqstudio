using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class SiteContentControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetSiteData_ReturnsAllPublicData()
    {
        // Act
        var response = await Client.GetAsync("/api/site");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<SiteDataResponse>();
        data.Should().NotBeNull();
        data!.Services.Should().NotBeNull();
        data.Blocks.Should().NotBeNull();
        data.Testimonials.Should().NotBeNull();
        data.Faq.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSiteData_ContainsSeededServices()
    {
        // Act
        var response = await Client.GetAsync("/api/site");
        var data = await response.Content.ReadFromJsonAsync<SiteDataResponse>();

        // Assert
        data!.Services.Should().Contain(s => s.Title == "Доводчики дверей");
        data.Services.Should().Contain(s => s.Title == "Шумоизоляция");
    }

    [Fact]
    public async Task GetBlocks_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/site/blocks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBlocks_WithAuth_ReturnsBlocks()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/site/blocks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocks = await response.Content.ReadFromJsonAsync<List<SiteBlock>>();
        blocks.Should().NotBeNull();
        blocks!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTestimonials_WithAuth_ReturnsTestimonials()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/site/testimonials");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var testimonials = await response.Content.ReadFromJsonAsync<List<Testimonial>>();
        testimonials.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFaq_WithAuth_ReturnsFaq()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/site/faq");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var faq = await response.Content.ReadFromJsonAsync<List<FaqItem>>();
        faq.Should().NotBeNull();
    }
}
