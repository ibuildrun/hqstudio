using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using Xunit;

namespace HQStudio.Setup.Tests;

public class LogSanitizerTests
{
    private const string Profile = @"C:\Users\Ivan";

    [Theory]
    [InlineData("ADMIN_PASSWORD=Sunny-Day-2026", "Sunny-Day-2026")]
    [InlineData("POSTGRES_PASSWORD=abcDEF123456", "abcDEF123456")]
    [InlineData("JWT_KEY=averylongrandomjwtkeyvalue1234567890", "averylongrandomjwtkeyvalue1234567890")]
    [InlineData("TUNA_TOKEN=tuna-secret-token-777", "tuna-secret-token-777")]
    [InlineData("SERVICE_API_KEY=AIzaSyTestKey123456", "AIzaSyTestKey123456")]
    [InlineData("api_key: \"quoted-secret-value\"", "quoted-secret-value")]
    [InlineData("--password hunter22222", "hunter22222")]
    [InlineData("--token=abc123xyz789", "abc123xyz789")]
    public void Sanitize_RedactsSecretAssignments(string line, string secret)
    {
        var result = LogSanitizer.Sanitize(line, null, Profile);

        result.Should().NotContain(secret);
        result.Should().Contain(LogSanitizer.Redacted);
    }

    [Fact]
    public void Sanitize_RedactsJwtAndBearerTokens()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dBjftJeZ4CVPmB92K27uhbUJU1p1r";

        var result = LogSanitizer.Sanitize($"got {jwt} and Authorization: Bearer abc.def.ghi-123", null, Profile);

        result.Should().NotContain("eyJhbGci").And.NotContain("abc.def.ghi-123");
    }

    [Fact]
    public void Sanitize_RedactsPasswordsInUrls()
    {
        LogSanitizer.Sanitize("postgres://hqstudio:SuperSecret99@db:5432/hqstudio", null, Profile)
            .Should().NotContain("SuperSecret99");
    }

    [Fact]
    public void Sanitize_ReplacesTheUserProfilePath()
    {
        var result = LogSanitizer.Sanitize(@"copy to C:\Users\Ivan\AppData\Local\Programs\HQ Studio and c:/users/ivan/x", null, Profile);

        result.Should().NotContain("Ivan");
        result.Should().Contain(@"%USERPROFILE%\AppData\Local\Programs\HQ Studio");
    }

    [Fact]
    public void Sanitize_MasksExactSecretsTypedInTheWizardEvenWithoutAKeyword()
    {
        var masker = new SecretMasker();
        masker.Add("Sunny-Day-2026");
        masker.Add("tuna-secret-token-777");

        var result = LogSanitizer.Sanitize("login failed for Sunny-Day-2026 using tuna-secret-token-777", masker, Profile);

        result.Should().NotContain("Sunny-Day-2026").And.NotContain("tuna-secret-token-777");
    }

    [Fact]
    public void Sanitize_LeavesOrdinaryLinesAlone()
    {
        LogSanitizer.Sanitize(" 3c6d4a1b9e2f Downloading [=>      ]  2.1MB/29MB", null, Profile)
            .Should().Be(" 3c6d4a1b9e2f Downloading [=>      ]  2.1MB/29MB");
    }

    [Fact]
    public void SecretMasker_IgnoresTooShortSecrets()
    {
        var masker = new SecretMasker();
        masker.Add("ab");

        masker.Mask("about").Should().Be("about");
    }
}

public class SetupLogTests
{
    [Fact]
    public void Write_MasksSecretsInTheFileAndInTheEvent()
    {
        using var temp = new TempDir();
        var path = temp.Combine("setup.log");
        var log = new SetupLog(path);
        log.Masker.Add("Sunny-Day-2026");
        var seen = new List<string>();
        log.LineWritten += seen.Add;

        log.Write("creating admin with Sunny-Day-2026");

        File.ReadAllText(path).Should().NotContain("Sunny-Day-2026").And.Contain("[REDACTED]");
        seen.Should().ContainSingle().Which.Should().NotContain("Sunny-Day-2026");
        log.Snapshot().Should().ContainSingle().Which.Should().NotContain("Sunny-Day-2026");
    }

