using System.Text.Json;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    public static class SiteComposeCommand
    {
        public const string TunnelProfile = "tunnel";

        /// <summary>
        /// <c>compose --project-directory D -f D\docker-compose.yml [--profile tunnel] команда...</c>.
        /// Профиль tunnel добавляется только когда в .env задан TUNA_TOKEN.
        /// </summary>
        public static IReadOnlyList<string> Build(string serverDir, bool tunnelEnabled, params string[] command)
        {
            var args = new List<string>
            {
                "compose", "--project-directory", serverDir, "-f", SitePaths.ComposeFile(serverDir)
            };
            if (tunnelEnabled)
            {
                args.Add("--profile");
                args.Add(TunnelProfile);
            }
            args.AddRange(command);
            return args;
        }
    }

    public static class SiteComposePsParser
    {
        /// <summary>
        /// Разбирает вывод <c>ps --all --format json</c>: и по объекту на строку (новый compose), и один массив (старый).
        /// Посторонние строки, например предупреждения, пропускаются.
        /// </summary>
        public static IReadOnlyList<ComposeServiceEntry> Parse(string? output)
        {
            var result = new List<ComposeServiceEntry>();
            var text = (output ?? "").Trim();
            if (text.Length == 0)
                return result;

            if (text[0] == '[' && TryParseDocument(text, result))
                return result;

            result.Clear();
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] is not ('{' or '['))
                    continue;
                TryParseDocument(line, result);
            }
            return result;
        }

        private static bool TryParseDocument(string json, List<ComposeServiceEntry> sink)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                switch (doc.RootElement.ValueKind)
                {
                    case JsonValueKind.Array:
                        foreach (var item in doc.RootElement.EnumerateArray())
                            AddEntry(item, sink);
                        return true;
                    case JsonValueKind.Object:
                        AddEntry(doc.RootElement, sink);
                        return true;
                    default:
                        return false;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static void AddEntry(JsonElement element, List<ComposeServiceEntry> sink)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return;

            var service = GetString(element, "Service");
            if (service.Length == 0)
                return;

            sink.Add(new ComposeServiceEntry(
                service,
                GetString(element, "State").ToLowerInvariant(),
                GetString(element, "Health").ToLowerInvariant(),
                GetString(element, "Status"),
                GetInt(element, "ExitCode")));
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

        private static int? GetInt(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.Number &&
                    property.Value.TryGetInt32(out var value))
                    return value;
            }
            return null;
        }
    }

    public static class SiteTunnelUrlParser
    {
        private static readonly Regex Forwarding = new(@"Forwarding\s+(https?://\S+?)\s+->", RegexOptions.Compiled);

        /// <summary>Последний «Forwarding https://... -> ...» в журнале tuna: после перезапуска адрес пишется заново.</summary>
        public static string? Parse(string? log)
        {
            if (string.IsNullOrEmpty(log))
                return null;

            string? url = null;
            foreach (Match match in Forwarding.Matches(log))
                url = match.Groups[1].Value;
            return url;
        }
    }
}
