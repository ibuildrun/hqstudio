using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using HQStudio.Services.BugReport;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportViewModelTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly FakeHttpHandler _handler = new();
    private readonly FakeShell _shell = new();
    private TaskCompletionSource? _gate;

    public void Dispose() => _dir.Dispose();

    private BugReportViewModel Create(Exception? crash = null, string clientId = "client-xyz", bool withToken = false)
    {
        var http = new HttpClient(_handler);
        var store = new GitHubTokenStore(_dir.File("github.token"));
        if (withToken) store.Save("gho_saved");

        var flow = new GitHubDeviceFlowClient(http, (_, ct) => (_gate?.Task ?? Task.CompletedTask).WaitAsync(ct));
        var service = new BugReportService(flow, new GitHubIssueClient(http), store, clientId);
        var collector = new DiagnosticsCollector(new FakeProcessRunner(), new FakeFileReader(), @"C:\none");

        return new BugReportViewModel(crash, service, collector, _shell, () => "http://localhost:5000");
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("condition was not met in 5 seconds");
            await Task.Delay(10);
        }
    }

    [Fact]
    public void StartsOnFormWithDiagnosticsTicked()
    {
        var vm = Create();

        vm.Stage.Should().Be(BugReportStage.Form);
        vm.IncludeDiagnostics.Should().BeTrue();
        vm.StepTitle.Should().Be("Шаг 1: опишите проблему");
    }

    [Fact]
    public async Task ManualReport_WithoutDescription_StaysOnForm()
    {
        var vm = Create();

        await vm.GoToReviewAsync();

        vm.Stage.Should().Be(BugReportStage.Form);
        vm.ValidationMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CrashReport_PrefillsTitleAndAllowsEmptyDescription()
    {
        var vm = Create(new InvalidOperationException("Sequence contains no elements"));

        vm.Title.Should().Be("Crash: InvalidOperationException: Sequence contains no elements");
        await vm.GoToReviewAsync();

        vm.Stage.Should().Be(BugReportStage.Review);
        vm.PreviewBody.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task Preview_IsSanitized_AndIsExactlyWhatGetsSent()
    {
        var vm = Create(withToken: true);
        vm.Title = "Не печатается";
        vm.Description = "Не работает.\nmy password=hunter2 и token=abc123";
        _handler.Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);

        await vm.GoToReviewAsync();
        var shownTitle = vm.PreviewTitle;
        var shownBody = vm.PreviewBody;
        await vm.SendViaGitHubAsync();

        shownBody.Should().NotContain("hunter2").And.NotContain("abc123");
        shownBody.Should().Contain("Не работает.");
        vm.Stage.Should().Be(BugReportStage.Success);
        vm.IssueUrl.Should().Be("https://github.com/ibuildrun/hqstudio/issues/42");

        using var sent = JsonDocument.Parse(_handler.Requests.Single().Body!);
        sent.RootElement.GetProperty("title").GetString().Should().Be(shownTitle);
        sent.RootElement.GetProperty("body").GetString().Should().Be(shownBody);
    }

    [Fact]
    public async Task UntickingDiagnostics_RemovesThemFromPreview()
    {
        var vm = Create(new InvalidOperationException("boom"));
        vm.IncludeDiagnostics = false;

        await vm.GoToReviewAsync();

        vm.PreviewBody.Should().NotContain("## Diagnostics").And.NotContain("boom");
    }

    [Fact]
    public async Task SendViaBrowser_OpensPrefilledUrl_AndCopiesFullBodyToClipboard()
    {
        var vm = Create(clientId: "");
        vm.Description = "Что-то сломалось";
        await vm.GoToReviewAsync();

        vm.SendViaBrowserCommand.Execute(null);

        _shell.OpenedUrls.Should().ContainSingle().Which.Should()
            .StartWith("https://github.com/ibuildrun/hqstudio/issues/new?title=").And.EndWith("&labels=from-app");
        _shell.Clipboard.Should().ContainSingle().Which.Should().Be(vm.PreviewBody);
        vm.Stage.Should().Be(BugReportStage.Success);
        vm.Succeeded.Should().BeTrue();
        vm.IsIssueLinkVisible.Should().BeFalse();
        _handler.Requests.Should().BeEmpty(because: "browser mode never talks to GitHub itself");
    }

    [Fact]
    public async Task SendViaBrowser_WhenBrowserCannotOpen_ShowsError()
    {
        _shell.OpenFailure = new InvalidOperationException("no browser");
        var vm = Create(clientId: "");
        vm.Description = "x";
        await vm.GoToReviewAsync();

        vm.SendViaBrowserCommand.Execute(null);

        vm.Stage.Should().Be(BugReportStage.Error);
        vm.Succeeded.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("github.com/ibuildrun/hqstudio/issues/new");
    }

    [Fact]
    public void WithoutClientId_AutomaticModeIsUnavailable()
    {
        Create(clientId: "").IsAutomaticAvailable.Should().BeFalse();
        Create(clientId: "abc").IsAutomaticAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task NetworkFailure_ShowsPlainRussianMessage()
    {
        var vm = Create(withToken: true);
        vm.Description = "x";
        _handler.EnqueueThrow(new HttpRequestException("offline"));
        await vm.GoToReviewAsync();

        await vm.SendViaGitHubAsync();

        vm.Stage.Should().Be(BugReportStage.Error);
        vm.ErrorMessage.Should().Contain("подключение к интернету");
        vm.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task DeviceFlow_ShowsCodeThenFinishes()
    {
        var vm = Create();
        vm.Description = "x";
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.EnqueueOk(BugReportJson.DeviceCode)
            .EnqueueOk(BugReportJson.Token("gho_fresh"))
            .Enqueue(HttpStatusCode.Created, BugReportJson.IssueCreated);
        await vm.GoToReviewAsync();

        var sending = vm.SendViaGitHubAsync();
        await WaitFor(() => vm.Stage == BugReportStage.SigningIn);

        vm.UserCode.Should().Be("ABCD-1234");
        vm.StepTitle.Should().Be("Шаг 3: войдите в GitHub");

        vm.CopyCodeCommand.Execute(null);
        _shell.Clipboard.Should().Contain("ABCD-1234");

        vm.OpenVerificationCommand.Execute(null);
        _shell.OpenedUrls.Should().Contain("https://github.com/login/device");

        _gate.SetResult();
        await sending;

        vm.Stage.Should().Be(BugReportStage.Success);
        vm.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CancellingWhileSigningIn_ReturnsToReview()
    {
        var vm = Create();
        vm.Description = "x";
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.EnqueueOk(BugReportJson.DeviceCode);
        await vm.GoToReviewAsync();

        var sending = vm.SendViaGitHubAsync();
        await WaitFor(() => vm.Stage == BugReportStage.SigningIn);

        vm.CancelSendCommand.Execute(null);
        await sending;

        vm.Stage.Should().Be(BugReportStage.Review);
        vm.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task OpenVerification_IgnoresForeignHosts()
    {
        var vm = Create();
        vm.Description = "x";
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.EnqueueOk(
            "{\"device_code\":\"d\",\"user_code\":\"AAAA-BBBB\",\"verification_uri\":\"http://evil.example/login\",\"expires_in\":900,\"interval\":5}");
        await vm.GoToReviewAsync();

        var sending = vm.SendViaGitHubAsync();
        await WaitFor(() => vm.Stage == BugReportStage.SigningIn);
        vm.OpenVerificationCommand.Execute(null);
        vm.CancelSendCommand.Execute(null);
        await sending;

        _shell.OpenedUrls.Should().ContainSingle().Which.Should().Be("https://github.com/login/device");
    }

    [Theory]
    [InlineData(BugReportErrorKind.Network, "интернету")]
    [InlineData(BugReportErrorKind.AuthExpired, "Время")]
    [InlineData(BugReportErrorKind.AuthDenied, "не был подтверждён")]
    [InlineData(BugReportErrorKind.Unauthorized, "войдите заново")]
    [InlineData(BugReportErrorKind.RateLimited, "Подождите")]
    [InlineData(BugReportErrorKind.NotConfigured, "браузер")]
    public void DescribeError_UsesPlainRussian(BugReportErrorKind kind, string fragment)
    {
        BugReportViewModel.DescribeError(new BugReportException(kind, "technical")).Should().Contain(fragment);
    }
}
