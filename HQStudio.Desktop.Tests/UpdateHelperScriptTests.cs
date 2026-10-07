using System.Diagnostics;
using System.IO;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Runs the generated swap script for real, but only on throw-away copies of harmless Windows
/// executables inside a temp folder.
/// </summary>
public class UpdateHelperScriptTests
{
    private static readonly string System32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

    [Fact]
    public async Task Script_WaitsForProcessExit_ThenSwapsExe_AndCleansUp()
    {
        using var temp = new UpdateTempDir();
        var installDir = temp.Combine("Папка с пробелами и юникодом");
        Directory.CreateDirectory(installDir);
        var target = Path.Combine(installDir, "HQStudio.exe");
        var oldBytes = File.ReadAllBytes(Path.Combine(System32, "whoami.exe"));
        var newBytes = File.ReadAllBytes(Path.Combine(System32, "rundll32.exe"));
        File.WriteAllBytes(target, oldBytes);

        var zip = UpdateTestData.ZipBytes("HQStudio.exe", newBytes);
        var http = new UpdateFakeHttp { Respond = _ => UpdateFakeHttp.Bytes(zip) };
        string? script = null;

        // The "running app" is a short ping the script has to wait for.
        using var running = Process.Start(new ProcessStartInfo("ping.exe", "-n 4 127.0.0.1")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!;
        var pid = running.Id;

        var service = new AppUpdateService(new ReleaseDownloader(http), new AppUpdateOptions
        {
            CurrentExePath = target,
            CurrentProcessId = pid,
            TempRoot = temp.Combine("tmp"),
            LaunchHelper = s => script = s,
            ShutdownApplication = () => { }
        });
        var result = await service.UpdateAsync(UpdateTestData.Release("1.20.0", desktopZip: zip), null, null, default);
        result.Success.Should().BeTrue(result.Message);
        script.Should().NotBeNull();

        using var helper = Process.Start(new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", script! },
            CreateNoWindow = true,
            UseShellExecute = false
        })!;

        await Task.Delay(700);
        running.HasExited.Should().BeFalse("the ping is still running, so the script must be waiting");
        File.ReadAllBytes(target).Should().Equal(oldBytes, "nothing is replaced while the app process is alive");

        await helper.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);

        File.ReadAllBytes(target).Should().Equal(newBytes);
        File.Exists(target + ".bak").Should().BeFalse();
        File.Exists(script!).Should().BeFalse("the script deletes itself");
        Directory.Exists(Path.Combine(temp.Combine("tmp"), "app-1.20.0")).Should().BeFalse("staging is removed");
    }
}
