using FluentAssertions;
using HQStudio.Services.Site;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class SiteEnvFileTests
{
    private const string Sample =
        "# Настройки HQ Studio\r\n" +
        "HQSTUDIO_VERSION=1.19.6\r\n" +
        "\r\n" +
        "# ключ ИИ\r\n" +
        "GEMINI_API_KEY=\r\n" +
        "TUNA_TOKEN=old-token\r\n" +
        "POSTGRES_PASSWORD=pg=secret\r\n";

    [Fact]
    public void SetValues_ReplacesOnlyTheRequestedLines_KeepingCommentsAndBlanks()
    {
        var result = SiteEnvFile.SetValues(Sample, new Dictionary<string, string>
        {
            ["GEMINI_API_KEY"] = "AIzaNew",
            ["TUNA_TOKEN"] = "new-token"
        });

        result.Should().Be(
            "# Настройки HQ Studio\r\n" +
            "HQSTUDIO_VERSION=1.19.6\r\n" +
            "\r\n" +
            "# ключ ИИ\r\n" +
            "GEMINI_API_KEY=AIzaNew\r\n" +
            "TUNA_TOKEN=new-token\r\n" +
            "POSTGRES_PASSWORD=pg=secret\r\n");
    }

    [Fact]
    public void SetValues_AppendsMissingKeysAtTheEnd()
    {
        var result = SiteEnvFile.SetValues(Sample, new Dictionary<string, string> { ["TUNA_SUBDOMAIN"] = "my-site" });

        result.Should().StartWith(Sample).And.EndWith("TUNA_SUBDOMAIN=my-site\r\n");
    }

    [Fact]
    public void SetValues_FileWithoutTrailingNewline_AppendsOnNewLine()
    {
        var result = SiteEnvFile.SetValue("A=1\nB=2", "C", "3");

        result.Should().Be("A=1\nB=2\nC=3\n");
    }

    [Fact]
    public void SetValues_KeepsLineEndingStyle()
    {
        SiteEnvFile.SetValue("A=1\nB=2\n", "A", "9").Should().Be("A=9\nB=2\n");
        SiteEnvFile.SetValue("A=1\r\nB=2\r\n", "B", "9").Should().Be("A=1\r\nB=9\r\n");
    }

    [Fact]
    public void SetValues_ReplacesEveryDuplicateAssignment()
    {
        SiteEnvFile.SetValue("K=1\nX=2\nK=3\n", "K", "9").Should().Be("K=9\nX=2\nK=9\n");
    }

    [Fact]
    public void SetValues_DoesNotTouchCommentedOutKeys()
    {
        var result = SiteEnvFile.SetValue("# TUNA_TOKEN=old\nOTHER=1\n", "TUNA_TOKEN", "new");

        result.Should().Be("# TUNA_TOKEN=old\nOTHER=1\nTUNA_TOKEN=new\n");
    }

    [Fact]
    public void SetValues_StripsByteOrderMark()
    {
        SiteEnvFile.SetValue("﻿A=1\n", "A", "2").Should().Be("A=2\n");
    }

    [Fact]
    public void SetValues_EmptyFile_CreatesAssignment()
    {
        SiteEnvFile.SetValue("", "A", "1").Should().Be("A=1" + Environment.NewLine);
    }

    [Fact]
    public void SetValues_ValueMayBeEmpty()
    {
        SiteEnvFile.SetValue("TUNA_TOKEN=abc\n", "TUNA_TOKEN", "").Should().Be("TUNA_TOKEN=\n");
    }

    [Fact]
    public void GetValue_ReadsPlainQuotedAndCommentedValues()
    {
        var text = "A=plain\nB=\"quoted value\"\nC='single'\nD=with comment # note\nE=\nexport F=exported\n";

        SiteEnvFile.GetValue(text, "A").Should().Be("plain");
        SiteEnvFile.GetValue(text, "B").Should().Be("quoted value");
        SiteEnvFile.GetValue(text, "C").Should().Be("single");
        SiteEnvFile.GetValue(text, "D").Should().Be("with comment");
        SiteEnvFile.GetValue(text, "E").Should().Be("");
        SiteEnvFile.GetValue(text, "F").Should().Be("exported");
        SiteEnvFile.GetValue(text, "MISSING").Should().BeNull();
    }

    [Fact]
    public void GetValue_ValueContainingEqualsSignIsKeptWhole()
    {
        SiteEnvFile.GetValue("POSTGRES_PASSWORD=pg=secret\n", "POSTGRES_PASSWORD").Should().Be("pg=secret");
    }

    [Fact]
    public void Parse_ReturnsAllAssignments()
    {
        var map = SiteEnvFile.Parse(Sample);

        map.Should().ContainKey("HQSTUDIO_VERSION").WhoseValue.Should().Be("1.19.6");
        map.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("AIzaSyD-example_key123", true)]
    [InlineData("tuna_abc.def-123", true)]
    [InlineData("", true)]
    [InlineData("has space", false)]
    [InlineData("tab\there", false)]
    [InlineData("quo\"te", false)]
    [InlineData("dollar$sign", false)]
    [InlineData("hash#tag", false)]
    [InlineData("back\\slash", false)]
    [InlineData("кириллица", false)]
    public void IsSafeValue_RejectsCharactersThatBreakEnvFiles(string value, bool expected)
    {
        SiteEnvFile.IsSafeValue(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("mysite", true)]
    [InlineData("my-site-2", true)]
    [InlineData("a", true)]
    [InlineData("-mysite", false)]
    [InlineData("mysite-", false)]
    [InlineData("MySite", false)]
    [InlineData("my_site", false)]
    [InlineData("my site", false)]
    [InlineData("сайт", false)]
    public void IsValidSubdomain_AcceptsLowercaseLatinDigitsAndDash(string value, bool expected)
    {
        SiteEnvFile.IsValidSubdomain(value).Should().Be(expected);
    }

    [Fact]
    public void IsValidSubdomain_RejectsTooLongNames()
    {
        SiteEnvFile.IsValidSubdomain(new string('a', 63)).Should().BeTrue();
        SiteEnvFile.IsValidSubdomain(new string('a', 64)).Should().BeFalse();
    }
}

public class SiteSecretSanitizerTests
{
    [Theory]
    [InlineData("POSTGRES_PASSWORD=pgsecret123", "POSTGRES_PASSWORD=***")]
    [InlineData("JWT_KEY=abcdef123456", "JWT_KEY=***")]
    [InlineData("GEMINI_API_KEY=AIzaSyAbcdefghijklmnopqrstuvwxyz0123456", "GEMINI_API_KEY=***")]
    [InlineData("TUNA_TOKEN: tuna_abc123", "TUNA_TOKEN: ***")]
    [InlineData("Jwt__Key: supersecretkeyvalue", "Jwt__Key: ***")]
    [InlineData("ADMIN_PASSWORD=adminsecret1", "ADMIN_PASSWORD=***")]
    [InlineData("{\"password\":\"hunter2\",\"user\":\"admin\"}", "{\"password\":\"***\",\"user\":\"admin\"}")]
    [InlineData("api_key=\"quoted-secret\"", "api_key=\"***\"")]
    public void Sanitize_MasksSecretLookingAssignments(string input, string expected)
    {
        SiteSecretSanitizer.Sanitize(input).Should().Be(expected);
    }

    [Fact]
    public void Sanitize_MasksPasswordInConnectionString()
    {
        var result = SiteSecretSanitizer.Sanitize("Host=db;Database=hqstudio;Username=hqstudio;Password=pgsecret123;Timeout=5");

        result.Should().Be("Host=db;Database=hqstudio;Username=hqstudio;Password=***;Timeout=5");
    }

    [Fact]
    public void Sanitize_MasksBearerAndBasicHeaders()
    {
        SiteSecretSanitizer.Sanitize("Authorization: Bearer abc.def-123_xyz").Should().Contain("Bearer ***").And.NotContain("abc.def");
        SiteSecretSanitizer.Sanitize("Authorization: Basic dXNlcjpwYXNzd29yZA==").Should().Contain("Basic ***").And.NotContain("dXNlcjpw");
    }

    [Fact]
    public void Sanitize_MasksJwtTokens()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dBjftJeZ4CVPmB92K27uhbUJU1p1r";

        var result = SiteSecretSanitizer.Sanitize($"issued token for user: {jwt} ok");

        result.Should().NotContain("eyJhbGci").And.Contain("***");
    }

    [Fact]
    public void Sanitize_MasksGoogleApiKeyInsideUrl()
    {
        var result = SiteSecretSanitizer.Sanitize("GET https://generativelanguage.googleapis.com/v1/models?key=AIzaSyAbcdefghijklmnopqrstuvwxyz0123456 200");

        result.Should().NotContain("AIzaSy").And.Contain("200");
    }

    [Fact]
    public void Sanitize_MasksPasswordInUrlUserInfo()
    {
        SiteSecretSanitizer.Sanitize("postgres://hqstudio:pgsecret123@db:5432/hqstudio")
            .Should().Be("postgres://hqstudio:***@db:5432/hqstudio");
    }

    [Fact]
    public void Sanitize_ReplacesKnownSecretsLiterally_EvenWithoutKeyName()
    {
        var result = SiteSecretSanitizer.Sanitize("connecting with pgsecret123 to db, again pgsecret123", new[] { "pgsecret123" });

        result.Should().Be("connecting with *** to db, again ***");
    }

    [Fact]
    public void Sanitize_IgnoresTooShortKnownSecrets()
    {
        SiteSecretSanitizer.Sanitize("port 80 is open", new[] { "80", "" }).Should().Be("port 80 is open");
    }

    [Fact]
    public void Sanitize_LongerSecretIsReplacedBeforeItsSubstring()
    {
        var result = SiteSecretSanitizer.Sanitize("value abcd1234 end", new[] { "abcd", "abcd1234" });

        result.Should().Be("value *** end");
    }

    [Theory]
    [InlineData("Forwarding https://hq-studio.ru.tuna.am -> proxy:80")]
    [InlineData("info: Request finished HTTP/1.1 GET http://localhost:5000/api/health - 200 - 1.4ms")]
    [InlineData("api-1  | Now listening on: http://[::]:5000")]
    public void Sanitize_LeavesOrdinaryLogLinesAlone(string line)
    {
        SiteSecretSanitizer.Sanitize(line).Should().Be(line);
    }

    [Fact]
    public void Sanitize_IsIdempotent()
    {
        var once = SiteSecretSanitizer.Sanitize("POSTGRES_PASSWORD=pgsecret123 Bearer abcdef123456", new[] { "pgsecret123" });

        SiteSecretSanitizer.Sanitize(once).Should().Be(once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sanitize_EmptyInput_ReturnsEmpty(string? input)
    {
        SiteSecretSanitizer.Sanitize(input).Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_HandlesMultiLineText()
    {
        var result = SiteSecretSanitizer.Sanitize("line one\nJWT_KEY=abc123456\nline three");

        result.Should().Be("line one\nJWT_KEY=***\nline three");
    }
}

public class SiteErrorMapperTests
{
    [Theory]
    [InlineData("error during connect: open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.")]
    [InlineData("Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?")]
    [InlineData("failed to connect to the docker API at npipe:////./pipe/docker_engine")]
    public void Classify_DockerNotRunning(string output)
    {
        SiteErrorMapper.Classify(output).Should().Be(SiteFailureKind.DockerNotRunning);
    }

    [Theory]
    [InlineData("Error response from daemon: Bind for 127.0.0.1:8080 failed: port is already allocated")]
    [InlineData("listen tcp 127.0.0.1:8080: bind: address already in use")]
    [InlineData("Ports are not available: exposing port TCP 127.0.0.1:8080")]
    [InlineData("Only one usage of each socket address (protocol/network address/port) is normally permitted")]
    public void Classify_PortBusy(string output)
    {
        SiteErrorMapper.Classify(output).Should().Be(SiteFailureKind.PortBusy);
    }

    [Theory]
    [InlineData("Get \"https://registry-1.docker.io/v2/\": dial tcp: lookup registry-1.docker.io: no such host")]
    [InlineData("net/http: TLS handshake timeout")]
    [InlineData("Client.Timeout exceeded while awaiting headers")]
    [InlineData("dial tcp 104.18.0.1:443: i/o timeout")]
    public void Classify_NoInternet(string output)
    {
        SiteErrorMapper.Classify(output).Should().Be(SiteFailureKind.NoInternet);
    }

    [Theory]
    [InlineData("service \"api\" depends on undefined service")]
    [InlineData("")]
    [InlineData(null)]
    public void Classify_OtherFailuresFallBackToCompose(string? output)
    {
        SiteErrorMapper.Classify(output).Should().Be(SiteFailureKind.ComposeFailed);
    }

    [Fact]
    public void FromProcess_PortBusy_NamesThePort()
    {
        var failure = SiteErrorMapper.FromProcess("Запуск сайта",
            new SiteProcessResult(1, "", "Bind for 127.0.0.1:8080 failed: port is already allocated"));

        failure.Kind.Should().Be(SiteFailureKind.PortBusy);
        failure.Title.Should().Be("Порт занят");
        failure.Message.Should().Contain("порт 8080");
    }

    [Fact]
    public void FromProcess_DockerNotRunning_PointsToStartDockerButton()
    {
        var failure = SiteErrorMapper.FromProcess("Запуск сайта", SiteTestEnv.DockerDown());

        failure.Kind.Should().Be(SiteFailureKind.DockerNotRunning);
        failure.Message.Should().Contain("Запустить Docker");
    }

    [Fact]
    public void FromProcess_NoInternet_AsksToCheckConnection()
    {
        var failure = SiteErrorMapper.FromProcess("Запуск сайта", new SiteProcessResult(1, "", "lookup registry-1.docker.io: no such host"));

        failure.Kind.Should().Be(SiteFailureKind.NoInternet);
        failure.Message.Should().Contain("интернет");
    }

    [Fact]
    public void FromProcess_UnknownFailure_KeepsOnlyLastLinesAndHidesSecrets()
    {
        var lines = Enumerable.Range(1, 30).Select(i => $"line {i}").Append("POSTGRES_PASSWORD=pgsecret123");
        var failure = SiteErrorMapper.FromProcess("Запуск сайта", new SiteProcessResult(1, "", string.Join("\n", lines)),
            new[] { "pgsecret123" });

        failure.Kind.Should().Be(SiteFailureKind.ComposeFailed);
        failure.Message.Should().StartWith("Запуск сайта не получилось");
        failure.Details.Should().Contain("line 30").And.NotContain("line 10 ").And.NotContain("pgsecret123");
        failure.Details.Split(Environment.NewLine).Should().HaveCount(12);
    }

    [Fact]
    public void FromException_ProcessThatCannotStart_MeansDockerIsMissing()
    {
        var failure = SiteErrorMapper.FromException("Запуск сайта", new System.ComponentModel.Win32Exception(2, "not found"));

        failure.Kind.Should().Be(SiteFailureKind.DockerMissing);
    }

    [Fact]
    public void FromException_Cancellation_IsNotAnError()
    {
        SiteErrorMapper.FromException("Запуск сайта", new OperationCanceledException()).Kind.Should().Be(SiteFailureKind.Cancelled);
    }

    [Fact]
    public void FromException_Unexpected_ShowsSanitizedMessage()
    {
        var failure = SiteErrorMapper.FromException("Запуск сайта", new InvalidOperationException("token=abc123456"));

        failure.Kind.Should().Be(SiteFailureKind.Other);
        failure.Details.Should().Be("token=***");
    }
}
