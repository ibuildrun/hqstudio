using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs.Auth;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Property-based tests for authorization behavior.
/// Validates Requirements 2.2, 2.3, 2.5, 2.6 from requirements.md
/// </summary>
public class AuthorizationPropertyTests : IntegrationTestBase
{
    #region Property 1: Desktop Authorization Bypass
    
    /// <summary>
    /// Property 1: For any HTTP request with X-Client-Type header set to "Desktop" 
    /// to an endpoint with [DesktopOrAuthorize] attribute, the system SHALL allow 
    /// the request regardless of JWT token presence or validity.
    /// Validates: Requirements 2.2, 2.5
    /// Note: /api/dashboard uses [Authorize] not [DesktopOrAuthorize] - will be refactored in Task 9.3
    /// </summary>
    [Theory]
    [InlineData("/api/clients", "GET")]
    [InlineData("/api/orders", "GET")]
    [InlineData("/api/callbacks", "GET")]
    [InlineData("/api/sessions", "GET")]
    public async Task DesktopClient_WithoutJwtToken_CanAccessProtectedEndpoints(string endpoint, string method)
    {
        // Arrange - Desktop client without any JWT token
        AddDesktopClientHeader();
        
        // Act
        var response = method switch
        {
            "GET" => await Client.GetAsync(endpoint),
            _ => throw new ArgumentException($"Unsupported method: {method}")
        };
        
        // Assert - Should NOT be Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"Desktop client should bypass JWT auth for {endpoint}");
    }

    [Theory]
    [InlineData("/api/clients", "GET")]
    [InlineData("/api/orders", "GET")]
    public async Task DesktopClient_WithInvalidJwtToken_CanAccessProtectedEndpoints(string endpoint, string method)
    {
        // Arrange - Desktop client with invalid JWT token
        AddDesktopClientHeader();
        Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
        
        // Act
        var response = method switch
        {
            "GET" => await Client.GetAsync(endpoint),
            _ => throw new ArgumentException($"Unsupported method: {method}")
        };
        
        // Assert - Should NOT be Unauthorized (Desktop bypasses auth)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"Desktop client should bypass JWT validation for {endpoint}");
    }

    [Fact]
    public async Task DesktopClient_CanAccessRoleProtectedEndpoint_WithoutRole()
    {
        // Arrange - Desktop client without any authentication
        AddDesktopClientHeader();
        
        // Act - Try to access Admin-only endpoint
        var response = await Client.DeleteAsync("/api/orders/999");
        
        // Assert - Should NOT be Forbidden (Desktop bypasses role check)
        // Note: May return NotFound if order doesn't exist, but NOT Forbidden
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Desktop client should bypass role checks");
    }

    #endregion

    #region Property 2: Web Authorization Enforcement

    /// <summary>
    /// Property 2: For any HTTP request without X-Client-Type "Desktop" header 
    /// to an endpoint with [DesktopOrAuthorize] attribute, the system SHALL return 
    /// 401 Unauthorized if no valid JWT token is provided.
    /// Validates: Requirements 2.3, 2.5
    /// </summary>
    [Theory]
    [InlineData("/api/clients")]
    [InlineData("/api/orders")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/callbacks")]
    public async Task WebClient_WithoutJwtToken_ReceivesUnauthorized(string endpoint)
    {
        // Arrange - Web client (no Desktop header, no JWT)
        // Client is already a "web" client by default
        
        // Act
        var response = await Client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Web client without JWT should receive 401 for {endpoint}");
    }

    [Theory]
    [InlineData("/api/clients")]
    [InlineData("/api/orders")]
    [InlineData("/api/dashboard")]
    public async Task WebClient_WithInvalidJwtToken_ReceivesUnauthorized(string endpoint)
    {
        // Arrange - Web client with invalid JWT
        Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
        
        // Act
        var response = await Client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Web client with invalid JWT should receive 401 for {endpoint}");
    }

    [Fact]
    public async Task WebClient_WithValidJwtToken_CanAccessProtectedEndpoints()
    {
        // Arrange - Authenticate as admin
        await AuthenticateAsync();
        
        // Act
        var response = await Client.GetAsync("/api/clients");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Authenticated web client should access protected endpoints");
    }

    [Fact]
    public async Task WebClient_WithAdminRole_CanAccessAdminEndpoint()
    {
        // Arrange - Authenticate as admin
        await AuthenticateAsync();
        
        // Act - Try to access Admin-only endpoint (permanent delete)
        var response = await Client.DeleteAsync("/api/orders/999/permanent");
        
        // Assert - Should NOT be Forbidden or Unauthorized (may be NotFound if order doesn't exist)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Admin should access admin-only endpoints");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "Admin should be authenticated");
    }

    #endregion

    #region Property: DesktopOnly Attribute

    [Fact]
    public async Task DesktopOnlyEndpoint_WithDesktopHeader_AllowsAccess()
    {
        // Arrange
        AddDesktopClientHeader();
        
        // Act - Access desktop-only cleanup endpoint
        var response = await Client.DeleteAsync("/api/orders/cleanup/without-clients");
        
        // Assert - Should NOT be Forbidden
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Desktop client should access DesktopOnly endpoints");
    }

    [Fact]
    public async Task DesktopOnlyEndpoint_WithoutDesktopHeader_ReturnsForbidden()
    {
        // Arrange - Web client (even authenticated)
        await AuthenticateAsync();
        
        // Act - Try to access desktop-only endpoint
        var response = await Client.DeleteAsync("/api/orders/cleanup/without-clients");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Web client should NOT access DesktopOnly endpoints");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("desktop")]  // lowercase
    [InlineData("DESKTOP")]  // uppercase
    [InlineData("Desktop")]  // mixed case
    public async Task DesktopHeader_IsCaseInsensitive(string headerValue)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("X-Client-Type", headerValue);
        
        // Act
        var response = await Client.GetAsync("/api/clients");
        
        // Assert - All case variations should work
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"Desktop header value '{headerValue}' should be recognized");
    }

    [Theory]
    [InlineData("Web")]
    [InlineData("Mobile")]
    [InlineData("")]
    [InlineData("DesktopApp")]
    public async Task NonDesktopClientType_RequiresAuthentication(string headerValue)
    {
        // Arrange
        if (!string.IsNullOrEmpty(headerValue))
        {
            Client.DefaultRequestHeaders.Add("X-Client-Type", headerValue);
        }
        
        // Act
        var response = await Client.GetAsync("/api/clients");
        
        // Assert - Non-desktop clients require auth
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Client type '{headerValue}' should require authentication");
    }

    #endregion
}
