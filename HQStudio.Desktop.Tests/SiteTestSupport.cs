using System.IO;
using System.ComponentModel;
using HQStudio.Services.Site;
using HQStudio.ViewModels;

namespace HQStudio.Desktop.Tests;

/// <summary>Файлы в памяти. Журнал операций позволяет проверять порядок действий.</summary>
internal sealed class SiteFakeFiles : ISiteFiles
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Log { get; } = new();
    public Func<string, bool>? FailWrite { get; set; }
    public Func<string, bool>? FailCopy { get; set; }
    public Dictionary<string, string[]> Listings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Общая лента событий (файлы, процессы, реестр) для проверки порядка действий.</summary>
    public List<string>? Timeline { get; set; }

    public SiteFakeFiles Add(string path, string content = "")
    {
        Files[path] = content;
        return this;
    }

    public bool FileExists(string path) => Files.ContainsKey(path);

    public bool DirectoryExists(string path) => Directories.Contains(path);

    public string ReadAllText(string path) => Files.TryGetValue(path, out var text)
        ? text
        : throw new FileNotFoundException(path);

    public void WriteAllTextAtomic(string path, string content)
    {
        if (FailWrite?.Invoke(path) == true)
            throw new IOException("write failed");
        Log.Add("write:" + Path.GetFileName(path));
        Timeline?.Add("write:" + Path.GetFileName(path));
        Files[path] = content;
    }

    public void DeleteFile(string path)
    {
        Log.Add("delete-file:" + path);
        Timeline?.Add("delete-file:" + Path.GetFileName(path));
        Files.Remove(path);
    }

    public void DeleteDirectory(string path)
    {
        Log.Add("delete-dir:" + path);
        Timeline?.Add("delete-dir:" + Path.GetFileName(path));
        Directories.Remove(path);
    }

    public void CopyFile(string source, string destination, bool overwrite)
    {
        if (FailCopy?.Invoke(destination) == true)
            throw new IOException("copy failed");
        Log.Add($"copy:{source}->{destination}");
        Timeline?.Add("copy:" + Path.GetFileName(source));
        Files[destination] = Files.TryGetValue(source, out var text) ? text : "";
    }

    public void CreateDirectory(string path) => Directories.Add(path);

    public IReadOnlyList<string> ListFiles(string directory) =>
        Listings.TryGetValue(directory, out var files) ? files : Array.Empty<string>();
}

/// <summary>Подставной запуск процессов: ответы задаются правилами, все вызовы записываются.</summary>
internal sealed class SiteFakeRunner : ISiteProcessRunner
{
    private readonly object _lock = new();
    public List<string> Calls { get; } = new();
    public List<(string File, string Args, string? WorkingDirectory)> Detached { get; } = new();
    public Action<string>? OnCall { get; set; }

    /// <summary>Правила проверяются по порядку; первое сработавшее даёт ответ. Без правил - успех.</summary>
    public List<(Func<string, bool> Match, Func<SiteProcessResult> Result)> Rules { get; } = new();

    public Exception? ThrowOnRun { get; set; }
    public Task? Gate { get; set; }

    public SiteFakeRunner When(string contains, int exit = 0, string output = "", string error = "")
    {
        Rules.Add((call => call.Contains(contains, StringComparison.Ordinal), () => new SiteProcessResult(exit, output, error)));
        return this;
    }

    public SiteFakeRunner When(Func<string, bool> match, Func<SiteProcessResult> result)
    {
        Rules.Add((match, result));
        return this;
    }

    public async Task<SiteProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, Action<string>? onLine,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var call = string.Join(' ', arguments);
        lock (_lock)
            Calls.Add(call);
        OnCall?.Invoke(call);

        if (ThrowOnRun != null)
            throw ThrowOnRun;
        if (Gate != null)
            await Gate;

        foreach (var (match, result) in Rules)
        {
            if (match(call))
            {
                var value = result();
                if (value.Output.Length > 0 && onLine != null)
                {
                    foreach (var line in value.Output.Split('\n'))
                        onLine(line.TrimEnd('\r'));
                }
                return value;
            }
        }
        return new SiteProcessResult(0, "", "");
    }

    public void StartDetached(string fileName, string arguments, string? workingDirectory)
    {
        lock (_lock)
            Detached.Add((fileName, arguments, workingDirectory));
        OnCall?.Invoke("detached:" + fileName);
    }

    /// <summary>Только команды compose: без префикса с путями и профилем.</summary>
    public IReadOnlyList<string> ComposeCommands(string serverDir)
    {
        var prefix = $"compose --project-directory {serverDir} -f {Path.Combine(serverDir, "docker-compose.yml")}";
        lock (_lock)
        {
            return Calls
                .Where(c => c.StartsWith(prefix, StringComparison.Ordinal))
                .Select(c => c[prefix.Length..].Trim())
                .Select(c => c.StartsWith("--profile tunnel", StringComparison.Ordinal) ? "[tunnel] " + c["--profile tunnel".Length..].Trim() : c)
                .ToList();
        }
    }
}

