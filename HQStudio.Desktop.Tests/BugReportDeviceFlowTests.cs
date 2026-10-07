using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportDeviceFlowTests
{
    private sealed class Harness
    {
        public FakeHttpHandler Handler { get; } = new();
        public List<TimeSpan> Delays { get; } = new();
        public GitHubDeviceFlowClient Client { get; }

        public Harness()
        {
            Client = new GitHubDeviceFlowClient(new HttpClient(Handler), (delay, _) =>
            {
                Delays.Add(delay);
                return Task.CompletedTask;
            });
        }
    }

    private static readonly DeviceCodeInfo Code = new("dev-123", "ABCD-1234", "https://github.com/login/device", 900, 5);

    [Fact]
    public async Task RequestDeviceCode_SendsExpectedRequestAndParsesResponse()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.DeviceCode);

        var code = await h.Client.RequestDeviceCodeAsync("client-xyz");

        code.Should().Be(Code);
        var request = h.Handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.Url.Should().Be("https://github.com/login/device/code");
        request.Header("Accept").Should().Contain("application/json");
        request.Header("User-Agent").Should().Contain("HQStudio-Desktop");
        request.Form().Should().Contain("client_id", "client-xyz").And.Contain("scope", "public_repo");
    }

    [Theory]
    [InlineData("device_flow_disabled")]
    [InlineData("incorrect_client_credentials")]
    public async Task RequestDeviceCode_NotUsableApp_ThrowsNotConfigured(string error)
    {
        var h = new Harness();
        h.Handler.Enqueue(HttpStatusCode.BadRequest, BugReportJson.Error(error));

        var act = () => h.Client.RequestDeviceCodeAsync("client-xyz");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.NotConfigured);
    }

    [Fact]
    public async Task RequestDeviceCode_NetworkFailure_ThrowsNetwork()
    {
        var h = new Harness();
        h.Handler.EnqueueThrow(new HttpRequestException("offline"));

        var act = () => h.Client.RequestDeviceCodeAsync("client-xyz");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Network);
    }

    [Fact]
    public async Task RequestDeviceCode_IncompleteResponse_Throws()
    {
        var h = new Harness();
        h.Handler.EnqueueOk("{\"device_code\":\"x\"}");

        var act = () => h.Client.RequestDeviceCodeAsync("client-xyz");

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Unexpected);
    }

    [Fact]
    public async Task Poll_PendingThenSuccess_ReturnsToken()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Token("gho_realtoken"));

        var token = await h.Client.PollForTokenAsync("client-xyz", Code);

        token.Should().Be("gho_realtoken");
        h.Handler.Requests.Should().HaveCount(3);
        h.Delays.Should().Equal(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Poll_SendsDeviceCodeGrant()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Token("gho_realtoken"));

        await h.Client.PollForTokenAsync("client-xyz", Code);

        var request = h.Handler.Requests.Single();
        request.Url.Should().Be("https://github.com/login/oauth/access_token");
        request.Header("Accept").Should().Contain("application/json");
        request.Form().Should()
            .Contain("client_id", "client-xyz")
            .And.Contain("device_code", "dev-123")
            .And.Contain("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
    }

    [Fact]
    public async Task Poll_SlowDownWithoutInterval_AddsFiveSeconds()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("slow_down"))
            .EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Token("gho_realtoken"));

        await h.Client.PollForTokenAsync("client-xyz", Code);

        h.Delays.Select(d => (int)d.TotalSeconds).Should().Equal(5, 10, 10);
    }

    [Fact]
    public async Task Poll_SlowDownWithServerInterval_UsesServerValue()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("slow_down", interval: 20))
            .EnqueueOk(BugReportJson.Token("gho_realtoken"));

        await h.Client.PollForTokenAsync("client-xyz", Code);

        h.Delays.Select(d => (int)d.TotalSeconds).Should().Equal(5, 20);
    }

    [Fact]
    public async Task Poll_ExpiredToken_Throws()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("authorization_pending"))
            .EnqueueOk(BugReportJson.Error("expired_token"));

        var act = () => h.Client.PollForTokenAsync("client-xyz", Code);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.AuthExpired);
    }

    [Fact]
    public async Task Poll_AccessDenied_Throws()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("access_denied"));

        var act = () => h.Client.PollForTokenAsync("client-xyz", Code);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.AuthDenied);
    }

    [Fact]
    public async Task Poll_StopsOnceLifetimeIsExceeded_WithoutMoreRequests()
    {
        var h = new Harness();
        for (var i = 0; i < 10; i++) h.Handler.EnqueueOk(BugReportJson.Error("authorization_pending"));
        var shortLived = Code with { ExpiresInSeconds = 12 };

        var act = () => h.Client.PollForTokenAsync("client-xyz", shortLived);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.AuthExpired);
        h.Handler.Requests.Should().HaveCount(3, because: "5 + 5 + 5 seconds of waiting already exceeds 12");
    }

    [Fact]
    public async Task Poll_UnknownError_Throws()
    {
        var h = new Harness();
        h.Handler.EnqueueOk(BugReportJson.Error("something_new"));

        var act = () => h.Client.PollForTokenAsync("client-xyz", Code);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Unexpected);
    }

    [Fact]
    public async Task Poll_SingleNetworkGlitch_IsRetried()
    {
        var h = new Harness();
        h.Handler.EnqueueThrow(new HttpRequestException("blip"))
            .EnqueueOk(BugReportJson.Token("gho_realtoken"));

        var token = await h.Client.PollForTokenAsync("client-xyz", Code);

        token.Should().Be("gho_realtoken");
        h.Delays.Should().HaveCount(2);
    }

    [Fact]
    public async Task Poll_RepeatedNetworkFailure_GivesUp()
    {
        var h = new Harness();
        for (var i = 0; i < 5; i++) h.Handler.EnqueueThrow(new HttpRequestException("offline"));

        var act = () => h.Client.PollForTokenAsync("client-xyz", Code);

        (await act.Should().ThrowAsync<BugReportException>()).Which.Kind.Should().Be(BugReportErrorKind.Network);
        h.Handler.Requests.Count.Should().BeLessThan(5);
    }

    [Fact]
    public async Task Poll_CancellationDuringDelay_Propagates()
    {
        var handler = new FakeHttpHandler();
        using var cts = new CancellationTokenSource();
        var client = new GitHubDeviceFlowClient(new HttpClient(handler), (_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var act = () => client.PollForTokenAsync("client-xyz", Code, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Requests.Should().BeEmpty();
    }
}
