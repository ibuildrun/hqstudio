using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    /// <summary>Прячет пароли, токены и ключи в тексте журналов, прежде чем показать его на экране или скопировать.</summary>
    public static class SiteSecretSanitizer
    {
        public const string Mask = "***";

        private const string NameChars = @"[A-Za-z0-9_.\-]";
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        // Имя, похожее на секрет (Password, JWT_KEY, Jwt__Key, TUNA_TOKEN...), знак = или : и значение.
        private static readonly Regex KeyValue = new(
            @"(?<![A-Za-z0-9_.\-])(" + NameChars + @"*?(?:password|passwd|pwd|secret|token|credential|jwt|api[_\-]?key|private[_\-]?key|access[_\-]?key|[_\-.]key)" +
            NameChars + @"*)([""']?\s*[:=]\s*[""']?)([^\s""';,&]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout);

        private static readonly Regex Bearer = new(@"\b(Bearer|Basic)\s+[A-Za-z0-9._\-+/=]{6,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout);

        private static readonly Regex Jwt = new(@"eyJ[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]*",
            RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout);

        private static readonly Regex GoogleKey = new(@"AIza[0-9A-Za-z_\-]{20,}",
            RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout);

        private static readonly Regex UrlUserInfo = new(@"(://[^/\s:@]+:)([^/\s@]+)(@)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled, Timeout);

        /// <param name="text">Исходный текст журнала.</param>
        /// <param name="knownSecrets">Настоящие значения из .env: заменяются дословно, где бы ни встретились.</param>
        public static string Sanitize(string? text, IEnumerable<string>? knownSecrets = null)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            try
            {
                var result = text;
                if (knownSecrets != null)
                {
                    foreach (var secret in knownSecrets.Where(s => s.Length >= 4).Distinct().OrderByDescending(s => s.Length))
                        result = result.Replace(secret, Mask, StringComparison.Ordinal);
                }

                result = UrlUserInfo.Replace(result, "$1" + Mask + "$3");
                result = Bearer.Replace(result, "$1 " + Mask);
                result = Jwt.Replace(result, Mask);
                result = GoogleKey.Replace(result, Mask);
                result = KeyValue.Replace(result, "$1$2" + Mask);
                return result;
            }
            catch (RegexMatchTimeoutException)
            {
                return "[Журнал не показан: не удалось надёжно скрыть из него пароли и ключи.]";
            }
        }
    }
}
