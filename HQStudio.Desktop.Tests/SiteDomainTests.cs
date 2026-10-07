using FluentAssertions;
using HQStudio.Services.Site;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class SiteDomainValidatorTests
{
    [Theory]
    [InlineData("crm.example.ru")]
    [InlineData("a.b")]
    [InlineData("my-crm.example.ru")]
    [InlineData("xn--80aswg.xn--p1ai")]
    [InlineData("sub.domain.example.co.uk")]
    [InlineData("crm2.example.ru")]
    public void Valid_HostNames_AreAccepted(string value)
    {
        SiteEnvFile.CheckDomain(value).Should().Be(DomainCheck.Ok);
        SiteEnvFile.IsValidDomain(value).Should().BeTrue();
    }

    [Fact]
    public void Empty_IsAccepted_BecauseTheDomainIsOptional()
    {
        SiteEnvFile.CheckDomain("").Should().Be(DomainCheck.Ok);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("https://crm.example.ru")]
    [InlineData("http://crm.example.ru/")]
    [InlineData("crm.example.ru/path")]
    [InlineData("crm.example.ru:8080")]
    [InlineData("crm example.ru")]
    [InlineData("Crm.Example.ru")]
    [InlineData("-a.example.ru")]
    [InlineData("a-.example.ru")]
    [InlineData("a..example.ru")]
    [InlineData(".example.ru")]
    [InlineData("example.ru.")]
    [InlineData("crm_example.ru")]
    [InlineData("crm@example.ru")]
    [InlineData("192.168.0.1")]
    [InlineData("example.123")]
    public void Malformed_Values_AreRejected_AsInvalid(string value)
    {
        SiteEnvFile.CheckDomain(value).Should().Be(DomainCheck.Invalid);
        SiteEnvFile.IsValidDomain(value).Should().BeFalse();
    }

    [Theory]
    [InlineData("црм.пример.рф")]
    [InlineData("кафе.ru")]
    [InlineData("café.fr")]
    public void NonAscii_AsksForPunycode(string value)
    {
        SiteEnvFile.CheckDomain(value).Should().Be(DomainCheck.NonAscii);
    }

    [Fact]
    public void TooLongLabelOrName_IsRejected()
    {
        SiteEnvFile.CheckDomain(new string('a', 63) + ".ru").Should().Be(DomainCheck.Ok);
        SiteEnvFile.CheckDomain(new string('a', 64) + ".ru").Should().Be(DomainCheck.Invalid);
        SiteEnvFile.CheckDomain(string.Join('.', Enumerable.Repeat(new string('a', 60), 5)) + ".ru").Should().Be(DomainCheck.Invalid);
    }

    [Fact]
    public void DomainKey_IsAddedWithoutDisturbingOtherLines()
    {
        var text = SiteTestEnv.BaseEnv.Replace("TUNA_DOMAIN=\r\n", "");

        var result = SiteEnvFile.SetValue(text, "TUNA_DOMAIN", "crm.example.ru");

        result.Should().StartWith(text).And.EndWith("TUNA_DOMAIN=crm.example.ru\r\n");
    }
}

public class SiteDomainApplyTests
{
    private static string Dir => SiteTestEnv.ServerDir;
    private const string TunnelLog = "tuna-1  | Forwarding https://crm.example.ru -> proxy:80";

