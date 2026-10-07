using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportServiceTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly FakeHttpHandler _handler = new();
    private readonly List<TimeSpan> _delays = new();
    private readonly BugReportDraft _draft = new("Title", "Body");

    public void Dispose() => _dir.Dispose();

    private GitHubTokenStore NewStore() => new(_dir.File("github.token"));

    private BugReportService NewService(GitHubTokenStore store, string clientId = "client-xyz")
    {
        var http = new HttpClient(_handler);
        var flow = new GitHubDeviceFlowClient(http, (delay, _) =>
        {
            _delays.Add(delay);
            return Task.CompletedTask;
        });
        return new BugReportService(flow, new GitHubIssueClient(http), store, clientId);
    }

    private void QueueSuccessfulSignIn(string token)
    {
        _handler.EnqueueOk(BugReportJson.DeviceCode)
            .EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Token(token));
    }

    [Fact]
    public async Task NoClientId_ThrowsNotConfigured_WithoutNetwork()
    {
        var service = NewService(NewStore(), clientId: "");

        service.IsAutomaticModeAvailable.Should().BeFalse();
        var act = () => service.SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.NotConfigured);
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task StoredToken_IsUsedWithoutSignIn()
    {
        var store = NewStore();
        store.Save("gho_saved");
        _handler.Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);
        var progress = new RecordingProgress();

        var issue = await NewService(store).SubmitAsync(_draft, progress);

        issue.Number.Should().Be(42);
        _handler.Requests.Should().ContainSingle();
        _handler.Requests[0].Header("Authorization").Should().Be("Bearer gho_saved");
        progress.States.Should().Equal(BugReportState.CreatingIssue, BugReportState.Completed);
    }

    [Fact]
    public async Task NoStoredToken_RunsDeviceFlow_SavesTokenAndCreatesIssue()
    {
        var store = NewStore();
        QueueSuccessfulSignIn("gho_fresh");
        _handler.Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);
        var progress = new RecordingProgress();

        var issue = await NewService(store).SubmitAsync(_draft, progress);

        issue.HtmlUrl.Should().Be("https://github.com/ibuildrun/hqstudio/issues/42");
        store.Load().Should().Be("gho_fresh");
        _handler.Requests.Select(r => r.Url).Should().Equal(
            "https://github.com/login/device/code",
            "https://github.com/login/oauth/access_token",
            "https://github.com/login/oauth/access_token",
            "https://api.github.com/repos/ibuildrun/hqstudio/issues");
        _handler.Requests[3].Header("Authorization").Should().Be("Bearer gho_fresh");
        progress.States.Should().Equal(
            BugReportState.RequestingCode, BugReportState.WaitingForUser,
            BugReportState.CreatingIssue, BugReportState.Completed);
        progress.Items[1].DeviceCode!.UserCode.Should().Be("ABCD-1234");
    }

    [Fact]
    public async Task Unauthorized_ForStoredToken_ClearsItAndRestartsDeviceFlow()
    {
        var store = NewStore();
        store.Save("gho_revoked");
        string? tokenWhenSignInStarted = "not captured";
        _handler.Enqueue(HttpStatusCode.Unauthorized, "{\"message\":\"Bad credentials\"}");
        _handler.Enqueue(HttpStatusCode.OK, BugReportJson.DeviceCode, _ => tokenWhenSignInStarted = store.Load());
        _handler.EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Token("gho_new"));
        _handler.Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);
        var progress = new RecordingProgress();

        var issue = await NewService(store).SubmitAsync(_draft, progress);

        issue.Number.Should().Be(42);
        tokenWhenSignInStarted.Should().BeNull(because: "the rejected token must be deleted before the sign-in starts");
        store.Load().Should().Be("gho_new", because: "the revoked token must be replaced by the new one");
        _handler.Requests.Should().HaveCount(5);
        _handler.Requests[0].Header("Authorization").Should().Be("Bearer gho_revoked");
        _handler.Requests[1].Url.Should().Be("https://github.com/login/device/code");
        _handler.Requests[4].Header("Authorization").Should().Be("Bearer gho_new");
        progress.States.Should().Equal(
            BugReportState.CreatingIssue, BugReportState.RequestingCode, BugReportState.WaitingForUser,
            BugReportState.CreatingIssue, BugReportState.Completed);
    }

    [Fact]
    public async Task Unauthorized_ForFreshToken_DeletesItAndStopsWithoutLooping()
    {
        var store = NewStore();
        QueueSuccessfulSignIn("gho_fresh");
        _handler.Enqueue(HttpStatusCode.Unauthorized, "{\"message\":\"Bad credentials\"}");

        var act = () => NewService(store).SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Unauthorized);
        store.Load().Should().BeNull();
        _handler.Requests.Count(r => r.Url.EndsWith("/device/code")).Should().Be(1, because: "only one sign-in attempt per submit");
    }

    [Fact]
    public async Task NetworkFailureAfterSignIn_KeepsTheNewToken()
    {
        var store = NewStore();
        QueueSuccessfulSignIn("gho_fresh");
        _handler.EnqueueThrow(new HttpRequestException("offline"));

        var act = () => NewService(store).SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Network);
        store.Load().Should().Be("gho_fresh", because: "the user should not have to sign in again just because of a network blip");
    }

    [Fact]
    public async Task NetworkFailure_ForStoredToken_KeepsToken()
    {
        var store = NewStore();
        store.Save("gho_saved");
        _handler.EnqueueThrow(new HttpRequestException("offline"));

        var act = () => NewService(store).SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Network);
        store.Load().Should().Be("gho_saved");
    }

    [Fact]
    public async Task DeniedSignIn_DoesNotSaveAnything()
    {
        var store = NewStore();
        _handler.EnqueueOk(BugReportJson.DeviceCode).EnqueueOk(BugReportJson.Error("access_denied"));

        var act = () => NewService(store).SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.AuthDenied);
        store.Load().Should().BeNull();
    }

    [Fact]
    public async Task ForbiddenFromGitHub_KeepsToken()
    {
        var store = NewStore();
        store.Save("gho_saved");
        _handler.Enqueue(HttpStatusCode.Forbidden, "{\"message\":\"Resource not accessible\"}");

        var act = () => NewService(store).SubmitAsync(_draft);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Forbidden);
        store.Load().Should().Be("gho_saved");
    }
}
