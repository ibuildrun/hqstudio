using System.Diagnostics;
using HQStudio.Setup.Core;

namespace HQStudio.Setup.Services.Real;

/// <summary>Talks to Docker through its command line, always with hidden windows.</summary>
public sealed class DockerClient : IDockerClient
{
    private readonly IProcessRunner _runner;
    private readonly Func<string, bool> _fileExists;
    private readonly string? _pathVariable;
    private readonly string _programFiles;
    private readonly string _localAppData;

    public DockerClient(
        IProcessRunner runner,
        Func<string, bool>? fileExists = null,
        string? pathVariable = null,
        string? programFiles = null,
        string? localAppData = null)
    {
        _runner = runner;
        _fileExists = fileExists ?? File.Exists;
        _pathVariable = pathVariable ?? Environment.GetEnvironmentVariable("PATH");
        _programFiles = programFiles ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        _localAppData = localAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    public string? FindDocker()
    {
        foreach (var dir in (_pathVariable ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = SafeCombine(dir.Trim().Trim('"'), "docker.exe");
            if (candidate != null && _fileExists(candidate))
                return candidate;
        }

        // A freshly installed Docker is missing from the PATH of this already running process.
        foreach (var candidate in new[]
                 {
                     Path.Combine(_programFiles, "Docker", "Docker", "resources", "bin", "docker.exe"),
                     Path.Combine(_localAppData, "Programs", "Docker", "Docker", "resources", "bin", "docker.exe")
                 })
        {
            if (_fileExists(candidate))
                return candidate;
        }
        return null;
    }

    public async Task<DockerStatus> GetStatusAsync(CancellationToken ct)
    {
        var docker = FindDocker();
        if (docker == null)
            return DockerStatus.NotInstalled;

        var result = await _runner.RunAsync(
            Request(docker, new[] { "info", "--format", "{{.ServerVersion}}" }, TimeSpan.FromSeconds(20)), null, ct).ConfigureAwait(false);
        return result.ExitCode == 0 && !result.TimedOut ? DockerStatus.Running : DockerStatus.InstalledNotRunning;
    }

    public bool TryStartDesktop()
    {
        foreach (var path in new[]
                 {
                     Path.Combine(_programFiles, "Docker", "Docker", "Docker Desktop.exe"),
                     Path.Combine(_localAppData, "Programs", "Docker", "Docker", "Docker Desktop.exe")
                 })
        {
            if (!_fileExists(path))
                continue;
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Minimized });
                return true;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                return false;
            }
        }
        return false;
    }

    public async Task<CommandResult> ComposeAsync(
        string serverDir,
        bool tunnel,
        IReadOnlyList<string> command,
        Action<string>? onLine,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        var docker = FindDocker();
        if (docker == null)
            return new CommandResult(-1, "docker.exe not found");

        var args = ComposeArgs.Build(serverDir, tunnel, command.ToArray());
        var result = await _runner.RunAsync(Request(docker, args, timeout), onLine, ct).ConfigureAwait(false);
        return new CommandResult(result.TimedOut ? -1 : result.ExitCode, result.Output);
    }

    private static ProcessRequest Request(string docker, IReadOnlyList<string> args, TimeSpan? timeout)
    {
        // docker-credential-desktop lives next to docker.exe and must be reachable even when this process has a stale PATH.
        var binDir = Path.GetDirectoryName(docker) ?? "";
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var env = new Dictionary<string, string> { ["PATH"] = binDir + ";" + path };
        return new ProcessRequest(docker, args, Timeout: timeout, Environment: env);
    }

    private static string? SafeCombine(string dir, string file)
    {
        try { return Path.Combine(dir, file); }
        catch (ArgumentException) { return null; }
    }
}
