using System.IO.Compression;
using System.Text;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Sim;

namespace HQStudio.Setup.Tests;

/// <summary>A throw-away folder under the system temp directory.</summary>
public sealed class TempDir : IDisposable
{
    public TempDir() => Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hqsetup-tests", Guid.NewGuid().ToString("N"));

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine(new[] { Path }.Concat(parts).ToArray());

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

public sealed class InstantDelay : IDelay
{
    public int Calls { get; private set; }

    public Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        Calls++;
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed record ComposeCall(string Verb, bool Tunnel, IReadOnlyList<string> Command);

/// <summary>Scriptable docker: status sequence plus a handler that answers compose commands.</summary>
public sealed class FakeDocker : IDockerClient
{
    private readonly Queue<DockerStatus> _statuses = new();

    public DockerStatus Status { get; set; } = DockerStatus.Running;
    public int StatusCalls { get; private set; }
    public int DesktopStarts { get; private set; }
    public List<ComposeCall> Calls { get; } = new();

    /// <summary>Returns the result for a compose call; the default succeeds with some output per verb.</summary>
    public Func<ComposeCall, CommandResult>? Handler { get; set; }

    public void QueueStatuses(params DockerStatus[] statuses)
    {
        foreach (var s in statuses)
            _statuses.Enqueue(s);
    }

    public Task<DockerStatus> GetStatusAsync(CancellationToken ct)
    {
        StatusCalls++;
        return Task.FromResult(_statuses.Count > 0 ? _statuses.Dequeue() : Status);
    }

    public bool TryStartDesktop()
    {
        DesktopStarts++;
        return true;
    }

    public Task<CommandResult> ComposeAsync(string serverDir, bool tunnel, IReadOnlyList<string> command, Action<string>? onLine,
        CancellationToken ct, TimeSpan? timeout = null)
    {
        ct.ThrowIfCancellationRequested();
        var call = new ComposeCall(command[0], tunnel, command.ToList());
        Calls.Add(call);

        var result = Handler?.Invoke(call) ?? DefaultResult(call);
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            onLine?.Invoke(line.TrimEnd('\r'));
        return Task.FromResult(result);
    }

    public int CountOf(string verb) => Calls.Count(c => c.Verb == verb);

    private static CommandResult DefaultResult(ComposeCall call) => call.Verb switch
    {
        "pull" => new CommandResult(0, " Image postgres:16-alpine Pulling\n 3c6d4a1b9e2f Pulling fs layer\n 3c6d4a1b9e2f Pull complete\n Image postgres:16-alpine Pulled\n"),
        "logs" => new CommandResult(0, "tuna-1  | Forwarding https://mystudio.ru.tuna.am -> proxy:80\n"),
        _ => new CommandResult(0, "")
    };
}

public sealed class FakeHealth : IHealthProbe
{
    public List<int> Probes { get; } = new();
    public Func<int, int, bool> Responder { get; set; } = (_, _) => true;

    public Task<bool> IsHealthyAsync(int port, CancellationToken ct)
    {
        Probes.Add(port);
        return Task.FromResult(Responder(port, Probes.Count));
    }
}

public sealed class FakePorts : IPortProbe
{
    public HashSet<int> Busy { get; } = new();

    public bool IsFree(int port) => !Busy.Contains(port);
}

public sealed class FakeShortcuts : IShortcutCreator
{
    public List<ShortcutSpec> Created { get; } = new();

    public void Create(ShortcutSpec spec) => Created.Add(spec);
}

public sealed class FakeRegistry : IRegistry
{
    public List<UninstallEntry> Entries { get; } = new();

    public void WriteUninstallEntry(UninstallEntry entry) => Entries.Add(entry);
}

public sealed class FakeShell : IShellActions
{
    public List<string> Opened { get; } = new();
    public List<string> Launched { get; } = new();
    public int Reboots { get; private set; }

    public void OpenUrl(string url) => Opened.Add(url);

    public void Launch(string path, string? arguments = null) => Launched.Add(path);

    public void RebootNow() => Reboots++;
}

public sealed class FakeInstaller : IDockerInstaller
{
    public DockerInstallResult Result { get; set; } = new(DockerInstallOutcome.Success, 0, "ok");
    public Exception? DownloadFailure { get; set; }
    public int Downloads { get; private set; }
    public int Runs { get; private set; }

