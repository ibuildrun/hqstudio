using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Services.Sim;
using Xunit;

namespace HQStudio.Setup.Tests;

public class EnvFileTests
{
    [Fact]
    public void Set_ReplacesExistingKeyAndKeepsCommentsAndOtherLines()
    {
        const string text = "# header\nA=1\n\n# note about B\nB=2\nC=3\n";

        var result = EnvFile.Set(text, "B", "9");

        result.Should().Be("# header\nA=1\n\n# note about B\nB=9\nC=3\n");
    }

    [Fact]
    public void Set_AppendsMissingKey()
    {
        var result = EnvFile.Set("A=1\n", "B", "2");

        result.Should().Be("A=1\nB=2\n");
    }

    [Fact]
    public void Set_KeepsWindowsLineEndings()
    {
        var result = EnvFile.Set("A=1\r\nB=2\r\n", "A", "5");

        result.Should().Be("A=5\r\nB=2\r\n");
    }

    [Fact]
    public void Set_DoesNotTouchCommentedKey()
    {
        var result = EnvFile.Set("# A=old\nB=1\n", "A", "new");

        result.Should().Be("# A=old\nB=1\nA=new\n");
    }

    [Theory]
    [InlineData("plain-Secret_123")]
    [InlineData("pa$$word with spaces")]
    [InlineData("tricky # hash and $VAR")]
    [InlineData("it's a \"quoted\" pass$word")]
    [InlineData("Иван Петров")]
    public void Quote_RoundTripsThroughSetAndGet(string value)
    {
        var text = EnvFile.Set("X=1\n", "SECRET", value);

        EnvFile.Get(text, "SECRET").Should().Be(value);
    }

    [Fact]
    public void Quote_PutsDollarSignsInsideSingleQuotesSoComposeDoesNotInterpolate()
    {
        EnvFile.Quote("a$b").Should().Be("'a$b'");
    }

    [Fact]
    public void Get_IgnoresInlineCommentOfBareValue()
    {
        EnvFile.Get("A=value # comment\n", "A").Should().Be("value");
    }

    [Fact]
    public void WriteAtomic_WritesUtf8WithoutBomAndReplacesFile()
    {
        using var temp = new TempDir();
        var path = temp.Combine("sub", ".env");

        EnvFile.WriteAtomic(path, "A=1\n");
        EnvFile.WriteAtomic(path, "A=2\nB=Привет\n");

        var bytes = File.ReadAllBytes(path);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        EnvFile.ReadAllTextOrNull(path).Should().Be("A=2\nB=Привет\n");
        Directory.GetFiles(temp.Combine("sub")).Should().HaveCount(1, "no temp files are left behind");
    }
}

public class EnvPlannerTests
{
    private static readonly string Example = SimPayloadSource.DefaultEnvExample;

    private static InstallAnswers Answers(string password = "Sunny-Day-2026", string gemini = "", string token = "", string sub = "") => new()
    {
        FirstName = "Иван",
        LastName = "Петров",
        Password = password,
        GeminiKey = gemini,
        TunaToken = token,
        TunaSubdomain = sub
    };

