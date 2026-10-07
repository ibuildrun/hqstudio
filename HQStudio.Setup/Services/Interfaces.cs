using HQStudio.Setup.Core;

namespace HQStudio.Setup.Services;

public enum DockerStatus
{
    NotInstalled,
    InstalledNotRunning,
    Running
}

public sealed record ProcessRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null,
    IReadOnlyDictionary<string, string>? Environment = null);

public sealed record ProcessResult(int ExitCode, string Output, bool TimedOut = false);

/// <summary>Starts a hidden process, streams its output line by line and returns the merged output.</summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, Action<string>? onLine, CancellationToken ct);
}

public sealed record CommandResult(int ExitCode, string Output)
{
    public bool Success => ExitCode == 0;
}

public interface IDockerClient
{
    Task<DockerStatus> GetStatusAsync(CancellationToken ct);

    /// <summary>Launches Docker Desktop without waiting for it. False when it cannot be found.</summary>
    bool TryStartDesktop();

    Task<CommandResult> ComposeAsync(
        string serverDir,
        bool tunnel,
        IReadOnlyList<string> command,
        Action<string>? onLine,
        CancellationToken ct,
        TimeSpan? timeout = null);
}

public sealed record DownloadProgress(long Received, long? Total)
{
    public double? Fraction => Total is > 0 ? Math.Clamp((double)Received / Total.Value, 0, 1) : null;
}

public enum DockerInstallOutcome
{
    Success,
    RebootRequired,
    Declined,
    Failed
}

public sealed record DockerInstallResult(DockerInstallOutcome Outcome, int ExitCode, string Message);

public interface IDockerInstaller
{
    /// <summary>Downloads the Docker Desktop installer and returns the local file path.</summary>
    Task<string> DownloadAsync(IProgress<DownloadProgress> progress, CancellationToken ct);

    /// <summary>Runs the downloaded installer elevated and silent; completes when it exits.</summary>
    Task<DockerInstallResult> RunAsync(string installerPath, CancellationToken ct);
}

public interface IHealthProbe
{
    Task<bool> IsHealthyAsync(int port, CancellationToken ct);
}

public interface IPortProbe
{
    bool IsFree(int port);
}

public sealed record ShortcutSpec(
    string Path,
    string Target,
    string? Arguments,
    string? WorkingDirectory,
    string? IconPath,
    string? Description);

public interface IShortcutCreator
{
    void Create(ShortcutSpec spec);
}

public sealed record UninstallEntry(
    string DisplayName,
    string DisplayVersion,
    string Publisher,
    string InstallLocation,
    string DisplayIcon,
    string UninstallString);

public interface IRegistry
{
    void WriteUninstallEntry(UninstallEntry entry);
}

public interface IShellActions
{
    void OpenUrl(string url);
    void Launch(string path, string? arguments = null);
    void RebootNow();
}

public interface IPayloadSource
{
    string Description { get; }

    /// <summary>The payload.zip stream, or null when this build has none.</summary>
    Stream? Open();
}

public interface IDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken ct);
}

/// <summary>--simulate-fail support: throws the failure a stage would produce, once.</summary>
public interface IFailureInjector
{
    void Check(StageId stage);
}

public sealed class SetupServices
{
    public required InstallPaths Paths { get; init; }
    public required IDockerClient Docker { get; init; }
    public required IDockerInstaller DockerInstaller { get; init; }
    public required IHealthProbe Health { get; init; }
    public required IPortProbe Ports { get; init; }
    public required IShortcutCreator Shortcuts { get; init; }
    public required IRegistry Registry { get; init; }
    public required IShellActions Shell { get; init; }
    public required IPayloadSource Payload { get; init; }
    public required IDelay Delay { get; init; }
    public IFailureInjector? FailureInjector { get; init; }

    /// <summary>Installer version (also the tag of the site images; "latest" for dev builds).</summary>
    public required string Version { get; init; }

    public bool IsSimulation { get; init; }
}
