using System.IO;
using System.Text.Json;

namespace HQStudio.Services.Site
{
    /// <summary>Пути, которые знает и приложение, и установщик.</summary>
    public sealed class SitePaths
    {
        public SitePaths(string localAppDataRoot)
        {
            Root = localAppDataRoot;
        }

        /// <summary>%LOCALAPPDATA%\HQStudio</summary>
        public string Root { get; }

        public string InstallJson => Path.Combine(Root, "install.json");

        /// <summary>Папка сайта, которую создаёт установщик.</summary>
        public string DefaultServerDir => Path.Combine(Root, "server");

        /// <summary>Копия .env, которую оставляет удаление без стирания данных.</summary>
        public string EnvBackup => Path.Combine(Root, "site-env.backup");

        public static SitePaths FromEnvironment() => new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio"));

        public static string EnvFile(string serverDir) => Path.Combine(serverDir, ".env");

        public static string ComposeFile(string serverDir) => Path.Combine(serverDir, "docker-compose.yml");

        public static string PublicUrlFile(string serverDir) => Path.Combine(serverDir, "public-url.txt");
    }

    public interface ISiteInstallStore
    {
        /// <summary>Нет файла - сайт не установлен (это не ошибка); битый файл возвращает пояснение.</summary>
        SiteInstallReadResult Read();
    }

    public sealed class SiteInstallStore : ISiteInstallStore
    {
        private readonly ISiteFiles _files;
        private readonly string _path;

        public SiteInstallStore(ISiteFiles files, string installJsonPath)
        {
            _files = files;
            _path = installJsonPath;
        }

        public SiteInstallReadResult Read() => Parse(_files, _path);

        public static SiteInstallReadResult Parse(ISiteFiles files, string path)
        {
            if (!files.FileExists(path))
                return new SiteInstallReadResult(null, null);

            try
            {
                using var doc = JsonDocument.Parse(files.ReadAllText(path));
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return new SiteInstallReadResult(null, "Файл установки сайта имеет неверный формат.");

                var serverDir = GetString(root, "serverDir");
                if (serverDir.Length == 0)
                    return new SiteInstallReadResult(null, "В файле установки сайта не указана папка сервера.");

                var webUrl = GetString(root, "webUrl");
                var apiUrl = GetString(root, "apiUrl");
                if (apiUrl.Length == 0)
                    apiUrl = webUrl;

                return new SiteInstallReadResult(
                    new SiteInstallInfo(serverDir, webUrl, apiUrl, GetString(root, "repo")), null);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return new SiteInstallReadResult(null, "Не удалось прочитать файл установки сайта.");
            }
        }

        private static string GetString(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString()?.Trim() ?? "";
            }
            return "";
        }
    }
}
