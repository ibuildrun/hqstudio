using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HQStudio.Services.BugReport
{
    public sealed record InstallInfo(string? ServerDir, string? WebUrl, string? ApiUrl);

    public sealed record DiagnosticsSnapshot
    {
        public string AppVersion { get; init; } = "unknown";
        public string WindowsVersion { get; init; } = "";
        public string DotNetVersion { get; init; } = "";
        public InstallInfo? Install { get; init; }
        public string? ApiUrl { get; init; }
        public string? ServerVersion { get; init; }

        // Поля ниже заполняются только когда логи запрошены.
        public string? ExceptionText { get; init; }
        public string? CrashLogTail { get; init; }
        public string? DockerComposePs { get; init; }
    }

    public sealed class DiagnosticsCollector
    {
        public const int CrashLogLines = 200;
        public static readonly TimeSpan DockerTimeout = TimeSpan.FromSeconds(20);

        private const int MaxExceptionChars = 8000;
        private const int MaxLogChars = 20000;
        private const int MaxDockerChars = 6000;

        private readonly IProcessRunner _process;
        private readonly IFileReader _files;
        private readonly string _crashLogPath;
        private readonly string _installInfoPath;

        public DiagnosticsCollector(IProcessRunner? process = null, IFileReader? files = null, string? dataDirectory = null)
        {
            _process = process ?? new SystemProcessRunner();
            _files = files ?? new DiskFileReader();
            var dir = dataDirectory ?? BugReportConfig.DataDirectory;
            _crashLogPath = Path.Combine(dir, "crash.log");
            _installInfoPath = Path.Combine(dir, "install.json");
        }

        public async Task<DiagnosticsSnapshot> CollectAsync(Exception? crash, bool includeLogs,
            string? apiUrl, string? serverVersion, CancellationToken ct = default)
        {
            var install = ReadInstallInfo();
            var snapshot = new DiagnosticsSnapshot
            {
                AppVersion = GetAppVersion(),
                WindowsVersion = RuntimeInformation.OSDescription,
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                Install = install,
                ApiUrl = StripCredentials(apiUrl),
                ServerVersion = string.IsNullOrWhiteSpace(serverVersion) ? null : serverVersion
            };

            if (!includeLogs) return snapshot;

            return snapshot with
            {
                ExceptionText = crash == null ? null : Tail(crash.ToString(), MaxExceptionChars, keepEnd: false),
                CrashLogTail = ReadCrashLog(),
                DockerComposePs = await RunDockerComposePsAsync(install, ct)
            };
        }

        public static string GetAppVersion()
        {
            var version = typeof(DiagnosticsCollector).Assembly.GetName().Version;
            return version == null ? "unknown" : $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static string? StripCredentials(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var trimmed = url.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.UserInfo))
                return trimmed;

            return new UriBuilder(uri) { UserName = "", Password = "" }.Uri.AbsoluteUri;
        }

        private InstallInfo? ReadInstallInfo()
        {
            try
            {
                if (!_files.FileExists(_installInfoPath)) return null;

                using var doc = JsonDocument.Parse(_files.ReadAllText(_installInfoPath));
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

                string? Read(string name)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.String)
                        {
                            return prop.Value.GetString();
                        }
                    }
                    return null;
                }

                return new InstallInfo(Read("serverDir"), StripCredentials(Read("webUrl")), StripCredentials(Read("apiUrl")));
            }
            catch
            {
                // Нечитаемый install.json просто пропускаем.
                return null;
            }
        }

        private string? ReadCrashLog()
        {
            try
            {
                if (!_files.FileExists(_crashLogPath)) return null;

                var lines = _files.ReadLastLines(_crashLogPath, CrashLogLines);
                if (lines.Count == 0) return null;

                return Tail(string.Join("\n", lines), MaxLogChars, keepEnd: true);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> RunDockerComposePsAsync(InstallInfo? install, CancellationToken ct)
        {
            var serverDir = install?.ServerDir;
            if (string.IsNullOrWhiteSpace(serverDir)) return null;

            try
            {
                if (!_files.DirectoryExists(serverDir))
                    return $"docker compose ps was not run: serverDir not found ({serverDir})";

                var result = await _process.RunAsync("docker", "compose ps", serverDir, DockerTimeout, ct);

                if (result.TimedOut)
                    return $"docker compose ps timed out after {DockerTimeout.TotalSeconds:0} s";

                if (result.ExitCode != 0)
                {
                    var reason = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
                    return Tail($"docker compose ps failed (exit code {result.ExitCode}): {reason.Trim()}",
                        MaxDockerChars, keepEnd: false);
                }

                var output = result.StdOut.Trim();
                return output.Length == 0
                    ? "(docker compose ps returned no containers)"
                    : Tail(output, MaxDockerChars, keepEnd: false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return $"docker compose ps failed: {ex.Message}";
            }
        }

        private static string Tail(string text, int maxChars, bool keepEnd)
        {
            if (text.Length <= maxChars) return text;
            return keepEnd
                ? "... (truncated)\n" + text[^maxChars..]
                : text[..maxChars] + "\n... (truncated)";
        }
    }
}
