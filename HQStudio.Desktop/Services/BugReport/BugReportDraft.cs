using System.Text;
using System.Text.RegularExpressions;

namespace HQStudio.Services.BugReport
{
    public sealed record BugReportDraft(string Title, string Body);

    public static class BugReportDraftBuilder
    {
        public const int MaxTitleLength = 200;
        public const int MaxBodyLength = 60000;
        public const int CrashMessageChars = 80;

        public static string BuildCrashTitle(Exception crash) =>
            $"Crash: {crash.GetType().Name}: {Truncate(OneLine(crash.Message), CrashMessageChars)}";

        /// <param name="userProfileOverride">Только для тестов: какой путь профиля считать личным.</param>
        public static BugReportDraft Build(string? userTitle, string? description, DiagnosticsSnapshot snapshot,
            bool includeDiagnostics, Exception? crash, string? userProfileOverride = null)
        {
            var title = ResolveTitle(userTitle, description, crash);
            // TextBox отдаёт \r\n, StringBuilder.AppendLine тоже: приводим всё к \n.
            var body = BuildBody(description, snapshot, includeDiagnostics).Replace("\r\n", "\n").Replace('\r', '\n');

            if (body.Length > MaxBodyLength)
                body = SafeCut(body, MaxBodyLength) + "\n\n_(report truncated)_";

            return new BugReportDraft(
                Clean(title, userProfileOverride),
                Clean(body, userProfileOverride));
        }

        private static string Clean(string text, string? userProfileOverride) =>
            userProfileOverride == null
                ? DiagnosticsSanitizer.Sanitize(text)
                : DiagnosticsSanitizer.Sanitize(text, userProfileOverride);

        private static string ResolveTitle(string? userTitle, string? description, Exception? crash)
        {
            var title = OneLine(userTitle);
            if (title.Length == 0 && crash != null)
                title = BuildCrashTitle(crash);

            if (title.Length == 0)
            {
                var firstLine = (description ?? "")
                    .Split('\n')
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => l.Length > 0);
                title = OneLine(firstLine);
            }

            if (title.Length == 0)
                title = "Report from HQStudio app";

            return Truncate(title, MaxTitleLength);
        }

        private static string BuildBody(string? description, DiagnosticsSnapshot s, bool includeDiagnostics)
        {
            var sb = new StringBuilder();
            sb.AppendLine(BugReportConfig.ReportMarker);
            sb.AppendLine();
            sb.AppendLine("## Description");
            sb.AppendLine();
            var text = (description ?? "").Trim();
            sb.AppendLine(text.Length == 0 ? "_(no description)_" : text);
            sb.AppendLine();

            sb.AppendLine("## Environment");
            sb.AppendLine();
            sb.AppendLine($"- App version: {s.AppVersion}");
            sb.AppendLine($"- Windows: {s.WindowsVersion}");
            sb.AppendLine($"- .NET: {s.DotNetVersion}");
            if (!string.IsNullOrWhiteSpace(s.ServerVersion))
                sb.AppendLine($"- Server version: {s.ServerVersion}");
            if (!string.IsNullOrWhiteSpace(s.ApiUrl))
                sb.AppendLine($"- API URL: {s.ApiUrl}");
            if (s.Install != null)
            {
                if (!string.IsNullOrWhiteSpace(s.Install.WebUrl))
                    sb.AppendLine($"- Install web URL: {s.Install.WebUrl}");
                if (!string.IsNullOrWhiteSpace(s.Install.ApiUrl))
                    sb.AppendLine($"- Install API URL: {s.Install.ApiUrl}");
                if (!string.IsNullOrWhiteSpace(s.Install.ServerDir))
                    sb.AppendLine($"- Install server dir: {s.Install.ServerDir}");
            }

            if (!includeDiagnostics) return sb.ToString().TrimEnd() + "\n";

            sb.AppendLine();
            sb.AppendLine("## Diagnostics");

            var any = false;
            any |= AppendSection(sb, "Exception", s.ExceptionText);
            any |= AppendSection(sb, $"crash.log (last {DiagnosticsCollector.CrashLogLines} lines)", s.CrashLogTail);
            any |= AppendSection(sb, "docker compose ps", s.DockerComposePs);
            if (!any)
            {
                sb.AppendLine();
                sb.AppendLine("_Nothing to attach: no crash.log and no install.json were found._");
            }

            return sb.ToString().TrimEnd() + "\n";
        }

        private static bool AppendSection(StringBuilder sb, string heading, string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var fence = Fence(content);
            sb.AppendLine();
            sb.AppendLine($"### {heading}");
            sb.AppendLine();
            sb.AppendLine(fence);
            sb.AppendLine(content.TrimEnd());
            sb.AppendLine(fence);
            return true;
        }

        // Забор длиннее любой серии обратных кавычек внутри лога, иначе лог "вылезет" из блока.
        private static string Fence(string content)
        {
            var longest = 0;
            foreach (Match m in Regex.Matches(content, "`+"))
                longest = Math.Max(longest, m.Length);
            return new string('`', Math.Max(3, longest + 1));
        }

        private static string OneLine(string? text) =>
            string.IsNullOrWhiteSpace(text) ? "" : Regex.Replace(text, @"\s+", " ").Trim();

        private static string Truncate(string text, int max) =>
            text.Length <= max ? text : SafeCut(text, max).TrimEnd();

        // Не режем суррогатную пару пополам: иначе Uri.EscapeDataString падает.
        internal static string SafeCut(string text, int length)
        {
            if (length >= text.Length) return text;
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            return text[..length];
        }
    }
}