    [Fact]
    public void Write_IgnoresBlankLines()
    {
        var log = new SetupLog(null);

        log.Write("");
        log.Write("   ");

        log.Snapshot().Should().BeEmpty();
    }
}

public class IssueReportTests
{
    private static IssueReportContext Context(string technical = "InstallException: docker compose pull failed") =>
        new("1.20.0", StageId.Pull, FailureKind.Network, technical, Simulated: false);

    [Fact]
    public void Build_PointsToTheNewIssuePageWithTitleBodyAndLabel()
    {
        var link = IssueReport.Build(Context(), new[] { "line one", "line two" });

        link.Url.Should().StartWith("https://github.com/ibuildrun/hqstudio/issues/new?title=");
        link.Url.Should().Contain("&labels=from-app");
        link.Url.Should().Contain("&body=");
        Uri.UnescapeDataString(link.Url).Should().Contain("Скачивание сайта");
        link.Body.Should().Contain("1.20.0").And.Contain("line one").And.Contain("line two");
        link.Title.Should().Contain("Скачивание сайта");
    }

    [Fact]
    public void Build_SanitizesSecretsAndThePersonalPath()
    {
        var masker = new SecretMasker();
        masker.Add("Sunny-Day-2026");
        var lines = new[]
        {
            "ADMIN_PASSWORD=Sunny-Day-2026",
            "TUNA_TOKEN=tuna-secret-token-777",
            @"copied to C:\Users\Ivan\AppData\Local\HQStudio",
            "JWT_KEY=averylongrandomjwtkeyvalue1234567890"
        };

        var link = IssueReport.Build(Context("password=Sunny-Day-2026"), lines, masker, @"C:\Users\Ivan");

        var all = Uri.UnescapeDataString(link.Url);
        all.Should().NotContain("Sunny-Day-2026");
        all.Should().NotContain("tuna-secret-token-777");
        all.Should().NotContain("averylongrandomjwtkeyvalue1234567890");
        all.Should().NotContain("Ivan");
        all.Should().Contain("%USERPROFILE%");
    }

    [Fact]
    public void Build_KeepsTheLinkShortAndKeepsTheNewestLogLines()
    {
        var lines = Enumerable.Range(1, 400).Select(i => $"line {i:000} " + new string('x', 80)).ToList();

        var link = IssueReport.Build(Context(), lines);

        link.Url.Length.Should().BeLessThanOrEqualTo(IssueReport.MaxUrlLength);
        link.Truncated.Should().BeTrue();
        link.Body.Should().Contain("line 400").And.NotContain("line 001 ");
    }

    [Fact]
    public void Build_ShortLogIsNotTruncated()
    {
        var link = IssueReport.Build(Context(), new[] { "a", "b" });

        link.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Build_FenceInTheLogCannotBreakTheMarkdown()
    {
        var link = IssueReport.Build(Context(), new[] { "```", "inject" });

        link.Body.Split("```").Length.Should().Be(5, "two fenced blocks only: technical message and log tail");
    }
}

public class FailureAnalyzerTests
{
    [Theory]
    [InlineData("Error response from daemon: Get \"https://ghcr.io/v2/\": dial tcp: lookup ghcr.io: no such host", FailureKind.Network)]
    [InlineData("net/http: TLS handshake timeout", FailureKind.Network)]
    [InlineData("failed to resolve reference: failed to do request: i/o timeout", FailureKind.Network)]
    [InlineData("toomanyrequests: You have reached your pull rate limit", FailureKind.Network)]
    [InlineData("error during connect: this error may indicate that the docker daemon is not running", FailureKind.DockerNotRunning)]
    [InlineData("Cannot connect to the Docker daemon at npipe:////./pipe/docker_engine", FailureKind.DockerNotRunning)]
    [InlineData("Bind for 127.0.0.1:8080 failed: port is already allocated", FailureKind.PortBusy)]
    [InlineData("listen tcp 127.0.0.1:8080: bind: Only one usage of each socket address", FailureKind.PortBusy)]
    [InlineData("An attempt was made to access a socket in a way forbidden by its access permissions", FailureKind.PortBusy)]
    [InlineData("write /var/lib/docker/x: no space left on device", FailureKind.DiskFull)]
    [InlineData("docker: 'compose' is not a docker command.", FailureKind.ComposeMissing)]
    [InlineData("dependency failed to start: container hqstudio-api-1 is unhealthy", FailureKind.HealthTimeout)]
    [InlineData("something completely different", FailureKind.Unknown)]
    [InlineData("", FailureKind.Unknown)]
    public void ClassifyOutput_MapsCommonCauses(string output, FailureKind expected)
    {
        FailureAnalyzer.ClassifyOutput(output).Should().Be(expected);
    }

