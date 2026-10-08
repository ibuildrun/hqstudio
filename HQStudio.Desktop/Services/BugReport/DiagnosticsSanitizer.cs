using System.Text.RegularExpressions;

namespace HQStudio.Services.BugReport
{
    /// <summary>
    /// Вырезает секреты и личные пути из текста, который уйдёт на GitHub. Чистая функция.
    /// </summary>
    public static class DiagnosticsSanitizer
    {
        public const string Redacted = "[REDACTED]";

        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(3);

        // Подстроки покрывают и JWT_KEY, TUNA_TOKEN, POSTGRES_PASSWORD и любой другой ..._API_KEY (ключ с префиксом/суффиксом).
        private const string Keywords = @"password|passwd|pwd|secret|token|api[_-]?key|jwt[_-]?key";

        private static Regex Make(string pattern, RegexOptions extra = RegexOptions.None) =>
            new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | extra, MatchTimeout);

        private static readonly Regex GitHubTokens = Make(
            @"(?<![A-Za-z0-9])(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})");

        private static readonly Regex BearerToken = Make(
            @"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase);

        private static readonly Regex Jwt = Make(
            @"\beyJ[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]{5,}(?:\.[A-Za-z0-9_\-]*)?");

        private static readonly Regex UrlCredentials = Make(
            @"(?<=://)(?<user>[^/\s:@]+):(?<pass>[^@\s/]+)@");

        private static readonly Regex KeyValue = Make(
            @"(?<![A-Za-z0-9_])(?<pre>[A-Za-z0-9_.\-]{0,40}(?:" + Keywords + @")[A-Za-z0-9_.\-]{0,40}[""']?\s*[:=]\s*)" +
            // semi: значение до ";" с пробелами внутри (строка подключения); raw: значение до пробела.
            @"(?:(?<dq>""(?:[^""\\\r\n]|\\.)*"")|(?<sq>'(?:[^'\\\r\n]|\\.)*')" +
            @"|(?<semi>(?!\[REDACTED\])[^;""'\r\n]+(?=;))|(?<raw>(?!\[REDACTED\])[^\s;""',}&]+))",
            RegexOptions.IgnoreCase);

        private static readonly Regex XmlElement = Make(
            @"(?<open><[A-Za-z0-9_.:\-]*(?:" + Keywords + @")[A-Za-z0-9_.:\-]*>)[^<]+(?=</)",
            RegexOptions.IgnoreCase);

        public static string Sanitize(string? text) =>
            Sanitize(text, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        public static string Sanitize(string? text, string? userProfile)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            try
            {
                var result = text;
                result = ReplaceProfilePath(result, userProfile);
                result = GitHubTokens.Replace(result, Redacted);
                result = Jwt.Replace(result, Redacted);
                result = BearerToken.Replace(result, "Bearer " + Redacted);
                result = UrlCredentials.Replace(result, "${user}:" + Redacted + "@");
                result = KeyValue.Replace(result, m =>
                {
                    var pre = m.Groups["pre"].Value;
                    if (m.Groups["dq"].Success) return pre + "\"" + Redacted + "\"";
                    if (m.Groups["sq"].Success) return pre + "'" + Redacted + "'";
                    return pre + Redacted;
                });
                result = XmlElement.Replace(result, "${open}" + Redacted);
                return result;
            }
            catch (RegexMatchTimeoutException)
            {
                // Лучше ничего не отправить, чем отправить непрочищенное.
                return "[content withheld: sanitizer timed out]";
            }
        }

        private static string ReplaceProfilePath(string text, string? userProfile)
        {
            if (string.IsNullOrWhiteSpace(userProfile)) return text;

            var trimmed = userProfile.TrimEnd('\\', '/');
            if (trimmed.Length == 0) return text;

            var variants = new[]
            {
                trimmed,
                trimmed.Replace('\\', '/'),
                trimmed.Replace("\\", "\\\\")
            };

            foreach (var variant in variants.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var regex = new Regex(Regex.Escape(variant) + @"(?![A-Za-z0-9_])",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
                text = regex.Replace(text, "%USERPROFILE%");
            }

            return text;
        }
    }
}
