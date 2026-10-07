using System.Globalization;
using System.Text.RegularExpressions;

namespace HQStudio.Setup.Core;

public static class TunnelUrlParser
{
    private static readonly Regex Ansi = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);
    private static readonly Regex Forwarding = new(@"Forwarding\s+(https?://\S+?)\s+->", RegexOptions.Compiled);

    /// <summary>The newest "Forwarding https://... -> ..." address in the tuna log, or null.</summary>
    public static string? Parse(string? log)
    {
        if (string.IsNullOrEmpty(log))
            return null;

        string? last = null;
        foreach (Match m in Forwarding.Matches(Ansi.Replace(log, "")))
            last = m.Groups[1].Value;
        return last;
    }
}

public static class PortSelector
{
    public const int First = 8080;
    public const int Last = 8090;

    /// <summary>The previously used port first (when valid), then First..Last.</summary>
    public static IEnumerable<int> Candidates(int? preferred = null)
    {
        var seen = new HashSet<int>();
        if (preferred is > 0 and < 65536 && seen.Add(preferred.Value))
            yield return preferred.Value;
        for (var p = First; p <= Last; p++)
        {
            if (seen.Add(p))
                yield return p;
        }
    }

    public static int? Select(IEnumerable<int> candidates, Func<int, bool> isUsable)
    {
        foreach (var port in candidates)
        {
            if (isUsable(port))
                return port;
        }
        return null;
    }

    /// <summary>Next port in the range after a conflict on <paramref name="busy"/>, or null when the range is used up.</summary>
    public static int? Next(int busy, Func<int, bool> isFree, ISet<int>? alreadyTried = null)
    {
        for (var p = First; p <= Last; p++)
        {
            if (p == busy || (alreadyTried?.Contains(p) ?? false))
                continue;
            if (isFree(p))
                return p;
        }
        return null;
    }
}

public static class ByteSize
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>"512 КБ", "34,5 МБ", "1,2 ГБ" (decimal units, like the docker output).</summary>
    public static string Format(long bytes)
    {
        if (bytes < 0) bytes = 0;
        if (bytes < 1000) return $"{bytes} Б";
        if (bytes < 1_000_000) return string.Create(Ru, $"{bytes / 1000.0:0} КБ");
        if (bytes < 1_000_000_000) return string.Create(Ru, $"{bytes / 1_000_000.0:0.#} МБ");
        return string.Create(Ru, $"{bytes / 1_000_000_000.0:0.##} ГБ");
    }
}