    public Task<string> DownloadAsync(IProgress<DownloadProgress> progress, CancellationToken ct)
    {
        Downloads++;
        if (DownloadFailure != null)
            throw DownloadFailure;
        progress.Report(new DownloadProgress(100_000_000, 400_000_000));
        progress.Report(new DownloadProgress(400_000_000, 400_000_000));
        return Task.FromResult("installer.exe");
    }

    public Task<DockerInstallResult> RunAsync(string installerPath, CancellationToken ct)
    {
        Runs++;
        return Task.FromResult(Result);
    }
}

public sealed class FakePayload : IPayloadSource
{
    private readonly byte[]? _bytes;

    public FakePayload(byte[]? bytes) => _bytes = bytes;

    public int Opens { get; private set; }
    public string Description => "fake payload";

    public Stream? Open()
    {
        Opens++;
        return _bytes == null ? null : new MemoryStream(_bytes);
    }

    public static byte[] Zip(IEnumerable<(string Name, string Content)> entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = zip.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }
        return stream.ToArray();
    }

    public static byte[] StandardZip() => Zip(new[]
    {
        ("app/HQStudio.exe", "exe"),
        ("app/ИНСТРУКЦИЯ.html", "<html/>"),
        ("server/docker-compose.yml", "name: hqstudio"),
        ("server/.env.example", SimPayloadSource.DefaultEnvExample),
        ("server/nginx/default.conf", "server {}")
    });
}

/// <summary>Everything the installer needs, wired to fakes and a temp tree.</summary>
public sealed class Rig : IDisposable
{
    public Rig(byte[]? payload = null, string? version = null)
    {
        Temp = new TempDir();
        Paths = InstallPaths.Under(Temp.Path);
        Docker = new FakeDocker();
        Health = new FakeHealth();
        Ports = new FakePorts();
        Shortcuts = new FakeShortcuts();
        Registry = new FakeRegistry();
        Shell = new FakeShell();
        Installer = new FakeInstaller();
        Payload = new FakePayload(payload ?? FakePayload.StandardZip());
        Delay = new InstantDelay();
        Version = version ?? "1.20.0";
    }

    public TempDir Temp { get; }
    public InstallPaths Paths { get; }
    public FakeDocker Docker { get; }
    public FakeHealth Health { get; }
    public FakePorts Ports { get; }
    public FakeShortcuts Shortcuts { get; }
    public FakeRegistry Registry { get; }
    public FakeShell Shell { get; }
    public FakeInstaller Installer { get; }
    public FakePayload Payload { get; }
    public InstantDelay Delay { get; }
    public string Version { get; }
    public IFailureInjector? Injector { get; set; }

    public SetupServices Services() => new()
    {
        Paths = Paths,
        Docker = Docker,
        DockerInstaller = Installer,
        Health = Health,
        Ports = Ports,
        Shortcuts = Shortcuts,
        Registry = Registry,
        Shell = Shell,
        Payload = Payload,
        Delay = Delay,
        FailureInjector = Injector,
        Version = Version,
        IsSimulation = true
    };

    public static InstallAnswers Answers(bool tuna = false, bool skipSite = false, bool desktopShortcut = true) => new()
    {
        FirstName = "Иван",
        LastName = "Петров",
        Password = "Sunny-Day-2026",
        GeminiKey = "AIzaSyTestKey123456",
        TunaToken = tuna ? "tuna-secret-token-777" : "",
        TunaSubdomain = tuna ? "MyStudio" : "",
        SkipSite = skipSite,
        DesktopShortcut = desktopShortcut
    };

    public InstallContext Context(InstallAnswers answers, SetupLog? log = null) =>
        new(answers, Services(), log ?? new SetupLog(null));

    public void Dispose() => Temp.Dispose();
}

/// <summary>Counts how many times each stage was started, to prove that a retry skips finished stages.</summary>
public sealed class CountingStage : IInstallStage
{
    private readonly IInstallStage _inner;
    private readonly Dictionary<StageId, int> _counts;

    public CountingStage(IInstallStage inner, Dictionary<StageId, int> counts)
    {
        _inner = inner;
        _counts = counts;
    }

    public StageId Id => _inner.Id;
    public string Title => _inner.Title;
    public double Weight => _inner.Weight;

    public bool IsApplicable(InstallContext context) => _inner.IsApplicable(context);

    public Task<StageOutcome> RunAsync(InstallContext context, IStageReporter reporter, CancellationToken ct)
    {
        _counts[Id] = _counts.GetValueOrDefault(Id) + 1;
        return _inner.RunAsync(context, reporter, ct);
    }
}
