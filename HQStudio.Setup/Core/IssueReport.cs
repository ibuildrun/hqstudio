using System.Text;

namespace HQStudio.Setup.Core;

public sealed record IssueReportContext(
    string InstallerVersion,
    StageId Stage,
    FailureKind Kind,
    string Technical,
    bool Simulated);

public sealed record IssueLink(string Url, string Title, string Body, bool Truncated);

/// <summary>Prefilled "new issue" link for GitHub with a sanitized log tail.</summary>
public static class IssueReport
{
    public const string Repository = "ibuildrun/hqstudio";
    public const string NewIssueUrl = "https://github.com/" + Repository + "/issues/new";
    public const string Label = "from-app";
    public const int MaxUrlLength = 7000;
    public const int MaxLogLines = 120;

    public static string Title(StageId stage) => $"Установщик: ошибка на этапе «{StageNames.Title(stage)}»";

    public static IssueLink Build(
        IssueReportContext context,
        IReadOnlyList<string> logLines,
        SecretMasker? masker = null,
        string? userProfile = null)
    {
        var title = Title(context.Stage);
        var header = BuildHeader(context, masker, userProfile);

        var sanitized = logLines
            .TakeLast(MaxLogLines)
            .Select(l => LogSanitizer.Sanitize(l, masker, userProfile))
            .ToList();

        // Fewer log lines until the whole link fits; the newest lines are the most useful.
        var low = 0;
        var high = sanitized.Count;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (Url(title, Compose(header, sanitized, mid)).Length <= MaxUrlLength)
                low = mid;
            else
                high = mid - 1;
        }

        var body = Compose(header, sanitized, low);
        return new IssueLink(Url(title, body), title, body, low < sanitized.Count);
    }

    private static string BuildHeader(IssueReportContext c, SecretMasker? masker, string? userProfile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Установка HQ Studio завершилась ошибкой.");
        sb.AppendLine();
        sb.AppendLine($"- Версия установщика: {c.InstallerVersion}");
        sb.AppendLine($"- Этап: {StageNames.Title(c.Stage)}");
        sb.AppendLine($"- Причина: {c.Kind}");
        sb.AppendLine($"- Windows: {Environment.OSVersion.Version}");
        if (c.Simulated)
            sb.AppendLine("- Режим: проверочный (--simulate)");
        sb.AppendLine();
        sb.AppendLine("Техническое сообщение:");
        sb.AppendLine("```");
        sb.AppendLine(LogSanitizer.Sanitize(c.Technical, masker, userProfile).Replace("```", "'''"));
        sb.AppendLine("```");
        return sb.ToString();
    }

    private static string Compose(string header, IReadOnlyList<string> lines, int take)
    {
        var sb = new StringBuilder(header);
        sb.AppendLine();
        sb.AppendLine("Конец журнала установки:");
        sb.AppendLine("```");
        foreach (var line in lines.Skip(lines.Count - take))
            sb.AppendLine(line.Replace("```", "'''"));
        sb.AppendLine("```");
        return sb.ToString();
    }

    private static string Url(string title, string body) =>
        $"{NewIssueUrl}?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}&labels={Uri.EscapeDataString(Label)}";
}
