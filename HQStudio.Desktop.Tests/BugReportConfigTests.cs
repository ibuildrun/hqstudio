using System.IO;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportConfigTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private string WriteSettings(string json)
    {
        var path = _dir.File("appsettings.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void DefaultClientId_IsEmptyUntilConfigured()
    {
        BugReportConfig.DefaultClientId.Should().BeEmpty();
    }

    [Fact]
    public void EnvironmentVariable_WinsOverAppSettings()
    {
        var path = WriteSettings("{\"GitHubClientId\":\"from-file\"}");

        BugReportConfig.ResolveClientId("  from-env  ", path).Should().Be("from-env");
    }

    [Fact]
    public void AppSettings_UsedWhenNoEnvironmentVariable()
    {
        var path = WriteSettings("{\"ApiUrl\":\"http://x\",\"GitHubClientId\":\" from-file \"}");

        BugReportConfig.ResolveClientId(null, path).Should().Be("from-file");
        BugReportConfig.ResolveClientId("   ", path).Should().Be("from-file");
    }

    [Fact]
    public void FallsBackToDefault_WhenNothingIsConfigured()
    {
        BugReportConfig.ResolveClientId(null, _dir.File("missing.json")).Should().Be(BugReportConfig.DefaultClientId);
        BugReportConfig.ResolveClientId(null, WriteSettings("{\"ApiUrl\":\"x\"}")).Should().Be(BugReportConfig.DefaultClientId);
        BugReportConfig.ResolveClientId(null, WriteSettings("{\"GitHubClientId\":\"\"}")).Should().Be(BugReportConfig.DefaultClientId);
    }

    [Fact]
    public void CorruptAppSettings_DoesNotThrow()
    {
        BugReportConfig.ResolveClientId(null, WriteSettings("{ broken")).Should().Be(BugReportConfig.DefaultClientId);
        BugReportConfig.ResolveClientId(null, WriteSettings("[1,2,3]")).Should().Be(BugReportConfig.DefaultClientId);
        BugReportConfig.ResolveClientId(null, WriteSettings("{\"GitHubClientId\":42}")).Should().Be(BugReportConfig.DefaultClientId);
    }

    [Fact]
    public void AppSettings_AllowCommentsAndTrailingCommas()
    {
        var path = WriteSettings("{\n // comment\n \"GitHubClientId\": \"abc\",\n}");

        BugReportConfig.ResolveClientId(null, path).Should().Be("abc");
    }

    [Fact]
    public void Paths_LiveInLocalAppDataHQStudio()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio");

        BugReportConfig.TokenPath.Should().Be(Path.Combine(root, "github.token"));
        BugReportConfig.CrashLogPath.Should().Be(Path.Combine(root, "crash.log"));
        BugReportConfig.InstallInfoPath.Should().Be(Path.Combine(root, "install.json"));
    }

    [Fact]
    public void CrashPromptGuard_AllowsOnlyOneAtATime()
    {
        CrashPromptGuard.Leave();

        CrashPromptGuard.TryEnter().Should().BeTrue();
        CrashPromptGuard.TryEnter().Should().BeFalse();
        CrashPromptGuard.Leave();
        CrashPromptGuard.TryEnter().Should().BeTrue();
        CrashPromptGuard.Leave();
    }
}
