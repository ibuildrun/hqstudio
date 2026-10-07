using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateAppUpdateServiceTests
{
    private sealed class Fixture : IDisposable
    {
        public UpdateTempDir Temp { get; } = new();
        public string InstallDir { get; }
        public string ExePath { get; }
        public byte[] OldExe { get; } = UpdateTestData.FakeExe('O');
        public byte[] NewExe { get; } = UpdateTestData.FakeExe('N');
        public byte[] DesktopZip { get; }
        public UpdateFakeHttp Http { get; } = new();
        public List<string> LaunchedScripts { get; } = new();
        public int ShutdownCalls { get; private set; }
        public UpdateProgressRecorder Progress { get; } = new();
        public List<string> Log { get; } = new();

        public Fixture(string? zipEntryName = "HQStudio.exe")
        {
            InstallDir = Temp.Combine("app");
            Directory.CreateDirectory(InstallDir);
            ExePath = Path.Combine(InstallDir, "HQStudio.exe");
            File.WriteAllBytes(ExePath, OldExe);
            DesktopZip = zipEntryName == null
                ? UpdateTestData.Zip(("readme.txt", "no exe here"))
                : UpdateTestData.ZipBytes(zipEntryName, NewExe);
            Http.Respond = _ => UpdateFakeHttp.Bytes(DesktopZip);
        }

        public AppUpdateService Service(string? exePath = null) => new(new ReleaseDownloader(Http), new AppUpdateOptions
        {
            CurrentExePath = exePath ?? ExePath,
            CurrentProcessId = 4242,
            TempRoot = Temp.Combine("tmp"),
            LaunchHelper = LaunchedScripts.Add,
            ShutdownApplication = () => ShutdownCalls++
        });

        public ReleaseInfo Release(bool withDigest = true, string? badDigest = null) =>
            UpdateTestData.Release("1.20.0", desktopZip: DesktopZip, withDigest: withDigest, badDigest: badDigest);

        public Task<UpdateOperationResult> RunAsync(ReleaseInfo release, string? exePath = null) =>
            Service(exePath).UpdateAsync(release, Progress, Log.Add, CancellationToken.None);

        public void Dispose() => Temp.Dispose();
    }

    [Fact]
    public async Task HappyPath_ExtractsExe_WritesHelper_AndShutsDown_WithoutTouchingInstalledExe()
    {
        using var f = new Fixture();

        var result = await f.RunAsync(f.Release());

        result.Success.Should().BeTrue(result.Message);
        result.RestartPending.Should().BeTrue();
        f.ShutdownCalls.Should().Be(1);
        f.LaunchedScripts.Should().ContainSingle();
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe, "the swap happens in the helper after the app exits");

        var staged = Path.Combine(f.Temp.Combine("tmp"), "app-1.20.0", "HQStudio.exe");
        File.ReadAllBytes(staged).Should().Equal(f.NewExe);
    }

    [Fact]
    public async Task HappyPath_HelperScript_WaitsForPid_Copies_AndRelaunches()
    {
        using var f = new Fixture();

        await f.RunAsync(f.Release());

        var script = File.ReadAllText(f.LaunchedScripts.Single());
        script.Should().Contain("APPPID=4242");
        script.Should().Contain($"set \"TARGET={f.ExePath}\"");
        script.Should().Contain("HQStudio.exe\"").And.Contain("tasklist.exe\" /FI \"PID eq %APPPID%\"");
        script.Should().Contain("%SystemRoot%\\System32\\find.exe", "a find.exe from PATH (Git, MSYS) must not be used");
        script.IndexOf("tasklist", StringComparison.Ordinal).Should().BeLessThan(script.IndexOf("copy /Y \"%NEWEXE%\"", StringComparison.Ordinal));
        script.IndexOf("copy /Y \"%NEWEXE%\"", StringComparison.Ordinal).Should().BeLessThan(script.IndexOf("start \"\"", StringComparison.Ordinal));
        script.Should().Contain("del \"%~f0\"", "the helper deletes itself");
        script.Should().Contain("%TARGET%.bak", "the old exe is kept until the new one is in place");
    }

    [Fact]
    public async Task HappyPath_ReportsMonotonicProgressToHundred()
    {
        using var f = new Fixture();

        await f.RunAsync(f.Release());

        var percents = f.Progress.Items.Select(p => p.Percent).ToList();
        percents.Should().BeInAscendingOrder();
        percents.Last().Should().Be(100);
        f.Progress.Items.Select(p => p.Stage).Should().Contain("Скачивание новой версии");
    }

    [Fact]
    public async Task DigestMismatch_LeavesExeUntouched_NoHelper_NoShutdown()
    {
        using var f = new Fixture();

        var result = await f.RunAsync(f.Release(badDigest: new string('f', 64)));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("повреждён").And.Contain("не затронута");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.LaunchedScripts.Should().BeEmpty();
        f.ShutdownCalls.Should().Be(0);
        Directory.Exists(Path.Combine(f.Temp.Combine("tmp"), "app-1.20.0")).Should().BeFalse("staging is cleaned up");
    }

    [Fact]
    public async Task CorruptZip_LeavesExeUntouched()
    {
        using var f = new Fixture();
        f.Http.Respond = _ => UpdateFakeHttp.Bytes(new byte[] { 9, 9, 9, 9, 9, 9 });

        var result = await f.RunAsync(f.Release(withDigest: false));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("повреждён");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.LaunchedScripts.Should().BeEmpty();
        f.ShutdownCalls.Should().Be(0);
    }

    [Fact]
    public async Task ZipWithoutExe_Fails()
    {
        using var f = new Fixture(zipEntryName: null);

        var result = await f.RunAsync(f.Release());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("HQStudio.exe");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.ShutdownCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExtractedFileThatIsNotAnExecutable_IsRejected()
    {
        using var f = new Fixture();
        var notExe = UpdateTestData.ZipBytes("HQStudio.exe", new byte[5000]);
        f.Http.Respond = _ => UpdateFakeHttp.Bytes(notExe);

        var result = await f.RunAsync(UpdateTestData.Release("1.20.0", desktopZip: notExe));

        result.Success.Should().BeFalse();
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.LaunchedScripts.Should().BeEmpty();
    }

    [Fact]
    public async Task ExeInsideSubfolder_IsFound()
    {
        using var f = new Fixture(zipEntryName: "HQStudio/HQStudio.exe");

        var result = await f.RunAsync(f.Release());

        result.Success.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task HttpFailure_LeavesExeUntouched()
    {
        using var f = new Fixture();
        f.Http.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await f.RunAsync(f.Release());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("не найден");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.ShutdownCalls.Should().Be(0);
    }

    [Fact]
    public async Task NetworkFailure_LeavesExeUntouched()
    {
        using var f = new Fixture();
        f.Http.Respond = _ => throw new HttpRequestException("offline");

        var result = await f.RunAsync(f.Release());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("интернету");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
    }

    [Fact]
    public async Task ReleaseWithoutDesktopAsset_Fails()
    {
        using var f = new Fixture();

        var result = await f.RunAsync(UpdateTestData.Release("1.20.0"));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("нет файла приложения");
        f.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunningUnderDotnetHost_IsRefusedBeforeDownloading()
    {
        using var f = new Fixture();
        var dotnet = Path.Combine(f.InstallDir, "dotnet.exe");
        File.WriteAllBytes(dotnet, f.OldExe);

        var result = await f.RunAsync(f.Release(), dotnet);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("среды разработки");
        f.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingExePath_IsRefused()
    {
        using var f = new Fixture();

        var result = await f.RunAsync(f.Release(), Path.Combine(f.InstallDir, "missing.exe"));

        result.Success.Should().BeFalse();
        f.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancellation_LeavesExeUntouched()
    {
        using var f = new Fixture();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await f.Service().UpdateAsync(f.Release(), f.Progress, f.Log.Add, cts.Token);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("отменено");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
        f.ShutdownCalls.Should().Be(0);
    }
}
