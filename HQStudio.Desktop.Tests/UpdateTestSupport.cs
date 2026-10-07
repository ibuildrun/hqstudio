using System.IO;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using HQStudio.Services.Updates;

namespace HQStudio.Desktop.Tests;

/// <summary>Temporary folder removed on dispose.</summary>
internal sealed class UpdateTempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "hqstudio-tests-" + Guid.NewGuid().ToString("N"));

    public UpdateTempDir() => Directory.CreateDirectory(Path);

    public string Combine(params string[] parts) => System.IO.Path.Combine(new[] { Path }.Concat(parts).ToArray());

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

internal sealed class UpdateFakeHttp : HttpMessageHandler
{
    private readonly object _lock = new();
    public List<string> Requests { get; } = new();
    public List<HttpRequestMessage> Messages { get; } = new();
    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            Requests.Add(request.RequestUri!.ToString());
            Messages.Add(request);
        }
        return Task.FromResult(Respond(request));
    }

    public int Count(string urlPart)
    {
        lock (_lock) return Requests.Count(r => r.Contains(urlPart, StringComparison.Ordinal));
    }

    public static HttpResponseMessage Bytes(byte[] data) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(data) };
    public static HttpResponseMessage Text(string text, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
}

internal sealed record UpdateFakeScript(int ExitCode = 0, string[]? Lines = null, string StdErr = "", Exception? Throw = null);

internal sealed class UpdateFakeProcess : IProcessRunner
{
    private readonly object _lock = new();
    /// <summary>Logical commands: "docker compose pull" (project directory, -f and --profile are stripped).</summary>
    public List<string> Calls { get; } = new();
    /// <summary>Exact arguments as passed, for the tests that check the compose invocation itself.</summary>
    public List<string> RawCalls { get; } = new();
    public List<string?> WorkingDirectories { get; } = new();
    /// <summary>Keyed by space-joined arguments ("compose pull"); the first matching key wins.</summary>
    public Dictionary<string, UpdateFakeScript> Scripts { get; } = new();
    public Action<string>? OnCall { get; set; }

    public Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string? workingDirectory,
        Action<string>? onLine, CancellationToken ct)
    {
        var key = Normalize(arguments);
        lock (_lock)
        {
            RawCalls.Add($"{fileName} {string.Join(' ', arguments)}");
            Calls.Add($"{fileName} {key}");
            WorkingDirectories.Add(workingDirectory);
        }
        OnCall?.Invoke(key);

        if (!Scripts.TryGetValue(key, out var script))
            script = new UpdateFakeScript();
        if (script.Throw != null)
            throw script.Throw;

        foreach (var line in script.Lines ?? Array.Empty<string>())
            onLine?.Invoke(line);
        return Task.FromResult(new ProcessResult(script.ExitCode, string.Join('\n', script.Lines ?? Array.Empty<string>()), script.StdErr));
    }

    private static string Normalize(IReadOnlyList<string> arguments)
    {
        var kept = new List<string>();
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is "--project-directory" or "-f" or "--profile")
            {
                i++;
                continue;
            }
            kept.Add(arguments[i]);
        }
        return string.Join(' ', kept);
    }
}

internal sealed class UpdateProgressRecorder : IProgress<UpdateProgress>
{
    public List<UpdateProgress> Items { get; } = new();
    public void Report(UpdateProgress value) { lock (Items) Items.Add(value); }
}

