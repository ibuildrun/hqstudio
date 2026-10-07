using System.IO.Compression;
using System.Text;
using HQStudio.Setup.Core;

namespace HQStudio.Setup.Services.Sim;

/// <summary>Shared pretend state of the machine for --simulate: what Docker looks like right now.</summary>
public sealed class SimWorld
{
    public SimWorld(SimDockerMode mode) => DockerMode = mode;

    public SimDockerMode DockerMode { get; set; }
    public bool DesktopStarted { get; set; }
    public int PollsSinceStart { get; set; }
}

public sealed class SimDockerClient : IDockerClient
{
    private readonly SimWorld _world;
    private readonly InstallPaths _paths;

    public SimDockerClient(SimWorld world, InstallPaths paths)
    {
        _world = world;
        _paths = paths;
    }

    public async Task<DockerStatus> GetStatusAsync(CancellationToken ct)
    {
        await Task.Delay(150, ct);
        switch (_world.DockerMode)
        {
            case SimDockerMode.Missing:
                return DockerStatus.NotInstalled;
            case SimDockerMode.Stopped:
                if (_world.DesktopStarted && ++_world.PollsSinceStart >= 3)
                {
                    _world.DockerMode = SimDockerMode.Running;
                    return DockerStatus.Running;
                }
                return DockerStatus.InstalledNotRunning;
            default:
                return DockerStatus.Running;
        }
    }

    public bool TryStartDesktop()
    {
        _world.DesktopStarted = true;
        _world.PollsSinceStart = 0;
        return true;
    }

    public async Task<CommandResult> ComposeAsync(
        string serverDir,
        bool tunnel,
        IReadOnlyList<string> command,
        Action<string>? onLine,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        var verb = command.Count > 0 ? command[0] : "";
        var output = new StringBuilder();
        void Emit(string line)
        {
            output.AppendLine(line);
            onLine?.Invoke(line);
        }

        switch (verb)
        {
            case "pull":
                await FakePullAsync(Emit, tunnel, ct);
                break;
            case "up":
                await FakeUpAsync(Emit, tunnel, ct);
                break;
            case "logs":
                var url = FakeTunnelUrl();
                await Task.Delay(400, ct);
                Emit($"tuna-1  | Forwarding {url} -> proxy:80");
                break;
            default:
                await Task.Delay(200, ct);
                break;
        }
        return new CommandResult(0, output.ToString());
    }

    private string FakeTunnelUrl()
    {
        var env = EnvFile.ReadAllTextOrNull(_paths.EnvFile);
        var sub = env == null ? null : EnvFile.Get(env, "TUNA_SUBDOMAIN");
        return $"https://{(string.IsNullOrWhiteSpace(sub) ? "hq-demo" : sub)}.ru.tuna.am";
    }

    private static async Task FakePullAsync(Action<string> emit, bool tunnel, CancellationToken ct)
    {
        var images = new List<(string Name, int Layers)>
        {
            ("postgres:16-alpine", 6),
            ("ghcr.io/ibuildrun/hqstudio/api:latest", 8),
            ("ghcr.io/ibuildrun/hqstudio/web:latest", 7),
            ("nginx:1.27-alpine", 5)
        };
        if (tunnel)
            images.Add(("yuccastream/tuna:latest", 3));

        var random = new Random(42);
        foreach (var (name, _) in images)
            emit($" Image {name} Pulling ");

        // Like compose, announce every layer of every image first, then download them.
        var plan = images
            .Select(i => (i.Name, Ids: Enumerable.Range(0, i.Layers).Select(_ => RandomId(random)).ToList()))
            .ToList();
        foreach (var (_, ids) in plan)
        {
            foreach (var id in ids)
                emit($" {id} Pulling fs layer ");
        }

        foreach (var (name, ids) in plan)
        {
            foreach (var id in ids)
            {
                var total = 2.0 + random.NextDouble() * 38.0;
                for (var step = 1; step <= 5; step++)
                {
                    await Task.Delay(70, ct);
                    emit($" {id} Downloading [{new string('=', step * 8)}>{new string(' ', 40 - step * 8)}]  {total * step / 5:0.0}MB/{total:0.0}MB");
                }
                emit($" {id} Download complete ");
                await Task.Delay(40, ct);
                emit($" {id} Extracting [==================================================>]  {total:0.0}MB/{total:0.0}MB");
                emit($" {id} Pull complete ");
            }
            emit($" Image {name} Pulled ");
        }
    }

    private static async Task FakeUpAsync(Action<string> emit, bool tunnel, CancellationToken ct)
    {
        var services = new List<string> { "db", "api", "web", "proxy" };
        if (tunnel)
            services.Add("tuna");

        foreach (var service in services)
        {
            emit($" Container hqstudio-{service}-1  Creating");
            await Task.Delay(350, ct);
            emit($" Container hqstudio-{service}-1  Started");
        }
    }

