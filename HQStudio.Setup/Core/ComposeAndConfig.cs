using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HQStudio.Setup.Core;

public static class ComposeArgs
{
    /// <summary>`docker compose --project-directory dir -f dir\docker-compose.yml [--profile tunnel] command...`</summary>
    public static IReadOnlyList<string> Build(string serverDir, bool tunnel, params string[] command)
    {
        var args = new List<string>
        {
            "compose",
            "--project-directory", serverDir,
            "-f", Path.Combine(serverDir, "docker-compose.yml")
        };
        if (tunnel)
        {
            args.Add("--profile");
            args.Add("tunnel");
        }
        args.AddRange(command);
        return args;
    }
}

/// <summary>install.json (read by the desktop app and its updater) and the first-run settings.json.</summary>
public static class AppConfigWriter
{
    public const string Repo = "ibuildrun/hqstudio";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string LocalUrl(int port) => $"http://localhost:{port}";

    public static string InstallJsonText(string serverDir, int port) => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["serverDir"] = serverDir,
        ["webUrl"] = LocalUrl(port),
        ["apiUrl"] = LocalUrl(port),
        ["repo"] = Repo
    }, Options);

    public static string SettingsJsonText(int port) =>
        $"{{\"Theme\":\"Dark\",\"ShowSplash\":true,\"Language\":\"ru\",\"ApiUrl\":\"{LocalUrl(port)}\",\"UseApi\":true}}";

    public static void WriteInstallJson(InstallPaths paths, int port)
    {
        Directory.CreateDirectory(paths.DataDir);
        File.WriteAllText(paths.InstallJson, InstallJsonText(paths.ServerDir, port), Utf8NoBom);
    }

    /// <summary>Returns true when the file was created; an existing file (the user's own settings) is left alone.</summary>
    public static bool WriteSettingsIfAbsent(InstallPaths paths, int port)
    {
        if (File.Exists(paths.SettingsJson))
            return false;
        Directory.CreateDirectory(paths.SettingsDir);
        File.WriteAllText(paths.SettingsJson, SettingsJsonText(port), Utf8NoBom);
        return true;
    }
}