internal static class UpdateTestData
{
    public static byte[] Zip(params (string Name, string Content)[] files)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = zip.CreateEntry(name);
                using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                w.Write(content);
            }
        }
        return ms.ToArray();
    }

    public static byte[] ZipBytes(string name, byte[] content)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(name);
            using var s = entry.Open();
            s.Write(content, 0, content.Length);
        }
        return ms.ToArray();
    }

    public static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    /// <summary>A fake executable: MZ header padded past the size sanity threshold.</summary>
    public static byte[] FakeExe(char fill = 'N')
    {
        var data = new byte[4096];
        Array.Fill(data, (byte)fill);
        data[0] = (byte)'M';
        data[1] = (byte)'Z';
        return data;
    }

    public const string OldCompose = "services: old\n";
    public const string OldNginx = "# old nginx\n";
    public const string OldEnv = "# site settings\r\nPOSTGRES_PASSWORD=secret\r\nHQSTUDIO_VERSION=1.19.6\r\nWEB_PORT=8080\r\n";

    public static string CreateInstall(UpdateTempDir temp, string envContent = OldEnv)
    {
        var serverDir = temp.Combine("server");
        Directory.CreateDirectory(Path.Combine(serverDir, "nginx"));
        File.WriteAllText(Path.Combine(serverDir, "docker-compose.yml"), OldCompose);
        File.WriteAllText(Path.Combine(serverDir, "nginx", "default.conf"), OldNginx);
        File.WriteAllText(Path.Combine(serverDir, ".env"), envContent);
        return serverDir;
    }

    public static InstallInfo Install(string serverDir) =>
        new(serverDir, "http://localhost:8080", "http://localhost:8080", "ibuildrun/hqstudio");

    public static byte[] ServerZip() => Zip(
        ("docker-compose.yml", "services: new\n"),
        ("nginx/default.conf", "# new nginx\n"),
        ("scripts/deploy.ps1", "Write-Host new\n"),
        (".env", "OVERWRITTEN=1\n"));

    public static ReleaseInfo Release(string version = "1.20.0", byte[]? serverZip = null, bool withDigest = true,
        byte[]? desktopZip = null, string? badDigest = null)
    {
        var assets = new List<ReleaseAsset>();
        if (serverZip != null)
            assets.Add(new ReleaseAsset($"HQStudio-Server-v{version}.zip", "https://download.test/server.zip",
                serverZip.Length, badDigest ?? (withDigest ? Sha256(serverZip) : null)));
        if (desktopZip != null)
            assets.Add(new ReleaseAsset($"HQStudio-Desktop-v{version}.zip", "https://download.test/desktop.zip",
                desktopZip.Length, badDigest ?? (withDigest ? Sha256(desktopZip) : null)));
        return new ReleaseInfo
        {
            Tag = "v" + version,
            Version = version,
            Name = "v" + version,
            Notes = "## Что нового\n- улучшения\n",
            Assets = assets
        };
    }

    public static string ReleaseJson(string tag = "v1.20.0", string? serverDigest = null, string? desktopDigest = null)
    {
        string Digest(string? d) => d == null ? "" : $", \"digest\": \"{d}\"";
        return $$"""
        {
          "tag_name": "{{tag}}",
          "name": "{{tag}}",
          "body": "## Changes\n- one\n- two",
          "html_url": "https://github.com/ibuildrun/hqstudio/releases/tag/{{tag}}",
          "published_at": "2026-10-01T12:00:00Z",
          "assets": [
            { "name": "HQStudio-Desktop-{{tag}}.zip", "browser_download_url": "https://download.test/desktop.zip", "size": 1000{{Digest(desktopDigest)}} },
            { "name": "HQStudio-Server-{{tag}}.zip", "browser_download_url": "https://download.test/server.zip", "size": 500{{Digest(serverDigest)}} },
            { "name": "notes.txt", "browser_download_url": "https://download.test/notes.txt", "size": 5 }
          ]
        }
        """;
    }

    public static ServerUpdateOptions FastOptions(UpdateTempDir temp) => new()
    {
        DockerExecutable = "docker",
        TempRoot = temp.Combine("tmp"),
        HealthTimeout = TimeSpan.FromMilliseconds(300),
        RollbackHealthTimeout = TimeSpan.FromMilliseconds(300),
        HealthPollInterval = TimeSpan.FromMilliseconds(20),
        DockerCheckTimeout = TimeSpan.FromSeconds(5)
    };

    public static Win32Exception DockerMissing() => new(2, "The system cannot find the file specified");
}

/// <summary>Coordinator wired to fakes: GitHub, downloads, health, docker and the app exe are all in-memory or temp.</summary>
internal sealed class UpdateRig : IDisposable
{
    public UpdateTempDir Temp { get; } = new();
    public string? ServerDir { get; }
    public string ExePath { get; }
    public byte[] OldExe { get; } = UpdateTestData.FakeExe('O');
    public UpdateFakeHttp Github { get; } = new();
    public UpdateFakeHttp Health { get; } = new();
    public UpdateFakeProcess Runner { get; } = new();
    public List<string> Events { get; } = new();
    public List<string> Toasts { get; } = new();
    public int ShutdownCalls { get; private set; }
    public DateTime Now { get; set; } = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
    public bool AutoCheck { get; set; } = true;
    public string StatePath { get; }
    public UpdateCoordinator Coordinator { get; }
    public byte[] ServerZip { get; } = UpdateTestData.ServerZip();
    public byte[] DesktopZip { get; }

    public UpdateRig(bool siteInstalled = true, string installedAppVersion = "1.19.6", string latestTag = "v1.20.0",
        string envContent = UpdateTestData.OldEnv)
    {
        DesktopZip = UpdateTestData.ZipBytes("HQStudio.exe", UpdateTestData.FakeExe('N'));
        ExePath = Temp.Combine("app", "HQStudio.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(ExePath)!);
        File.WriteAllBytes(ExePath, OldExe);

        var installJson = Temp.Combine("install.json");
        if (siteInstalled)
        {
            ServerDir = UpdateTestData.CreateInstall(Temp, envContent);
            File.WriteAllText(installJson, System.Text.Json.JsonSerializer.Serialize(new
            {
                serverDir = ServerDir,
                webUrl = "http://localhost:8080",
                apiUrl = "http://localhost:8080",
                repo = "ibuildrun/hqstudio"
            }));
        }

        Github.Respond = req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.Contains("api.github.com"))
                return UpdateFakeHttp.Text(UpdateTestData.ReleaseJson(latestTag));
            if (url.EndsWith("server.zip")) return UpdateFakeHttp.Bytes(ServerZip);
            if (url.EndsWith("desktop.zip")) return UpdateFakeHttp.Bytes(DesktopZip);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };
        Health.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK);
        Runner.OnCall = key => Events.Add("docker " + key);

        StatePath = Temp.Combine("update-state.json");
        var downloader = new ReleaseDownloader(Github);
        var app = new AppUpdateService(downloader, new AppUpdateOptions
        {
            CurrentExePath = ExePath,
            CurrentProcessId = 1,
            TempRoot = Temp.Combine("tmp"),
            LaunchHelper = _ => Events.Add("launch-helper"),
            ShutdownApplication = () => { ShutdownCalls++; Events.Add("shutdown"); }
        });
        var server = new ServerUpdateService(Runner, downloader, Health, UpdateTestData.FastOptions(Temp));

        Coordinator = new UpdateCoordinator(
            new GitHubReleaseClient(Github), app, server,
            () => InstallInfoReader.Read(installJson), installedAppVersion, new UpdateStateStore(StatePath),
            () => AutoCheck, () => Now, msg => Toasts.Add(msg));
    }

    public int GithubApiCalls => Github.Count("api.github.com");

    public void Dispose() => Temp.Dispose();
}
