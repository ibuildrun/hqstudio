using System.Text;

namespace HQStudio.Services.BugReport
{
    public sealed record IssueUrlResult(string Url, bool Truncated);

    /// <summary>
    /// Ссылка на страницу создания issue с предзаполненным текстом (запасной способ без OAuth).
    /// </summary>
    public static class IssueUrlBuilder
    {
        // Итоговая ссылка строго короче этого значения: длинные URL режут браузеры и сам GitHub.
        public const int MaxUrlLength = 7000;
        public const int MaxTitleLength = 256;

        public const string TruncationNote =
            "_Diagnostics were truncated to fit the URL. The full report text was copied to the clipboard - please paste it here._";

        public static IssueUrlResult Build(string title, string body)
        {
            title = BugReportDraftBuilder.SafeCut(title ?? "", MaxTitleLength);
            body ??= "";

            var head = $"{BugReportConfig.NewIssueWebUrl}?title={Escape(title)}&body=";
            var tail = $"&labels={Escape(BugReportConfig.IssueLabel)}";
            var budget = MaxUrlLength - 1 - head.Length - tail.Length;

            var encodedBody = Escape(body);
            if (encodedBody.Length <= budget)
                return new IssueUrlResult(head + encodedBody + tail, false);

            // Кодированная длина растёт вместе с длиной префикса, поэтому ищем бинарным поиском.
            var low = 0;
            var high = body.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (Escape(Compose(body, mid)).Length <= budget)
                    low = mid;
                else
                    high = mid - 1;
            }

            return new IssueUrlResult(head + Escape(Compose(body, low)) + tail, true);
        }

        private static string Compose(string body, int length)
        {
            var cut = BugReportDraftBuilder.SafeCut(body, length);
            var closing = OpenFence(cut);
            return cut + (closing == null ? "" : "\n" + closing) + "\n\n" + TruncationNote;
        }

        // Если обрезали посреди блока кода, закрываем его, иначе разметка issue поедет.
        private static string? OpenFence(string text)
        {
            string? fence = null;
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimStart();
                var ticks = 0;
                while (ticks < line.Length && line[ticks] == '`') ticks++;
                if (ticks < 3) continue;

                if (fence == null)
                    fence = new string('`', ticks);
                else if (ticks >= fence.Length && line[ticks..].Trim().Length == 0)
                    fence = null;
            }

            return fence;
        }

        private static string Escape(string value) => Uri.EscapeDataString(value);
    }
}
