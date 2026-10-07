using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportIssueClientTests
{
    private static (GitHubIssueClient Client, FakeHttpHandler Handler) Create()
    {
        var handler = new FakeHttpHandler();
        return (new GitHubIssueClient(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task CreateIssue_SendsExpectedRequestShape()
    {
        var (client, handler) = Create();
        handler.Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);

        var issue = await client.CreateIssueAsync("gho_token", "Заголовок \"в кавычках\"", "Тело\nвторая строка");

        issue.Should().Be(new CreatedIssue(42, "https://github.com/ibuildrun/hqstudio/issues/42"));

        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.Url.Should().Be("https://api.github.com/repos/ibuildrun/hqstudio/issues");
        request.Header("Authorization").Should().Be("Bearer gho_token");
        request.Header("User-Agent").Should().Contain("HQStudio-Desktop");
        request.Header("Accept").Should().Contain("application/vnd.github+json");
        request.Header("X-GitHub-Api-Version").Should().Be("2022-11-28");
        request.Header("Content-Type").Should().Contain("application/json");

        using var json = JsonDocument.Parse(request.Body!);
        json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("title", "body");
        json.RootElement.GetProperty("title").GetString().Should().Be("Заголовок \"в кавычках\"");
        json.RootElement.GetProperty("body").GetString().Should().Be("Тело\nвторая строка");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, BugReportErrorKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, BugReportErrorKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, BugReportErrorKind.Rejected)]
    [InlineData(HttpStatusCode.Gone, BugReportErrorKind.Rejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, BugReportErrorKind.Rejected)]
    [InlineData((HttpStatusCode)429, BugReportErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, BugReportErrorKind.ServerError)]
    [InlineData(HttpStatusCode.BadGateway, BugReportErrorKind.ServerError)]
    [InlineData(HttpStatusCode.Conflict, BugReportErrorKind.Unexpected)]
    public async Task CreateIssue_ErrorStatus_MapsToKind(HttpStatusCode status, BugReportErrorKind expected)
    {
        var (client, handler) = Create();
        handler.Enqueue(status, "{\"message\":\"nope\"}");

        var act = () => client.CreateIssueAsync("gho_token", "t", "b");

        var ex = (await act.Should().ThrowAsync<BugReportException>()).Which;
        ex.Kind.Should().Be(expected);
        ex.StatusCode.Should().Be((int)status);
        ex.Message.Should().Contain("nope");
    }

    [Fact]
    public async Task CreateIssue_Forbidden_WithExhaustedRateLimit_IsRateLimited()
    {
        var (client, handler) = Create();
        handler.Enqueue(HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}",
            r => r.Headers.Add("X-RateLimit-Remaining", "0"));

        var act = () => client.CreateIssueAsync("gho_token", "t", "b");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.RateLimited);
    }

    [Fact]
    public async Task CreateIssue_NetworkFailure_ThrowsNetwork()
    {
        var (client, handler) = Create();
        handler.EnqueueThrow(new HttpRequestException("offline"));

        var act = () => client.CreateIssueAsync("gho_token", "t", "b");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Network);
    }

    [Fact]
    public async Task CreateIssue_SuccessWithoutUrl_Throws()
    {
        var (client, handler) = Create();
        handler.Enqueue(HttpStatusCode.Created, "{\"number\":1}");

        var act = () => client.CreateIssueAsync("gho_token", "t", "b");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Unexpected);
    }

    [Fact]
    public async Task CreateIssue_NonJsonError_StillMapsStatus()
    {
        var (client, handler) = Create();
        handler.Enqueue(HttpStatusCode.Unauthorized, "<html>Unauthorized</html>");

        var act = () => client.CreateIssueAsync("gho_token", "t", "b");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Unauthorized);
    }
}
