using System.Text;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    /// <summary>Ключи файла .env, которые использует страница «Сайт».</summary>
    public static class SiteEnvKeys
    {
        public const string Version = "HQSTUDIO_VERSION";
        public const string Port = "HQ_PORT";
        public const string PostgresPassword = "POSTGRES_PASSWORD";
        public const string JwtKey = "JWT_KEY";
        public const string AdminPassword = "ADMIN_PASSWORD";
        public const string GeminiKey = "GEMINI_API_KEY";
        public const string TunaToken = "TUNA_TOKEN";
        public const string TunaSubdomain = "TUNA_SUBDOMAIN";
        public const string PublicUrl = "PUBLIC_URL";

        public const int DefaultPort = 8080;

        /// <summary>Значения, которые нельзя показывать в логах и на экране.</summary>
        public static readonly IReadOnlyList<string> Secrets = new[]
        {
            PostgresPassword, JwtKey, AdminPassword, GeminiKey, TunaToken
        };
    }

    /// <summary>Чтение и точечная правка .env: чужие строки, комментарии и стиль переводов строк остаются как были.</summary>
    public static class SiteEnvFile
    {
        private static readonly Regex Assignment = new(@"^\s*(?:export\s+)?([A-Za-z_][A-Za-z0-9_.\-]*)\s*=(.*)$",
            RegexOptions.Compiled);

        private static readonly Regex SubdomainPattern = new(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$",
            RegexOptions.Compiled);

        public static string? GetValue(string text, string key)
        {
            string? result = null;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                var match = Assignment.Match(line);
                if (match.Success && match.Groups[1].Value == key)
                    result = Unquote(match.Groups[2].Value);
            }
            return result;
        }

        public static IReadOnlyDictionary<string, string> Parse(string text)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var raw in text.Split('\n'))
            {
                var match = Assignment.Match(raw.TrimEnd('\r'));
                if (match.Success)
                    map[match.Groups[1].Value] = Unquote(match.Groups[2].Value);
            }
            return map;
        }

        /// <summary>
        /// Меняет значения всех присвоений указанных ключей, недостающие ключи дописывает в конец.
        /// </summary>
        public static string SetValues(string text, IReadOnlyDictionary<string, string> changes)
        {
            if (text.Length > 0 && text[0] == '﻿')
                text = text[1..];

            var newline = text.Contains("\r\n") ? "\r\n" : text.Contains('\n') ? "\n" : Environment.NewLine;
            var parts = text.Split('\n');
            var pending = new Dictionary<string, string>(changes, StringComparer.Ordinal);

            for (var i = 0; i < parts.Length; i++)
            {
                var hasCr = parts[i].EndsWith('\r');
                var content = hasCr ? parts[i][..^1] : parts[i];
                var match = Assignment.Match(content);
                if (!match.Success)
                    continue;

                var key = match.Groups[1].Value;
                if (!changes.TryGetValue(key, out var value))
                    continue;

                parts[i] = $"{key}={value}" + (hasCr ? "\r" : "");
                pending.Remove(key);
            }

            var result = string.Join('\n', parts);
            if (pending.Count == 0)
                return result;

            var sb = new StringBuilder(result);
            if (result.Length > 0 && !result.EndsWith('\n'))
                sb.Append(newline);
            foreach (var (key, value) in pending)
                sb.Append(key).Append('=').Append(value).Append(newline);
            return sb.ToString();
        }

        public static string SetValue(string text, string key, string value) =>
            SetValues(text, new Dictionary<string, string> { [key] = value });

        /// <summary>Значение попадает в .env без кавычек, поэтому пробелы и символы подстановки compose недопустимы.</summary>
        public static bool IsSafeValue(string value)
        {
            foreach (var c in value)
            {
                if (c < 0x21 || c > 0x7E || c is '"' or '\'' or '$' or '#' or '\\' or '`')
                    return false;
            }
            return true;
        }

        /// <summary>Имя адреса Tuna: латиница в нижнем регистре, цифры и дефис; пустое значение допустимо.</summary>
        public static bool IsValidSubdomain(string value) =>
            value.Length == 0 || SubdomainPattern.IsMatch(value);

        private static string Unquote(string raw)
        {
            var value = raw.Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                return value[1..^1];

            var comment = value.IndexOf(" #", StringComparison.Ordinal);
            return comment >= 0 ? value[..comment].TrimEnd() : value;
        }
    }
}
