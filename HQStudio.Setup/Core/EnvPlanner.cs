namespace HQStudio.Setup.Core;

public sealed record EnvPlan(string Text, string PostgresPassword, string JwtKey, bool KeptExistingSecrets);

/// <summary>Builds the server .env: new secrets on a fresh install, existing database secrets kept on re-install.</summary>
public static class EnvPlanner
{
    public const string PlaceholderPassword = "change-me";
    public const int PostgresPasswordLength = 24;
    public const int JwtKeyLength = 48;

    public static bool IsRealPostgresPassword(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), PlaceholderPassword, StringComparison.OrdinalIgnoreCase);

    public static bool IsRealJwtKey(string? value) =>
        value is { Length: >= 32 } && !value.StartsWith(PlaceholderPassword, StringComparison.OrdinalIgnoreCase);

    public static EnvPlan Build(
        string? existingText,
        string exampleText,
        InstallAnswers answers,
        int port,
        string version,
        Func<int, string>? secretFactory = null)
    {
        secretFactory ??= SecretGenerator.Create;
        var baseText = string.IsNullOrWhiteSpace(existingText) ? exampleText : existingText!;

        var existingPostgres = EnvFile.Get(baseText, "POSTGRES_PASSWORD");
        var existingJwt = EnvFile.Get(baseText, "JWT_KEY");
        var keepPostgres = IsRealPostgresPassword(existingPostgres);
        var keepJwt = IsRealJwtKey(existingJwt);

        var postgres = keepPostgres ? existingPostgres!.Trim() : secretFactory(PostgresPasswordLength);
        var jwt = keepJwt ? existingJwt!.Trim() : secretFactory(JwtKeyLength);

        var text = baseText;
        text = EnvFile.Set(text, "HQ_PORT", port.ToString());
        text = EnvFile.Set(text, "POSTGRES_PASSWORD", postgres);
        text = EnvFile.Set(text, "JWT_KEY", jwt);
        text = EnvFile.Set(text, "ADMIN_PASSWORD", answers.Password);
        text = EnvFile.Set(text, "ADMIN_NAME", answers.AdminName);
        text = EnvFile.Set(text, "GEMINI_API_KEY", EffectiveGemini(baseText, answers));
        text = EnvFile.Set(text, "TUNA_TOKEN", EffectiveTunaToken(baseText, answers));

        // An own domain replaces the Tuna subdomain, and is also the address the site will be reached at.
        var domain = EffectiveTunaDomain(baseText, answers);
        text = EnvFile.Set(text, "TUNA_DOMAIN", domain);
        text = EnvFile.Set(text, "TUNA_SUBDOMAIN", EffectiveTunaSubdomain(baseText, answers));
        if (domain.Length > 0)
            text = SetPublicUrl(text, PublicUrlFor(domain));

        if (!string.IsNullOrWhiteSpace(version))
            text = EnvFile.Set(text, "HQSTUDIO_VERSION", version);

        return new EnvPlan(text, postgres, jwt, keepPostgres && keepJwt);
    }

    // A blank answer keeps what is already configured, so a re-install never silently drops a key.
    public static string EffectiveGemini(string? envText, InstallAnswers answers) =>
        Pick(answers.GeminiKey, envText, "GEMINI_API_KEY");

    public static string EffectiveTunaToken(string? envText, InstallAnswers answers) =>
        Pick(answers.TunaToken, envText, "TUNA_TOKEN");

    public static string EffectiveTunaDomain(string? envText, InstallAnswers answers) =>
        Validators.NormalizeDomain(Pick(answers.TunaDomain, envText, "TUNA_DOMAIN"));

    /// <summary>Empty whenever an own domain is in force.</summary>
    public static string EffectiveTunaSubdomain(string? envText, InstallAnswers answers) =>
        EffectiveTunaDomain(envText, answers).Length > 0
            ? ""
            : Pick(answers.TunaSubdomain, envText, "TUNA_SUBDOMAIN").ToLowerInvariant();

    public static string PublicUrlFor(string domain) => "https://" + domain;

    public static string BlankAdminPassword(string text) => EnvFile.Set(text, "ADMIN_PASSWORD", "");

    public static string SetPort(string text, int port) => EnvFile.Set(text, "HQ_PORT", port.ToString());

    public static string SetPublicUrl(string text, string url) => EnvFile.Set(text, "PUBLIC_URL", url);

    public static int GetPort(string text, int fallback = 8080)
    {
        var value = EnvFile.Get(text, "HQ_PORT");
        return int.TryParse(value, out var port) && port is > 0 and < 65536 ? port : fallback;
    }

    private static string Pick(string answer, string? envText, string key)
    {
        if (!string.IsNullOrWhiteSpace(answer))
            return answer.Trim();
        var existing = envText == null ? null : EnvFile.Get(envText, key);
        return existing?.Trim() ?? "";
    }
}