internal sealed class SiteFakeLocator : ISiteDockerLocator
{
    public string? Docker { get; set; } = @"C:\docker\docker.exe";
    public string? Desktop { get; set; } = @"C:\docker\Docker Desktop.exe";
    public string? FindDocker() => Docker;
    public string? FindDockerDesktop() => Desktop;
}

internal sealed class SiteFakeProbe : ISiteHealthProbe
{
    public Func<int, bool> Healthy { get; set; } = _ => true;
    public int Calls { get; private set; }

    public Task<bool> IsHealthyAsync(int port, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(Healthy(Calls));
    }
}

internal sealed class SiteFakeInstallStore : ISiteInstallStore
{
    public SiteInstallReadResult Result { get; set; } = new(
        new SiteInstallInfo(SiteTestEnv.ServerDir, "http://localhost:8080", "http://localhost:8080", "ibuildrun/hqstudio"), null);

    public SiteInstallReadResult Read() => Result;
}

internal sealed class SiteFakeShell : ISiteShell
{
    public List<string> Opened { get; } = new();
    public List<string> Copied { get; } = new();
    public bool OpenResult { get; set; } = true;
    public bool CopyResult { get; set; } = true;

    public bool OpenUrl(string url)
    {
        Opened.Add(url);
        return OpenResult;
    }

    public bool CopyText(string text)
    {
        Copied.Add(text);
        return CopyResult;
    }
}

internal sealed class SiteFakeNotifier : ISiteNotifier
{
    public List<string> Messages { get; } = new();
    public void Success(string message) => Messages.Add("success:" + message);
    public void Info(string message) => Messages.Add("info:" + message);
    public void Warning(string message) => Messages.Add("warning:" + message);
    public void Error(string message) => Messages.Add("error:" + message);
}

internal sealed class SiteFakeDialogs : ISiteDialogs
{
    public bool ConfirmResult { get; set; } = true;
    public int KeysShown { get; private set; }
    public int LogsShown { get; private set; }
    public int UpdatesShown { get; private set; }

    public void ShowKeys(ISiteService service) => KeysShown++;
    public void ShowLogs(ISiteService service) => LogsShown++;
    public void ShowUpdates() => UpdatesShown++;
    public bool ConfirmUninstall() => ConfirmResult;
}

internal sealed class SiteFakeUninstallHost : ISiteUninstallHost
{
    public SiteOperationResult Result { get; set; } = SiteOperationResult.Ok("");
    public int Launched { get; private set; }
    public int ShutDown { get; private set; }

    public SiteOperationResult LaunchUninstall()
    {
        Launched++;
        return Result;
    }

    public void ShutdownApplication() => ShutDown++;
}

internal sealed class SiteFakeRegistry : IUninstallRegistry
{
    public List<string> Deleted { get; } = new();
    public Action<string>? OnDelete { get; set; }

    public void DeleteUninstallKey(string keyName)
    {
        Deleted.Add(keyName);
        OnDelete?.Invoke(keyName);
    }
}

/// <summary>Управляемая служба для тестов моделей представления.</summary>
internal sealed class SiteFakeService : ISiteService
{
    public Func<SiteOperation, Task<SiteSnapshot>>? Refresh { get; set; }
    public SiteSnapshot Snapshot { get; set; } = SiteTestEnv.RunningSnapshot();
    public int RefreshCalls { get; private set; }
    public List<SiteOperation> RefreshOperations { get; } = new();
    public Func<Action<string>?, CancellationToken, Task<SiteOperationResult>> StartHandler { get; set; } =
        (_, _) => Task.FromResult(SiteOperationResult.Ok("Сайт запущен."));
    public Func<Action<string>?, CancellationToken, Task<SiteOperationResult>> StopHandler { get; set; } =
        (_, _) => Task.FromResult(SiteOperationResult.Ok("Сайт остановлен."));
    public Func<SiteKeysUpdate, Action<string>?, CancellationToken, Task<SiteOperationResult>> ApplyHandler { get; set; } =
        (_, _, _) => Task.FromResult(SiteOperationResult.Ok("Настройки сохранены и применены."));
    public Func<string, Task<SiteLogsResult>> LogsHandler { get; set; } =
        service => Task.FromResult(new SiteLogsResult(true, $"log of {service}", null));
    public SiteKeysState? Keys { get; set; } = new(false, false, "");
    public List<SiteKeysUpdate> Applied { get; } = new();
    public List<string> LogRequests { get; } = new();

    public Task<SiteSnapshot> RefreshAsync(SiteOperation operation, CancellationToken ct)
    {
        RefreshCalls++;
        RefreshOperations.Add(operation);
        return Refresh != null ? Refresh(operation) : Task.FromResult(Snapshot);
    }

    public Task<SiteOperationResult> StartAsync(Action<string>? status, CancellationToken ct) => StartHandler(status, ct);
    public Task<SiteOperationResult> StopAsync(Action<string>? status, CancellationToken ct) => StopHandler(status, ct);
    public Task<SiteOperationResult> RestartAsync(Action<string>? status, CancellationToken ct) => StartHandler(status, ct);
    public Task<SiteOperationResult> StartDockerAsync(Action<string>? status, CancellationToken ct) => StartHandler(status, ct);