    [Fact]
    public async Task Domain_IsWritten_AndSubdomainBecomesEmpty_OtherLinesStayIntact()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "hq"));
        env.Runner.When("logs --no-color tuna", 0, TunnelLog);

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        SiteEnvFile.GetValue(env.Env, "TUNA_SUBDOMAIN").Should().Be("");
        env.Env.Should().Contain("TUNA_SUBDOMAIN=\r\n").And.NotContain("TUNA_SUBDOMAIN=hq");
        env.Env.Should().Contain("# HQ Studio settings\r\n")
            .And.Contain("POSTGRES_PASSWORD=pgsecret123\r\n")
            .And.Contain("JWT_KEY=jwtsecret456789\r\n")
            .And.Contain("TUNA_TOKEN=tunatoken1234\r\n")
            .And.Contain("HQSTUDIO_VERSION=1.19.6\r\n");
    }

    [Fact]
    public async Task Domain_SubdomainEmptyLineIsWritten_EvenWhenItWasAlreadyEmpty()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", ""));
        env.Runner.When("logs --no-color tuna", 0, TunnelLog);

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        env.Env.Should().Contain("TUNA_SUBDOMAIN=\r\n");
    }

    [Fact]
    public async Task Domain_AndSubdomainTogether_AreRejectedBeforeAnyWrite()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, "hq", "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        env.Files.Log.Should().BeEmpty();
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearingDomain_WritesExplicitEmptyValue_AndKeepsTheRest()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "", "crm.example.ru"));
        env.Runner.When("logs --no-color tuna", 0, "Forwarding https://random-name.tuna.am -> proxy:80");

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, ""), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Env.Should().Contain("TUNA_DOMAIN=\r\n").And.NotContain("crm.example.ru");
        env.Env.Should().Contain("TUNA_TOKEN=tunatoken1234\r\n").And.Contain("POSTGRES_PASSWORD=pgsecret123\r\n");
    }

    [Fact]
    public async Task ClearingDomain_AndTypingSubdomain_InOneSave_WritesBoth()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "", "crm.example.ru"));
        env.Runner.When("logs --no-color tuna", 0, "Forwarding https://brave-otter-4821.tuna.am -> proxy:80");

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, "brave-otter-4821", ""), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("");
        SiteEnvFile.GetValue(env.Env, "TUNA_SUBDOMAIN").Should().Be("brave-otter-4821");
    }

    [Fact]
    public async Task NewSubdomain_ReplacesAnExistingDomain()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "", "crm.example.ru"));
        env.Runner.When("logs --no-color tuna", 0, "Forwarding https://my-name.tuna.am -> proxy:80");

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, "my-name"), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "TUNA_SUBDOMAIN").Should().Be("my-name");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("");
    }

    [Fact]
    public async Task NewSubdomain_WithNoDomain_DoesNotTouchTheDomainKey()
    {
        var withoutKey = SiteTestEnv.EnvWithToken("tunatoken1234", "").Replace("TUNA_DOMAIN=\r\n", "");
        var env = new SiteTestEnv(withoutKey);
        env.Runner.When("logs --no-color tuna", 0, "Forwarding https://my-name.tuna.am -> proxy:80");

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, "my-name"), null, CancellationToken.None);

        env.Env.Should().NotContain("TUNA_DOMAIN");
    }

    [Theory]
    [InlineData("https://crm.example.ru")]
    [InlineData("crm.example.ru/path")]
    [InlineData("crm example.ru")]
    [InlineData("Crm.Example.ru")]
    [InlineData("localhost")]
    public async Task InvalidDomain_IsRejected_WithoutWritingAnything(string domain)
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, domain), null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        result.Message.Should().Contain("crm.example.ru");
        env.Files.Log.Should().BeEmpty();
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task NonAsciiDomain_AsksForPunycode()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "црм.пример.рф"), null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        result.Message.Should().Contain("punycode");
        env.Files.Log.Should().BeEmpty();
    }

    [Fact]
    public async Task Domain_IsTrimmedBeforeSaving()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());
        env.Runner.When("logs --no-color tuna", 0, TunnelLog);

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "  crm.example.ru "), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
    }

    [Fact]
    public async Task Apply_WithDomain_WritesEnvThenUpThenReadsTunnelLogThenWritesPublicUrl()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());
        env.Runner.When("logs --no-color tuna", 0, TunnelLog);
        var timeline = new List<string>();
        env.Files.Timeline = timeline;
        var prefix = $"compose --project-directory {Dir} -f {Dir}\\docker-compose.yml ";
        env.Runner.OnCall = call =>
        {
            var text = call.StartsWith(prefix, StringComparison.Ordinal) ? call[prefix.Length..] : call;
            text = text.StartsWith("--profile tunnel ") ? "[tunnel] " + text["--profile tunnel ".Length..] : text;
            timeline.Add("run:" + text);
        };

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("https://crm.example.ru");
        timeline.Where(e => e is "write:.env" or "write:public-url.txt" || e.StartsWith("run:version")
                            || e.StartsWith("run:[tunnel] up") || e.StartsWith("run:[tunnel] logs"))
            .Should().StartWith(new[]
            {
                "write:.env",
                "run:version --format {{.Server.Version}}",
                "run:[tunnel] up -d --remove-orphans",
                "run:[tunnel] logs --no-color tuna",
                "write:public-url.txt"
            });
        env.Files.Files[SiteTestEnv.PublicUrlPath].Should().Be("https://crm.example.ru");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://crm.example.ru");
    }

    [Fact]
    public async Task TunnelProfile_DependsOnTheTokenOnly_NotOnTheDomain()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        env.Runner.ComposeCommands(Dir).Should().Equal("up -d --remove-orphans");
    }

    [Fact]
    public async Task Domain_WithTokenAlreadySaved_UsesTunnelProfile()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());
        env.Runner.When("logs --no-color tuna", 0, TunnelLog);

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        env.Runner.ComposeCommands(Dir).Should().StartWith("[tunnel] up -d --remove-orphans", "[tunnel] logs --no-color tuna");
    }

    [Fact]
    public async Task Domain_AddressNotYetKnown_HintsAtTheTunaAccount()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null, null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("пока не получен").And.Contain("my.tuna.am/domains");
    }

    [Fact]
    public void ReadKeysState_ReportsTheSavedDomain()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "", "crm.example.ru"));

        env.Manager.ReadKeysState().Should().Be(new SiteKeysState(false, true, "", "crm.example.ru"));
    }

    [Fact]
    public async Task Refresh_ShowsTheDomainAddressFromThePublicUrlFile()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "", "crm.example.ru")).WithPs(SiteTestEnv.AllRunning(tunnel: true));
        env.Files.Add(SiteTestEnv.PublicUrlPath, "https://crm.example.ru");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.PublicUrl.Should().Be("https://crm.example.ru");
        snapshot.Overview.Pill.Should().Be(SitePill.Running);
    }

    [Fact]
    public void KeysUpdate_WithOnlyDomain_IsNotEmpty()
    {
        new SiteKeysUpdate(null, null, null, "crm.example.ru").IsEmpty.Should().BeFalse();
        new SiteKeysUpdate(null, null, null, "").IsEmpty.Should().BeFalse();
        new SiteKeysUpdate(null, null, null).IsEmpty.Should().BeTrue();
    }
}