    private static string RandomId(Random random)
    {
        var bytes = new byte[6];
        random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed class SimDockerInstaller : IDockerInstaller
{
    private readonly SimWorld _world;

    public SimDockerInstaller(SimWorld world) => _world = world;

    public async Task<string> DownloadAsync(IProgress<DownloadProgress> progress, CancellationToken ct)
    {
        const long total = 612_000_000;
        for (var step = 1; step <= 40; step++)
        {
            await Task.Delay(150, ct);
            progress.Report(new DownloadProgress(total * step / 40, total));
        }
        return "Docker Desktop Installer.exe";
    }

    public async Task<DockerInstallResult> RunAsync(string installerPath, CancellationToken ct)
    {
        await Task.Delay(5000, ct);
        _world.DockerMode = SimDockerMode.Stopped;
        _world.DesktopStarted = false;
        return new DockerInstallResult(DockerInstallOutcome.Success, 0, "Docker установлен.");
    }
}

public sealed class SimHealthProbe : IHealthProbe
{
    private int _calls;

    public async Task<bool> IsHealthyAsync(int port, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        return Interlocked.Increment(ref _calls) > 3;
    }
}

public sealed class SimPortProbe : IPortProbe
{
    public bool IsFree(int port) => true;
}

public sealed class SimDelay : IDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken ct) => Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, 1500)), ct);
}

/// <summary>Writes tiny placeholder files inside the temp tree instead of real .lnk files.</summary>
public sealed class SimShortcutCreator : IShortcutCreator
{
    public void Create(ShortcutSpec spec)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(spec.Path)!);
        File.WriteAllText(spec.Path, $"simulated shortcut to {spec.Target}");
    }
}

public sealed class SimRegistry : IRegistry
{
    public UninstallEntry? Last { get; private set; }

    public void WriteUninstallEntry(UninstallEntry entry) => Last = entry;
}

/// <summary>Opens external pages for real (handy to check the links) but never launches, reboots or opens localhost.</summary>
public sealed class SimShellActions : IShellActions
{
    private readonly IShellActions _real;
    private readonly Action<string> _log;

    public SimShellActions(IShellActions real, Action<string> log)
    {
        _real = real;
        _log = log;
    }

    public void OpenUrl(string url)
    {
        _log($"[simulate] open {url}");
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsLoopback && !uri.Host.EndsWith(".tuna.am", StringComparison.OrdinalIgnoreCase))
            _real.OpenUrl(url);
    }

    public void Launch(string path, string? arguments = null) => _log($"[simulate] launch {path} {arguments}");

    public void RebootNow() => _log("[simulate] reboot requested (ignored)");
}

/// <summary>A small zip with the same layout as the real payload, so the real extractor is exercised.</summary>
public sealed class SimPayloadSource : IPayloadSource
{
    private readonly string _envExample;

    public SimPayloadSource(string? envExample = null) => _envExample = envExample ?? DefaultEnvExample;

    public string Description => "simulated payload";

    public Stream? Open()
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "app/HQStudio.exe", "simulated executable");
            Add(zip, "app/ИНСТРУКЦИЯ.html", "<html><body>simulated</body></html>");
            Add(zip, "server/docker-compose.yml", "name: hqstudio\nservices: {}\n");
            Add(zip, "server/.env.example", _envExample);
            Add(zip, "server/nginx/default.conf", "server { listen 80; }\n");
        }
        stream.Position = 0;
        return stream;
    }

    private static void Add(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    public const string DefaultEnvExample =
        "# Created automatically by the installer.\n" +
        "HQSTUDIO_VERSION=latest\n" +
        "HQ_PORT=8080\n" +
        "POSTGRES_PASSWORD=change-me\n" +
        "JWT_KEY=change-me-to-a-random-string-of-at-least-32-characters\n" +
        "ADMIN_PASSWORD=\n" +
        "ADMIN_NAME=\n" +
        "GEMINI_API_KEY=\n" +
        "TUNA_TOKEN=\n" +
        "TUNA_SUBDOMAIN=\n" +
        "PUBLIC_URL=\n";
}

/// <summary>Throws the failure a stage would realistically produce, once per stage.</summary>
public sealed class FailOnceInjector : IFailureInjector
{
    private readonly StageId _stage;
    private bool _fired;

    public FailOnceInjector(StageId stage) => _stage = stage;

    public void Check(StageId stage)
    {
        if (stage != _stage || _fired)
            return;
        _fired = true;

        throw stage switch
        {
            StageId.Prepare => new InstallException(FailureKind.FilesLocked, "HQStudio.exe is in use (simulated)"),
            StageId.Configure => new InstallException(FailureKind.DiskFull, "There is not enough space on the disk (simulated)"),
            StageId.DockerReady => new InstallException(FailureKind.DockerNotRunning, "Docker did not start in time (simulated)"),
            StageId.Pull => new InstallException(FailureKind.Network, "docker compose pull failed (simulated)",
                "Error response from daemon: Get \"https://ghcr.io/v2/\": dial tcp: lookup ghcr.io: no such host"),
            StageId.Start => new InstallException(FailureKind.PortBusy, "No free port left in the range (simulated)",
                "Error response from daemon: Ports are not available: exposing port TCP 127.0.0.1:8080: bind: address already in use"),
            StageId.Health => new InstallException(FailureKind.HealthTimeout, "/api/health did not answer (simulated)"),
            StageId.PublicUrl => new InstallException(FailureKind.TunnelFailed, "tuna did not report an address (simulated)"),
            _ => new InstallException(FailureKind.Unknown, "Simulated failure")
        };
    }
}
