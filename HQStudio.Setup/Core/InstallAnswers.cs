namespace HQStudio.Setup.Core;

/// <summary>Everything the user typed or ticked in the wizard.</summary>
public sealed class InstallAnswers
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Password { get; set; } = "";
    public string GeminiKey { get; set; } = "";
    public string TunaToken { get; set; } = "";
    public string TunaSubdomain { get; set; } = "";

    /// <summary>Own domain for the tunnel (full host name, punycode); when set the subdomain is not used.</summary>
    public string TunaDomain { get; set; } = "";
    public bool DesktopShortcut { get; set; } = true;

    /// <summary>The user chose to install Docker later: only the program is installed now.</summary>
    public bool SkipSite { get; set; }

    public string AdminName => $"{FirstName.Trim()} {LastName.Trim()}".Trim();

    public IEnumerable<string> Secrets()
    {
        foreach (var s in new[] { Password, GeminiKey, TunaToken })
        {
            if (string.IsNullOrWhiteSpace(s))
                continue;
            yield return s;
            if (s.Trim() != s)
                yield return s.Trim();
        }
    }
}