public class SiteDomainKeysViewModelTests
{
    private static (SiteKeysViewModel Vm, SiteFakeService Service, SiteFakeShell Shell) Make(SiteKeysState? state = null)
    {
        var service = new SiteFakeService { Keys = state ?? new SiteKeysState(true, true, "") };
        var shell = new SiteFakeShell();
        return (new SiteKeysViewModel(service, shell), service, shell);
    }

    [Fact]
    public void SavedDomain_IsShown_AndDisablesTheSubdomainField()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "", "crm.example.ru"));

        vm.Domain.Should().Be("crm.example.ru");
        vm.SubdomainEnabled.Should().BeFalse();
        vm.BuildUpdate().IsEmpty.Should().BeTrue();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void TypingADomain_ProducesAnUpdate_AndIgnoresTheSubdomain()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "hq"));

        vm.Domain = "  crm.example.ru ";

        vm.SubdomainEnabled.Should().BeFalse();
        vm.SubdomainError.Should().BeEmpty();
        vm.BuildUpdate().Should().Be(new SiteKeysUpdate(null, null, null, "crm.example.ru"));
        vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void InvalidSubdomainText_DoesNotBlockSaving_WhileADomainIsUsed()
    {
        var (vm, _, _) = Make();
        vm.Subdomain = "Bad Name";
        vm.SubdomainError.Should().NotBeEmpty();

        vm.Domain = "crm.example.ru";

        vm.SubdomainError.Should().BeEmpty();
        vm.CanSave.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://crm.example.ru")]
    [InlineData("crm.example.ru/page")]
    [InlineData("localhost")]
    [InlineData("Crm.Example.ru")]
    public void InvalidDomain_ShowsMessage_BlocksSave_NoPunycodeLink(string value)
    {
        var (vm, _, _) = Make();

        vm.Domain = value;

        vm.DomainError.Should().Contain("crm.example.ru");
        vm.ShowPunycodeLink.Should().BeFalse();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void NonAsciiDomain_ShowsPunycodeMessageAndLink_AndBlocksSave()
    {
        var (vm, _, _) = Make();

        vm.Domain = "црм.пример.рф";

        vm.DomainError.Should().Contain("punycode");
        vm.ShowPunycodeLink.Should().BeTrue();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void ValidDomain_HasNoErrorAndNoPunycodeLink()
    {
        var (vm, _, _) = Make();

        vm.Domain = "crm.example.ru";

        vm.DomainError.Should().BeEmpty();
        vm.ShowPunycodeLink.Should().BeFalse();
    }

    [Fact]
    public void ClearingASavedDomain_SendsExplicitEmpty_AndReEnablesTheSubdomain()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "", "crm.example.ru"));

        vm.Domain = "";

        vm.SubdomainEnabled.Should().BeTrue();
        vm.BuildUpdate().Should().Be(new SiteKeysUpdate(null, null, null, ""));
        vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void ClearingDomain_AndTypingSubdomain_SendsBoth()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "", "crm.example.ru"));

        vm.Domain = "";
        vm.Subdomain = "brave-otter-4821";

        vm.BuildUpdate().Should().Be(new SiteKeysUpdate(null, null, "brave-otter-4821", ""));
    }

    [Fact]
    public void EmptyDomain_WhenNoneWasSaved_SendsNothing()
    {
        var (vm, _, _) = Make();

        vm.Domain = "  ";

        vm.BuildUpdate().TunaDomain.Should().BeNull();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public async Task Save_SendsTheDomain_AndReloadsTheSavedState()
    {
        var (vm, service, _) = Make();
        service.ApplyHandler = (update, _, _) =>
        {
            service.Keys = new SiteKeysState(true, true, "", update.TunaDomain ?? "");
            return Task.FromResult(SiteOperationResult.Ok("Настройки сохранены и применены."));
        };
        vm.Domain = "crm.example.ru";

        await vm.SaveAsync();

        service.Applied.Should().ContainSingle().Which.Should().Be(new SiteKeysUpdate(null, null, null, "crm.example.ru"));
        vm.Completed.Should().BeTrue();
        vm.Domain.Should().Be("crm.example.ru");
        vm.SubdomainEnabled.Should().BeFalse();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void HelpButtons_OpenTheRightPages()
    {
        var (vm, _, shell) = Make();

        vm.OpenDomainHelpCommand.Execute(null);
        vm.OpenPunycodeCommand.Execute(null);
        vm.OpenTunaDomainsCommand.Execute(null);

        shell.Opened.Should().Equal(
            "https://tuna.am/docs/tunnels/guides/connect-self-domain",
            "https://www.reg.ru/web-tools/punycode",
            "https://my.tuna.am/domains");
    }
}
