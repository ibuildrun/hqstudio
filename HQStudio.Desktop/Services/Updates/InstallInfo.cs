using System.IO;
using System.Text.Json;

namespace HQStudio.Services.Updates
{
    /// <summary>Install layout written by the installer to %LOCALAPPDATA%\HQStudio\install.json.</summary>
    public sealed record InstallInfo(string ServerDir, string WebUrl, string ApiUrl, string Repo);

    public sealed record InstallInfoReadResult(InstallInfo? Info, string? Problem)
    {
        public bool NotInstalled => Info == null && Problem == null;
    }

    public static class InstallInfoReader
    {
        public static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio", "install.json");

        /// <summary>A missing file means "site not installed" (no problem); a broken file reports a problem.</summary>
        public static InstallInfoReadResult Read(string? path = null)
        {
            path ??= DefaultPath;
            if (!File.Exists(path))
                return new InstallInfoReadResult(null, null);

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return new InstallInfoReadResult(null, "Файл установки сайта имеет неверный формат.");

                var serverDir = Get(root, "serverDir");
                if (serverDir.Length == 0)
                    return new InstallInfoReadResult(null, "В файле установки сайта не указана папка сервера.");

                var webUrl = Get(root, "webUrl");
                var apiUrl = Get(root, "apiUrl");
                if (apiUrl.Length == 0)
                    apiUrl = webUrl;
                if (apiUrl.Length == 0)
                    return new InstallInfoReadResult(null, "В файле установки сайта не указан адрес сайта.");

                var repo = Get(root, "repo");
                if (repo.Length == 0)
                    repo = GitHubReleaseClient.DefaultRepo;

                return new InstallInfoReadResult(new InstallInfo(serverDir, webUrl, apiUrl, repo), null);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return new InstallInfoReadResult(null, "Не удалось прочитать файл установки сайта.");
            }
        }

        private static string Get(JsonElement e, string name)
        {
            foreach (var p in e.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    p.Value.ValueKind == JsonValueKind.String)
                    return p.Value.GetString()?.Trim() ?? "";
            }
            return "";
        }
    }
}
