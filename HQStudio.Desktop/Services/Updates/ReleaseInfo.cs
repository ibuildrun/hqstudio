using System.Text.Json;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Updates
{
    public sealed record ReleaseAsset(string Name, string DownloadUrl, long Size, string? Sha256);

    public sealed class ReleaseInfo
    {
        public string Tag { get; init; } = "";
        /// <summary>Tag without the leading "v", e.g. 1.20.0.</summary>
        public string Version { get; init; } = "";
        public string Name { get; init; } = "";
        public string Notes { get; init; } = "";
        public string HtmlUrl { get; init; } = "";
        public DateTimeOffset? PublishedAt { get; init; }
        public IReadOnlyList<ReleaseAsset> Assets { get; init; } = Array.Empty<ReleaseAsset>();

        public ReleaseAsset? DesktopAsset => FindAsset("HQStudio-Desktop");
        public ReleaseAsset? ServerAsset => FindAsset("HQStudio-Server");

        private ReleaseAsset? FindAsset(string prefix)
        {
            var exact = $"{prefix}-v{Version}.zip";
            return Assets.FirstOrDefault(a => string.Equals(a.Name, exact, StringComparison.OrdinalIgnoreCase))
                   ?? Assets.FirstOrDefault(a =>
                       a.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                       a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }

        public static ReleaseInfo Parse(string json)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new UpdateException("Не удалось разобрать ответ GitHub. Попробуйте позже.", ex);
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    throw new UpdateException("GitHub вернул неожиданный ответ. Попробуйте позже.");

                var tag = GetString(root, "tag_name");
                if (!SemVer.TryParse(tag, out var semver))
                    throw new UpdateException("Не удалось определить номер последней версии.");

                var assets = new List<ReleaseAsset>();
                if (root.TryGetProperty("assets", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in arr.EnumerateArray())
                    {
                        var name = GetString(a, "name");
                        var url = GetString(a, "browser_download_url");
                        if (name.Length == 0 || url.Length == 0)
                            continue;

                        long size = 0;
                        if (a.TryGetProperty("size", out var sz) && sz.ValueKind == JsonValueKind.Number)
                            sz.TryGetInt64(out size);

                        assets.Add(new ReleaseAsset(name, url, size, ParseDigest(GetString(a, "digest"))));
                    }
                }

                DateTimeOffset? published = null;
                if (DateTimeOffset.TryParse(GetString(root, "published_at"),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal, out var p))
                    published = p;

                return new ReleaseInfo
                {
                    Tag = tag,
                    Version = semver.ToString(),
                    Name = GetString(root, "name"),
                    Notes = GetString(root, "body"),
                    HtmlUrl = GetString(root, "html_url"),
                    PublishedAt = published,
                    Assets = assets
                };
            }
        }

        private static string GetString(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        /// <summary>Accepts "sha256:&lt;hex&gt;"; any other algorithm or malformed value means no verification.</summary>
        internal static string? ParseDigest(string digest)
        {
            const string prefix = "sha256:";
            if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;
            var hex = digest[prefix.Length..].Trim().ToLowerInvariant();
            return Regex.IsMatch(hex, "^[0-9a-f]{64}$") ? hex : null;
        }
    }

    /// <summary>Turns GitHub markdown release notes into short plain text for a dialog.</summary>
    public static class ReleaseNotesFormatter
    {
        public static string ToPlainText(string? markdown, int maxLength = 4000)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "";

            var text = markdown.Replace("\r\n", "\n");
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
            text = Regex.Replace(text, @"^\s{0,3}#{1,6}\s*", "", RegexOptions.Multiline);
            text = Regex.Replace(text, @"^\s*[*+]\s+", "- ", RegexOptions.Multiline);
            text = text.Replace("**", "").Replace("__", "").Replace("`", "");
            text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();

            if (text.Length > maxLength)
                text = text[..maxLength].TrimEnd() + "...";
            return text;
        }
    }
}
