using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportDraftTests
{
    private const string Profile = @"C:\Users\Ivan Petrov";

    private static DiagnosticsSnapshot Snapshot(
        string? crashLog = null, string? docker = null, string? exception = null,
        InstallInfo? install = null, string? apiUrl = null, string? serverVersion = null) => new()
    {
        AppVersion = "1.19.6",
        WindowsVersion = "Microsoft Windows 10.0.22621",
        DotNetVersion = ".NET 8.0.10",
        ApiUrl = apiUrl,
        ServerVersion = serverVersion,
        Install = install,
        CrashLogTail = crashLog,
        DockerComposePs = docker,
        ExceptionText = exception
    };

    private static BugReportDraft Build(string? title, string? description, DiagnosticsSnapshot snapshot,
        bool diagnostics = true, Exception? crash = null) =>
        BugReportDraftBuilder.Build(title, description, snapshot, diagnostics, crash, Profile);

    [Fact]
    public void Body_StartsWithMarkerAndContainsSectionsInOrder()
    {
        var draft = Build("Не открывается заказ", "Нажимаю на заказ, ничего не происходит", Snapshot(crashLog: "line 1"));

        draft.Body.Split('\n')[0].Should().Be("<!-- hqstudio-app-report -->");
        var description = draft.Body.IndexOf("## Description", StringComparison.Ordinal);
        var environment = draft.Body.IndexOf("## Environment", StringComparison.Ordinal);
        var diagnostics = draft.Body.IndexOf("## Diagnostics", StringComparison.Ordinal);
        description.Should().BeGreaterThan(0);
        environment.Should().BeGreaterThan(description);
        diagnostics.Should().BeGreaterThan(environment);
        draft.Body.Should().Contain("Нажимаю на заказ, ничего не происходит");
    }

    [Fact]
    public void Environment_ListsVersionsAndUrls()
    {
        var install = new InstallInfo(@"C:\hq\server", "http://localhost:3000", "http://localhost:5000");

        var draft = Build("t", "d", Snapshot(install: install, apiUrl: "http://localhost:5000", serverVersion: "2.1.0"));

        draft.Body.Should().Contain("- App version: 1.19.6")
            .And.Contain("- Windows: Microsoft Windows 10.0.22621")
            .And.Contain("- .NET: .NET 8.0.10")
            .And.Contain("- Server version: 2.1.0")
            .And.Contain("- API URL: http://localhost:5000")
            .And.Contain("- Install web URL: http://localhost:3000")
            .And.Contain(@"- Install server dir: C:\hq\server");
    }

    [Fact]
    public void Diagnostics_AreOmittedWhenUserUnticked()
    {
        var snapshot = Snapshot(crashLog: "SECRET-LOG-CONTENT", docker: "docker-output", exception: "exception-text");

        var draft = Build("t", "d", snapshot, diagnostics: false);

        draft.Body.Should().NotContain("## Diagnostics")
            .And.NotContain("SECRET-LOG-CONTENT")
            .And.NotContain("docker-output")
            .And.NotContain("exception-text");
        draft.Body.Should().Contain("## Environment", because: "environment is always sent");
    }

    [Fact]
    public void Diagnostics_AreWrappedInFencedBlocksWithHeadings()
    {
        var snapshot = Snapshot(crashLog: "log a\nlog b", docker: "NAME STATUS", exception: "System.Exception: x");

        var draft = Build("t", "d", snapshot);

        draft.Body.Should().Contain("### Exception\n\n```\nSystem.Exception: x\n```");
        draft.Body.Should().Contain("### crash.log (last 200 lines)\n\n```\nlog a\nlog b\n```");
        draft.Body.Should().Contain("### docker compose ps\n\n```\nNAME STATUS\n```");
    }

    [Fact]
    public void Diagnostics_WithNothingAvailable_SaysSo()
    {
        var draft = Build("t", "d", Snapshot());

        draft.Body.Should().Contain("## Diagnostics").And.Contain("Nothing to attach");
    }

    [Fact]
    public void LogContainingBackticks_GetsLongerFence()
    {
        var draft = Build("t", "d", Snapshot(crashLog: "before\n```\ninside\n```\nafter"));

        draft.Body.Should().Contain("````\nbefore\n```\ninside\n```\nafter\n````");
    }

    [Fact]
    public void UserTitle_IsUsedAsIs()
    {
        Build("  Кнопка   не работает ", "d", Snapshot()).Title.Should().Be("Кнопка не работает");
    }

    [Fact]
    public void CrashWithoutTitle_UsesTypeAndFirst80CharsOfMessage()
    {
        var crash = new InvalidOperationException(new string('x', 200));

        var draft = Build("", "", Snapshot(), crash: crash);

        draft.Title.Should().Be("Crash: InvalidOperationException: " + new string('x', 80));
    }

    [Fact]
    public void CrashTitle_CollapsesNewlines()
    {
        var crash = new ArgumentException("first line\nsecond line");

        BugReportDraftBuilder.BuildCrashTitle(crash).Should().Be("Crash: ArgumentException: first line second line");
    }

    [Fact]
    public void EmptyTitle_FallsBackToFirstLineOfDescription()
    {
        var draft = Build("", "\n  Не печатается акт\nвторая строка", Snapshot());

        draft.Title.Should().Be("Не печатается акт");
    }

    [Fact]
    public void EmptyTitleAndDescription_GetsGenericTitle()
    {
        Build("", "", Snapshot()).Title.Should().Be("Report from HQStudio app");
    }

    [Fact]
    public void EmptyDescription_IsMarked()
    {
        Build("t", "  ", Snapshot()).Body.Should().Contain("_(no description)_");
    }

    [Fact]
    public void VeryLongTitle_IsClipped()
    {
        Build(new string('a', 1000), "d", Snapshot()).Title.Length.Should().Be(BugReportDraftBuilder.MaxTitleLength);
    }

    [Fact]
    public void Secrets_AreRemovedFromTitleDescriptionAndLogs()
    {
        var snapshot = Snapshot(
            crashLog: "login failed Password=hunter2; token=abc123\nBearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl",
            docker: "POSTGRES_PASSWORD=dbsecret",
            exception: @"at X in C:\Users\Ivan Petrov\src\X.cs:line 5",
            install: new InstallInfo(@"C:\Users\Ivan Petrov\hq", null, null));

        var draft = Build("password=titlesecret", "мой пароль: password: descsecret", snapshot);

        draft.Title.Should().NotContain("titlesecret");
        draft.Body.Should().NotContainAny("hunter2", "abc123", "eyJhbGci", "dbsecret", "descsecret", "Ivan Petrov");
        draft.Body.Should().Contain("%USERPROFILE%");
    }

    [Fact]
    public void BodyOfHugeInput_StaysBelowGitHubLimit()
    {
        var huge = new string('x', 200_000);

        var draft = Build("t", huge, Snapshot(crashLog: huge));

        draft.Body.Length.Should().BeLessThan(65536);
    }
}
