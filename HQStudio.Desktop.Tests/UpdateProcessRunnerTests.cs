using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>Runs harmless built-in Windows commands (cmd, ping) only; never docker.</summary>
public class UpdateProcessRunnerTests
{
    [Fact]
    public async Task Run_StreamsLinesOfBothStreams_AndReturnsExitCode()
    {
        var lines = new List<string>();

        var result = await new ProcessRunner().RunAsync("cmd.exe",
            new[] { "/c", "echo first& echo second 1>&2& exit 3" }, null, l => { lock (lines) lines.Add(l); }, default);

        result.ExitCode.Should().Be(3);
        result.StandardOutput.Should().Contain("first");
        result.StandardError.Should().Contain("second");
        lines.Should().BeEquivalentTo("first", "second");
    }

    [Fact]
    public async Task Run_UsesWorkingDirectory()
    {
        using var temp = new UpdateTempDir();

        var result = await new ProcessRunner().RunAsync("cmd.exe", new[] { "/c", "cd" }, temp.Path, null, default);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Trim().Should().Be(temp.Path);
    }

    [Fact]
    public async Task Run_MissingExecutable_ThrowsWin32Exception()
    {
        var act = async () => await new ProcessRunner().RunAsync("definitely-not-docker-123.exe",
            new[] { "version" }, null, null, default);

        await act.Should().ThrowAsync<Win32Exception>();
    }

    [Fact]
    public async Task Run_Cancellation_KillsTheProcess()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var clock = Stopwatch.StartNew();

        var act = async () => await new ProcessRunner().RunAsync("ping.exe",
            new[] { "-n", "30", "127.0.0.1" }, null, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        clock.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DockerLocator_ReturnsSomethingRunnable()
    {
        DockerLocator.Resolve().Should().NotBeNullOrWhiteSpace();
    }
}