    public Task<SiteOperationResult> ApplyKeysAsync(SiteKeysUpdate update, Action<string>? status, CancellationToken ct)
    {
        Applied.Add(update);
        return ApplyHandler(update, status, ct);
    }

    public Task<SiteLogsResult> GetLogsAsync(string service, CancellationToken ct)
    {
        LogRequests.Add(service);
        return LogsHandler(service);
    }

    public SiteKeysState? ReadKeysState() => Keys;
}

/// <summary>Готовые наборы данных и сборка менеджера с подставными зависимостями.</summary>
internal sealed class SiteTestEnv
{
    public const string ServerDir = @"C:\hq\server";
    public static string EnvPath => Path.Combine(ServerDir, ".env");
    public static string PublicUrlPath => Path.Combine(ServerDir, "public-url.txt");

    public const string BaseEnv =
        "# HQ Studio settings\r\n" +
        "HQSTUDIO_VERSION=1.19.6\r\n" +
        "HQ_PORT=8080\r\n" +
        "\r\n" +
        "POSTGRES_PASSWORD=pgsecret123\r\n" +
        "JWT_KEY=jwtsecret456789\r\n" +
        "ADMIN_PASSWORD=adminsecret1\r\n" +
        "GEMINI_API_KEY=\r\n" +
        "TUNA_TOKEN=\r\n" +
        "TUNA_SUBDOMAIN=\r\n" +
        "TUNA_DOMAIN=\r\n" +
        "PUBLIC_URL=\r\n";

    public SiteFakeFiles Files { get; } = new();
    public SiteFakeRunner Runner { get; } = new();
    public SiteFakeLocator Locator { get; } = new();
    public SiteFakeProbe Probe { get; } = new();
    public SiteFakeInstallStore Install { get; } = new();
    public List<string> Events { get; } = new();
    public SiteManager Manager { get; }

    public SiteTestEnv(string env = BaseEnv)
    {
        Files.Add(EnvPath, env);
        Runner.OnCall = call => Events.Add("run:" + call);
        var timings = new SiteTimings(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(3), (_, _) => Task.CompletedTask);
        Manager = new SiteManager(Install, Files, Runner, Locator, Probe, timings);
    }

    public static string EnvWithToken(string token = "tunatoken1234", string subdomain = "hq", string domain = "") =>
        BaseEnv.Replace("TUNA_TOKEN=\r\n", $"TUNA_TOKEN={token}\r\n")
            .Replace("TUNA_SUBDOMAIN=\r\n", $"TUNA_SUBDOMAIN={subdomain}\r\n")
            .Replace("TUNA_DOMAIN=\r\n", $"TUNA_DOMAIN={domain}\r\n");

    public string Env => Files.Files[EnvPath];

    public static ComposeServiceEntry Entry(string service, string state = "running", string health = "healthy",
        string status = "Up 5 minutes", int? exit = null) => new(service, state, health, status, exit);

    public static string PsJson(params ComposeServiceEntry[] entries) => string.Join("\n", entries.Select(e =>
        $"{{\"Service\":\"{e.Service}\",\"State\":\"{e.State}\",\"Health\":\"{e.Health}\",\"Status\":\"{e.Status}\",\"ExitCode\":{e.ExitCode ?? 0}}}"));

    public static ComposeServiceEntry[] AllRunning(bool tunnel = false)
    {
        var list = new List<ComposeServiceEntry>
        {
            Entry("db"), Entry("api"), Entry("web"), Entry("proxy", health: "")
        };
        if (tunnel)
            list.Add(Entry("tuna", health: ""));
        return list.ToArray();
    }

    public static SiteSnapshot RunningSnapshot(bool tunnel = false)
    {
        var input = new SiteStatusInput(true, null, SiteDockerState.Running, AllRunning(tunnel), null, tunnel, true, 0,
            SiteOperation.None);
        var services = SiteStatusEvaluator.BuildServices(input);
        return new SiteSnapshot(true, SiteDockerState.Running, SiteStatusEvaluator.Evaluate(input, services), services,
            "1.19.6", "http://localhost:8080", null, tunnel, null);
    }

    public static SiteSnapshot SnapshotFor(SitePill pill, bool installed = true, SiteDockerState docker = SiteDockerState.Running)
    {
        var services = SiteStatusEvaluator.PlaceholderServices();
        return new SiteSnapshot(installed, docker, new SiteOverview(pill, pill.ToString(), ""), services, "1.0",
            "http://localhost:8080", null, false, null);
    }

    /// <summary>Docker работает и отвечает; ps отдаёт заданные контейнеры.</summary>
    public SiteTestEnv WithPs(params ComposeServiceEntry[] entries)
    {
        Runner.When(" ps --all --format json", 0, PsJson(entries));
        return this;
    }

    public static SiteProcessResult DockerDown() => new(1, "",
        "error during connect: Get \"http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/v1.46/version\": open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.");
}
