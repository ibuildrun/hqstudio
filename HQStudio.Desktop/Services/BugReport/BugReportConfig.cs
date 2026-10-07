using System.IO;
using System.Text.Json;

namespace HQStudio.Services.BugReport
{
    public static class BugReportConfig
    {
        // Публичное значение OAuth App (секрета нет). Пусто = автоматическая отправка выключена.
        public const string DefaultClientId = "";

        public const string ClientIdEnvVar = "HQSTUDIO_GITHUB_CLIENT_ID";
        public const string ClientIdSettingsKey = "GitHubClientId";

        public const string Repository = "ibuildrun/hqstudio";
        public const string OAuthScope = "public_repo";
        public const string ReportMarker = "<!-- hqstudio-app-report -->";
        public const string IssueLabel = "from-app";
        public const string UserAgent = "HQStudio-Desktop";
        public const string ApiVersion = "2022-11-28";

        public const string DeviceCodeUrl = "https://github.com/login/device/code";
        public const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
        public const string DeviceActivationUrl = "https://github.com/login/device";
        public const string IssuesApiUrl = "https://api.github.com/repos/" + Repository + "/issues";
        public const string NewIssueWebUrl = "https://github.com/" + Repository + "/issues/new";

        public static string DataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio");

        public static string TokenPath => Path.Combine(DataDirectory, "github.token");
        public static string CrashLogPath => Path.Combine(DataDirectory, "crash.log");
        public static string InstallInfoPath => Path.Combine(DataDirectory, "install.json");

        public static string ResolveClientId() => ResolveClientId(
            Environment.GetEnvironmentVariable(ClientIdEnvVar),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"));

        public static string ResolveClientId(string? envValue, string appSettingsPath)
        {
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            try
            {
                if (File.Exists(appSettingsPath))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(appSettingsPath),
                        new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty(ClientIdSettingsKey, out var prop) &&
                        prop.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(prop.GetString()))
                    {
                        return prop.GetString()!.Trim();
                    }
                }
            }
            catch
            {
                // Битый appsettings.json не должен ломать отправку отчёта.
            }

            return DefaultClientId;
        }
    }

    public enum BugReportErrorKind
    {
        Network,
        NotConfigured,
        AuthExpired,
        AuthDenied,
        Unauthorized,
        Forbidden,
        RateLimited,
        Rejected,
        ServerError,
        Unexpected
    }

    public sealed class BugReportException : Exception
    {
        public BugReportErrorKind Kind { get; }
        public int? StatusCode { get; }

        public BugReportException(BugReportErrorKind kind, string message, int? statusCode = null, Exception? inner = null)
            : base(message, inner)
        {
            Kind = kind;
            StatusCode = statusCode;
        }
    }
}