    [Fact]
    public void NewInstall_GeneratesSecretsAndFillsAnswers()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(gemini: "AIza123", token: "tok-1", sub: "MyStudio"), 8081, "1.20.0");

        plan.KeptExistingSecrets.Should().BeFalse();
        plan.PostgresPassword.Should().HaveLength(EnvPlanner.PostgresPasswordLength);
        plan.JwtKey.Should().HaveLength(EnvPlanner.JwtKeyLength);

        EnvFile.Get(plan.Text, "HQ_PORT").Should().Be("8081");
        EnvFile.Get(plan.Text, "POSTGRES_PASSWORD").Should().Be(plan.PostgresPassword);
        EnvFile.Get(plan.Text, "JWT_KEY").Should().Be(plan.JwtKey);
        EnvFile.Get(plan.Text, "ADMIN_PASSWORD").Should().Be("Sunny-Day-2026");
        EnvFile.Get(plan.Text, "ADMIN_NAME").Should().Be("Иван Петров");
        EnvFile.Get(plan.Text, "GEMINI_API_KEY").Should().Be("AIza123");
        EnvFile.Get(plan.Text, "TUNA_TOKEN").Should().Be("tok-1");
        EnvFile.Get(plan.Text, "TUNA_SUBDOMAIN").Should().Be("mystudio", "the subdomain is lower-cased");
        EnvFile.Get(plan.Text, "HQSTUDIO_VERSION").Should().Be("1.20.0");
    }

    [Fact]
    public void NewInstall_KeepsCommentsFromTheExample()
    {
        var example = "# Created by the installer\nHQ_PORT=8080\n# Generated secrets\nPOSTGRES_PASSWORD=change-me\nJWT_KEY=change-me-to-a-random-string-of-at-least-32-characters\n";

        var plan = EnvPlanner.Build(null, example, Answers(), 8080, "latest");

        plan.Text.Should().Contain("# Created by the installer");
        plan.Text.Should().Contain("# Generated secrets");
    }

    [Fact]
    public void Reinstall_KeepsDatabaseSecretsEvenWhenOtherValuesChange()
    {
        var first = EnvPlanner.Build(null, Example, Answers(), 8080, "1.19.0");

        var second = EnvPlanner.Build(first.Text, Example, Answers(password: "Another-Pass-1"), 8082, "1.20.0");

        second.KeptExistingSecrets.Should().BeTrue();
        second.PostgresPassword.Should().Be(first.PostgresPassword);
        second.JwtKey.Should().Be(first.JwtKey);
        EnvFile.Get(second.Text, "POSTGRES_PASSWORD").Should().Be(first.PostgresPassword);
        EnvFile.Get(second.Text, "JWT_KEY").Should().Be(first.JwtKey);
        EnvFile.Get(second.Text, "HQ_PORT").Should().Be("8082");
        EnvFile.Get(second.Text, "HQSTUDIO_VERSION").Should().Be("1.20.0");
    }

    [Fact]
    public void Reinstall_ReplacesPlaceholderSecrets()
    {
        var plan = EnvPlanner.Build(Example, Example, Answers(), 8080, "1.0.0");

        plan.KeptExistingSecrets.Should().BeFalse();
        plan.PostgresPassword.Should().NotBe("change-me");
        plan.JwtKey.Should().NotStartWith("change-me");
    }

    [Fact]
    public void Reinstall_ShortJwtKeyIsRegenerated()
    {
        var existing = "POSTGRES_PASSWORD=realpassword123\nJWT_KEY=tooshort\n";

        var plan = EnvPlanner.Build(existing, Example, Answers(), 8080, "1.0.0");

        plan.PostgresPassword.Should().Be("realpassword123");
        plan.JwtKey.Should().HaveLength(EnvPlanner.JwtKeyLength);
    }

    [Fact]
    public void BlankAdminPassword_ClearsOnlyThatKey()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(), 8080, "1.0.0");

        var blanked = EnvPlanner.BlankAdminPassword(plan.Text);

        EnvFile.Get(blanked, "ADMIN_PASSWORD").Should().BeEmpty();
        EnvFile.Get(blanked, "POSTGRES_PASSWORD").Should().Be(plan.PostgresPassword);
        EnvFile.Get(blanked, "ADMIN_NAME").Should().Be("Иван Петров");
    }

    [Fact]
    public void BlankAnswersKeepExistingKeys()
    {
        var existing = "POSTGRES_PASSWORD=realpassword123\nJWT_KEY=" + new string('k', 40) + "\nGEMINI_API_KEY=old-gemini\nTUNA_TOKEN=old-token\nTUNA_SUBDOMAIN=oldsub\n";

        var plan = EnvPlanner.Build(existing, Example, Answers(), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "GEMINI_API_KEY").Should().Be("old-gemini");
        EnvFile.Get(plan.Text, "TUNA_TOKEN").Should().Be("old-token");
        EnvFile.Get(plan.Text, "TUNA_SUBDOMAIN").Should().Be("oldsub");
    }

    [Fact]
    public void PasswordWithSpecialCharactersSurvivesTheFile()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(password: "pa$$ #w'rd \"x\""), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "ADMIN_PASSWORD").Should().Be("pa$$ #w'rd \"x\"");
    }

    [Fact]
    public void GetPort_ReadsValueOrFallsBack()
    {
        EnvPlanner.GetPort("HQ_PORT=8085\n").Should().Be(8085);
        EnvPlanner.GetPort("HQ_PORT=abc\n").Should().Be(8080);
        EnvPlanner.GetPort("").Should().Be(8080);
    }
}

public class SecretGeneratorTests
{
    [Theory]
    [InlineData(24)]
    [InlineData(48)]
    public void Create_HasRequestedLength(int length) => SecretGenerator.Create(length).Should().HaveLength(length);

    [Fact]
    public void Create_UsesOnlyTheSafeAlphabet()
    {
        var secret = string.Concat(Enumerable.Range(0, 50).Select(_ => SecretGenerator.Create(64)));

        secret.ToCharArray().Should().OnlyContain(c => SecretGenerator.Alphabet.Contains(c));
        SecretGenerator.Alphabet.Should().NotContainAny("0", "O", "1", "l", "I", "$", "#", "'", "\"", " ");
    }

    [Fact]
    public void Create_ProducesDifferentValues()
    {
        var values = Enumerable.Range(0, 200).Select(_ => SecretGenerator.Create(32)).ToList();

        values.Distinct().Should().HaveCount(200);
    }

    [Fact]
    public void Create_RejectsNonPositiveLength()
    {
        var act = () => SecretGenerator.Create(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
