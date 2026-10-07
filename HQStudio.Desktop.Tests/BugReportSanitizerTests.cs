using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportSanitizerTests
{
    private const string Profile = @"C:\Users\Ivan Petrov";

    private static string Clean(string text, string? profile = Profile) =>
        DiagnosticsSanitizer.Sanitize(text, profile);

    [Theory]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData("gho_abcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData("ghu_abcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData("ghs_abcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData("ghr_abcdefghijklmnopqrstuvwxyz0123456789")]
    [InlineData("github_pat_11ABCDEFG0abcdefghijklmnopqrstuvwxyz_0123456789ABCDEF")]
    public void RedactsGitHubTokens(string token)
    {
        var result = Clean($"saved token {token} in file");

        result.Should().NotContain(token);
        result.Should().Contain(DiagnosticsSanitizer.Redacted);
    }

    [Fact]
    public void RedactsGitHubTokenWithoutKeyContext()
    {
        var result = Clean("see ghp_abcdefghijklmnopqrstuvwxyz0123456789 here");

        result.Should().Be("see [REDACTED] here");
    }

    [Theory]
    [InlineData("Authorization: Bearer abc.DEF-123_xyz~+/=")]
    [InlineData("authorization: bearer abc.DEF-123_xyz~+/=")]
    public void RedactsBearerValues(string line)
    {
        var result = Clean(line);

        result.Should().NotContain("abc.DEF-123");
        result.Should().Contain("Bearer " + DiagnosticsSanitizer.Redacted, because: "scheme stays so the header is still readable");
    }

    [Fact]
    public void RedactsJwt()
    {
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var result = Clean($"user sent {jwt} to server");

        result.Should().Be("user sent [REDACTED] to server");
    }

    [Theory]
    [InlineData("password=hunter2", "hunter2")]
    [InlineData("Password: hunter2", "hunter2")]
    [InlineData("passwd=hunter2", "hunter2")]
    [InlineData("pwd=hunter2", "hunter2")]
    [InlineData("secret=hunter2", "hunter2")]
    [InlineData("client_secret = hunter2", "hunter2")]
    [InlineData("token=hunter2", "hunter2")]
    [InlineData("access_token: hunter2", "hunter2")]
    [InlineData("api_key=hunter2", "hunter2")]
    [InlineData("api-key=hunter2", "hunter2")]
    [InlineData("apikey=hunter2", "hunter2")]
    [InlineData("JWT_KEY=hunter2", "hunter2")]
    [InlineData("TUNA_TOKEN=hunter2", "hunter2")]
    [InlineData("GEMINI_API_KEY=hunter2", "hunter2")]
    [InlineData("POSTGRES_PASSWORD=hunter2", "hunter2")]
    [InlineData("POSTGRES_PASSWORD: hunter2", "hunter2")]
    public void RedactsKeyValueForms(string line, string secret)
    {
        var result = Clean(line);

        result.Should().NotContain(secret);
        result.Should().EndWith(DiagnosticsSanitizer.Redacted);
    }

    [Theory]
    [InlineData("{\"password\": \"hunter2\"}", "hunter2")]
    [InlineData("{\"password\":\"hunter2\",\"user\":\"ivan\"}", "hunter2")]
    [InlineData("{\"JWT_KEY\": \"hunter2 with spaces\"}", "hunter2")]
    [InlineData("{\"apiKey\": \"hunter2\"}", "hunter2")]
    [InlineData("{\"token\": \"a\\\"quoted\\\"secret\"}", "quoted")]
    [InlineData("{'secret': 'hunter2'}", "hunter2")]
    public void RedactsJsonStyleValues(string json, string secret)
    {
        var result = Clean(json);

        result.Should().NotContain(secret);
        result.Should().Contain(DiagnosticsSanitizer.Redacted);
    }

    [Fact]
    public void JsonStyleKeepsOtherFields()
    {
        var result = Clean("{\"password\":\"hunter2\",\"user\":\"ivan\"}");

        result.Should().Be("{\"password\":\"[REDACTED]\",\"user\":\"ivan\"}");
    }

    [Fact]
    public void RedactsConnectionStringPassword()
    {
        var result = Clean("Server=db;Database=hq;User Id=sa;Password=p@ss w0rd!;Encrypt=true");

        result.Should().NotContain("p@ss");
        result.Should().Contain("Password=[REDACTED];Encrypt=true");
        result.Should().Contain("Server=db;Database=hq;User Id=sa;");
    }

    [Fact]
    public void RedactsQuotedConnectionStringPassword()
    {
        var result = Clean("Host=db;Pwd=\"top;secret\";Port=5432");

        result.Should().NotContain("top;secret");
        result.Should().Contain("Port=5432");
    }

    [Fact]
    public void RedactsPasswordInUrl()
    {
        var result = Clean("postgres://hq:s3cr3tpass@db:5432/hq");

        result.Should().Be("postgres://hq:[REDACTED]@db:5432/hq");
    }

    [Fact]
    public void RedactsQueryStringToken()
    {
        var result = Clean("GET /api/x?token=abc123&page=2");

        result.Should().Be("GET /api/x?token=[REDACTED]&page=2");
    }

    [Fact]
    public void RedactsXmlElementValue()
    {
        var result = Clean("<Password>hunter2</Password><User>ivan</User>");

        result.Should().Be("<Password>[REDACTED]</Password><User>ivan</User>");
    }

    [Theory]
    [InlineData(@"C:\Users\Ivan Petrov\AppData\Local\HQStudio\crash.log", @"%USERPROFILE%\AppData\Local\HQStudio\crash.log")]
    [InlineData(@"c:\users\ivan petrov\AppData\x", @"%USERPROFILE%\AppData\x")]
    [InlineData("C:/Users/Ivan Petrov/AppData/x", "%USERPROFILE%/AppData/x")]
    [InlineData(@"{""path"":""C:\\Users\\Ivan Petrov\\x""}", @"{""path"":""%USERPROFILE%\\x""}")]
    public void ReplacesUserProfilePath(string input, string expected)
    {
        Clean(input).Should().Be(expected);
    }

    [Fact]
    public void DoesNotReplaceLongerFolderWithSamePrefix()
    {
        var result = Clean(@"C:\Users\Ivan Petrov2\x");

        result.Should().Be(@"C:\Users\Ivan Petrov2\x");
    }

    [Fact]
    public void ProfilePathWithTrailingSlashStillMatches()
    {
        DiagnosticsSanitizer.Sanitize(@"C:\Users\Ivan\docs", @"C:\Users\Ivan\")
            .Should().Be(@"%USERPROFILE%\docs");
    }

    [Fact]
    public void SingleArgumentOverloadUsesRealUserProfile()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(profile)) return;

        DiagnosticsSanitizer.Sanitize($"file in {profile}\\x").Should().Be(@"file in %USERPROFILE%\x");
    }

    [Fact]
    public void LeavesOrdinaryTextUntouched()
    {
        const string text = "Не открывается карточка клиента.\nError at OrdersViewModel.Load() line 42\nTokenizer is fine; passwords are hidden.";

        Clean(text).Should().Be(text);
    }

    [Fact]
    public void WordTokenInProseIsNotMangled()
    {
        Clean("The token is invalid, please retry").Should().Be("The token is invalid, please retry");
    }

    [Fact]
    public void IsIdempotent()
    {
        var once = Clean("password=hunter2 Authorization: Bearer abc.def ghp_abcdefghijklmnopqrstuvwxyz0123456789");

        Clean(once).Should().Be(once);
    }

    [Fact]
    public void HandlesNullAndEmpty()
    {
        DiagnosticsSanitizer.Sanitize(null).Should().BeEmpty();
        DiagnosticsSanitizer.Sanitize("").Should().BeEmpty();
    }

    [Fact]
    public void RedactsMultipleSecretsOnManyLines()
    {
        var input = string.Join("\n", new[]
        {
            "JWT_KEY=aaaaaaaa",
            "TUNA_TOKEN=bbbbbbbb",
            "GEMINI_API_KEY=cccccccc",
            "POSTGRES_PASSWORD=dddddddd",
            "ok line"
        });

        var result = Clean(input);

        result.Should().NotContainAny("aaaaaaaa", "bbbbbbbb", "cccccccc", "dddddddd");
        result.Should().EndWith("ok line");
    }

    [Fact]
    public void LargeInputFinishesQuickly()
    {
        var input = string.Concat(Enumerable.Repeat("some log line with ordinary words and numbers 12345 and token word\n", 5000));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var result = Clean(input);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        result.Length.Should().Be(input.Length);
    }
}