    [Fact]
    public void Analyze_UsesTheKnownKindAndRussianTexts()
    {
        var info = FailureAnalyzer.Analyze(StageId.Pull, new InstallException(FailureKind.Network, "pull failed", "dial tcp"));

        info.Kind.Should().Be(FailureKind.Network);
        info.Stage.Should().Be(StageId.Pull);
        info.Hint.Should().Contain("VPN");
        info.Technical.Should().Contain("pull failed").And.Contain("dial tcp");
    }

    [Fact]
    public void Analyze_ClassifiesUnknownInstallExceptionsByTheirOutput()
    {
        var info = FailureAnalyzer.Analyze(StageId.Start, new InstallException(FailureKind.Unknown, "failed", "port is already allocated"));

        info.Kind.Should().Be(FailureKind.PortBusy);
        info.Message.Should().Contain("8080-8090");
    }

    [Fact]
    public void Analyze_DiskFullIoExceptionIsRecognised()
    {
        var info = FailureAnalyzer.Analyze(StageId.Prepare, new IOException("There is not enough space on the disk.", unchecked((int)0x80070070)));

        info.Kind.Should().Be(FailureKind.DiskFull);
    }

    [Fact]
    public void Analyze_SharingViolationMeansFilesAreLocked()
    {
        var info = FailureAnalyzer.Analyze(StageId.Prepare, new IOException("in use", unchecked((int)0x80070020)));

        info.Kind.Should().Be(FailureKind.FilesLocked);
    }

    [Fact]
    public void Analyze_HttpErrorsAreNetworkProblems()
    {
        FailureAnalyzer.Analyze(StageId.Pull, new HttpRequestException("no route")).Kind.Should().Be(FailureKind.Network);
    }

    [Fact]
    public void Analyze_UnknownExceptionsGetAFriendlyGenericMessage()
    {
        var info = FailureAnalyzer.Analyze(StageId.Shortcuts, new InvalidOperationException("boom"));

        info.Kind.Should().Be(FailureKind.Unknown);
        info.Title.Should().Be("Что-то пошло не так");
        info.Technical.Should().Contain("InvalidOperationException");
    }

    [Theory]
    [InlineData(FailureKind.DockerNotRunning)]
    [InlineData(FailureKind.Network)]
    [InlineData(FailureKind.PortBusy)]
    [InlineData(FailureKind.DiskFull)]
    [InlineData(FailureKind.FilesLocked)]
    [InlineData(FailureKind.HealthTimeout)]
    [InlineData(FailureKind.PayloadMissing)]
    public void Describe_AlwaysGivesTitleMessageAndHintInRussian(FailureKind kind)
    {
        var (title, message, hint) = FailureAnalyzer.Describe(kind, StageId.Pull, "raw");

        title.Should().NotBeNullOrWhiteSpace();
        message.Should().MatchRegex("[А-Яа-я]");
        hint.Should().MatchRegex("[А-Яа-я]");
    }
}
