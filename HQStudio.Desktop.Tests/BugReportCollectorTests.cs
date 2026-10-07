using System.IO;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportCollectorTests : IDisposable
{
    private const string DataDir = @"C:\data\HQStudio";
    private const string ServerDir = @"C:\hq\server";

    private readonly FakeFileReader _files = new();
    private readonly FakeProcessRunner _process = new();
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private DiagnosticsCollector Create() => new(_process, _files, DataDir);

    private void AddInstallJson(string json = "{\"serverDir\":\"" + @"C:\\hq\\server" + "\",\"webUrl\":\"http://localhost:3000\",\"apiUrl\":\"http://localhost:5000\"}")
    {
        _files.Files[Path.Combine(DataDir, "install.json")] = json;
    }

    private void AddCrashLog(int lines)
    {
        _files.Files[Path.Combine(DataDir, "crash.log")] =
            string.Join("\n", Enumerable.Range(1, lines).Select(i => "log line " + i)) + "\n";
    }

    [Fact]
    public async Task EnvironmentInfo_IsAlwaysCollected()
    {
        var snapshot = await Create().CollectAsync(null, includeLogs: false, "http://localhost:5000", "2.1.0");

        snapshot.AppVersion.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
        snapshot.WindowsVersion.Should().NotBeNullOrWhiteSpace();
        snapshot.DotNetVersion.Should().Contain(".NET");
        snapshot.ApiUrl.Should().Be("http://localhost:5000");
        snapshot.ServerVersion.Should().Be("2.1.0");
    }

    [Fact]
    public async Task WithoutLogs_NoFilesAreReadAndNoProcessIsStarted()
    {
        AddInstallJson();
        AddCrashLog(10);

        var snapshot = await Create().CollectAsync(new InvalidOperationException("boom"), includeLogs: false, null, null);

        snapshot.CrashLogTail.Should().BeNull();
        snapshot.DockerComposePs.Should().BeNull();
        snapshot.ExceptionText.Should().BeNull();
        _process.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingInstallJson_SkipsDockerAndDoesNotFail()
    {
        AddCrashLog(3);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.Install.Should().BeNull();
        snapshot.DockerComposePs.Should().BeNull();
        snapshot.CrashLogTail.Should().Contain("log line 3");
        _process.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingCrashLog_LeavesTailEmpty()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => new ProcessResult(0, "NAME  STATUS\napi   running\n", "", false);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.CrashLogTail.Should().BeNull();
        snapshot.DockerComposePs.Should().Contain("api   running");
    }

    [Fact]
    public async Task CrashLog_ReturnsOnlyLast200Lines()
    {
        AddCrashLog(300);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        var lines = snapshot.CrashLogTail!.Split('\n');
        lines.Should().HaveCount(200);
        lines[0].Should().Be("log line 101");
        lines[^1].Should().Be("log line 300".TrimEnd());
    }

    [Fact]
    public async Task InstallJson_IsParsedAndDockerIsRunInServerDir()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => new ProcessResult(0, "api running", "", false);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.Install.Should().Be(new InstallInfo(ServerDir, "http://localhost:3000", "http://localhost:5000"));
        _process.Calls.Should().ContainSingle().Which.Should().Be(("docker", "compose ps", ServerDir));
        snapshot.DockerComposePs.Should().Be("api running");
    }

    [Fact]
    public async Task DockerNonZeroExit_IsReportedWithReason()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => new ProcessResult(1, "", "no configuration file provided", false);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.DockerComposePs.Should().Contain("failed").And.Contain("exit code 1").And.Contain("no configuration file provided");
    }

    [Fact]
    public async Task DockerNotInstalled_IsReportedWithoutThrowing()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => new ProcessResult(-1, "", "The system cannot find the file specified", false);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.DockerComposePs.Should().Contain("failed").And.Contain("cannot find the file");
    }

    [Fact]
    public async Task DockerTimeout_IsReported()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => new ProcessResult(-1, "", "timed out", true);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.DockerComposePs.Should().Contain("timed out");
    }

    [Fact]
    public async Task DockerRunnerThrowing_IsReportedWithoutThrowing()
    {
        AddInstallJson();
        _files.Directories.Add(ServerDir);
        _process.Result = () => throw new InvalidOperationException("runner exploded");

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.DockerComposePs.Should().Contain("failed").And.Contain("runner exploded");
    }

    [Fact]
    public async Task MissingServerDir_SkipsDockerWithNote()
    {
        AddInstallJson();

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.DockerComposePs.Should().Contain("not found");
        _process.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CorruptInstallJson_IsIgnored()
    {
        AddInstallJson("{ not json");
        AddCrashLog(2);

        var snapshot = await Create().CollectAsync(null, includeLogs: true, null, null);

        snapshot.Install.Should().BeNull();
        snapshot.CrashLogTail.Should().NotBeNull();
    }

    [Fact]
    public async Task CrashException_IsIncluded()
    {
        var crash = new InvalidOperationException("Sequence contains no elements");

        var snapshot = await Create().CollectAsync(crash, includeLogs: true, null, null);

        snapshot.ExceptionText.Should().Contain("InvalidOperationException").And.Contain("Sequence contains no elements");
    }

    [Theory]
    [InlineData("http://user:secret@localhost:5000", "http://localhost:5000/")]
    [InlineData("https://admin:p%40ss@host.example/api", "https://host.example/api")]
    [InlineData("http://localhost:5000", "http://localhost:5000")]
    [InlineData("  http://localhost:5000  ", "http://localhost:5000")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void StripCredentials_RemovesUserInfo(string? input, string? expected)
    {
        DiagnosticsCollector.StripCredentials(input).Should().Be(expected);
    }

    [Fact]
    public async Task ApiUrlWithCredentials_NeverAppearsInSnapshot()
    {
        var snapshot = await Create().CollectAsync(null, includeLogs: false, "http://user:secret@localhost:5000", null);

        snapshot.ApiUrl.Should().NotContain("secret").And.NotContain("user:");
    }

    [Fact]
    public void DiskFileReader_ReadLastLines_ReturnsTailOfRealFile()
    {
        var path = _temp.File("crash.log");
        File.WriteAllText(path, string.Join("\n", Enumerable.Range(1, 300).Select(i => "line " + i)) + "\n");

        var lines = new DiskFileReader().ReadLastLines(path, 200);

        lines.Should().HaveCount(200);
        lines[0].Should().Be("line 101");
        lines[^1].Should().Be("line 300");
    }

    [Fact]
    public void DiskFileReader_ReadLastLines_WorksWhileFileIsOpenForAppend()
    {
        var path = _temp.File("crash.log");
        using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("first\nsecond\n"));
        writer.Flush();

        var lines = new DiskFileReader().ReadLastLines(path, 10);

        lines.Should().Equal("first", "second");
    }

    [Fact]
    public void DiskFileReader_ReadLastLines_OfHugeFileStillReturnsTail()
    {
        var path = _temp.File("crash.log");
        using (var writer = new StreamWriter(path))
        {
            for (var i = 1; i <= 30000; i++) writer.WriteLine("a fairly long log line number " + i + " with padding to make the file big");
        }

        var lines = new DiskFileReader().ReadLastLines(path, 200);

        lines.Should().HaveCount(200);
        lines[^1].Should().StartWith("a fairly long log line number 30000 ");
    }
}
