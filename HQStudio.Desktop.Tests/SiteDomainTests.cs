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
    public void Empty_IsAccepted_BecauseTheSiteWorksLocallyWithoutADomain()
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

    [Fact]
    public void PublicUrlOf_AddsHttps()
    {
        SiteEnvFile.PublicUrlOf("crm.example.ru").Should().Be("https://crm.example.ru");
    }
}

public class SiteDomainApplyTests
{
    private static string Dir => SiteTestEnv.ServerDir;

    [Fact]
    public async Task Domain_IsWritten_OtherLinesStayIntact()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", ""));

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        env.Env.Should().Contain("# HQ Studio settings\r\n")
            .And.Contain("POSTGRES_PASSWORD=pgsecret123\r\n")
            .And.Contain("JWT_KEY=jwtsecret456789\r\n")
            .And.Contain("TUNA_TOKEN=tunatoken1234\r\n")
            .And.Contain("HQSTUDIO_VERSION=1.19.6\r\n");
    }

    [Fact]
    public async Task ClearingDomain_WritesExplicitEmptyValue_AndKeepsTheRest()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "crm.example.ru"));

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, ""), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Env.Should().Contain("TUNA_DOMAIN=\r\n").And.NotContain("crm.example.ru");
        env.Env.Should().Contain("TUNA_TOKEN=tunatoken1234\r\n").And.Contain("POSTGRES_PASSWORD=pgsecret123\r\n");
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

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, domain), null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        result.Message.Should().Contain("crm.example.ru");
        env.Files.Log.Should().BeEmpty();
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task NonAsciiDomain_AsksForPunycode()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "црм.пример.рф"), null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        result.Message.Should().Contain("punycode");
        env.Files.Log.Should().BeEmpty();
    }

    [Fact]
    public async Task PunycodeDomain_IsAccepted()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "xn--80aswg.xn--p1ai"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("xn--80aswg.xn--p1ai");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://xn--80aswg.xn--p1ai");
    }

    [Fact]
    public async Task Domain_IsTrimmedBeforeSaving()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", ""));

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "  crm.example.ru "), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
    }

    [Theory]
    [InlineData("tunatoken1234", "crm.example.ru", true)]
    [InlineData("tunatoken1234", "", false)]
    [InlineData("", "crm.example.ru", false)]
    [InlineData("", "", false)]
    public async Task TunnelProfile_NeedsBothTokenAndDomain(string token, string domain, bool tunnel)
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken(token, domain));

        var result = await env.Manager.StopAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Runner.ComposeCommands(Dir).Should().Equal(tunnel ? "[tunnel] stop" : "stop");
    }

    [Fact]
    public async Task Domain_WithTokenAlreadySaved_UsesTunnelProfile()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", ""));

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "crm.example.ru"), null, CancellationToken.None);

        env.Runner.ComposeCommands(Dir).Should().StartWith("[tunnel] up -d --remove-orphans");
    }

    [Fact]
    public async Task Domain_WithoutAToken_NeverStartsTheTunnel()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        env.Runner.ComposeCommands(Dir).Should().Equal("up -d --remove-orphans");
    }

    [Fact]
    public void KeysUpdate_WithOnlyDomain_IsNotEmpty()
    {
        new SiteKeysUpdate(null, "crm.example.ru").IsEmpty.Should().BeFalse();
        new SiteKeysUpdate(null, "").IsEmpty.Should().BeFalse();
        new SiteKeysUpdate("", null).IsEmpty.Should().BeFalse();
        new SiteKeysUpdate(null, null).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_ShowsTheDomainAddress()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "crm.example.ru")).WithPs(SiteTestEnv.AllRunning(tunnel: true));

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.PublicUrl.Should().Be("https://crm.example.ru");
        snapshot.Overview.Pill.Should().Be(SitePill.Running);
    }
}

public class SiteDomainKeysViewModelTests
{
    private static (SiteKeysViewModel Vm, SiteFakeService Service, SiteFakeShell Shell) Make(SiteKeysState? state = null)
    {
        var service = new SiteFakeService { Keys = state ?? new SiteKeysState(true, "") };
        var shell = new SiteFakeShell();
        return (new SiteKeysViewModel(service, shell), service, shell);
    }

    [Fact]
    public void SavedDomain_IsShown_AndNothingToSave()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, "crm.example.ru"));

        vm.Domain.Should().Be("crm.example.ru");
        vm.BuildUpdate().IsEmpty.Should().BeTrue();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void TypingADomain_ProducesAnUpdate()
    {
        var (vm, _, _) = Make();

        vm.Domain = "  crm.example.ru ";

        vm.BuildUpdate().Should().Be(new SiteKeysUpdate(null, "crm.example.ru"));
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
    public void ClearingASavedDomain_SendsExplicitEmpty()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, "crm.example.ru"));

        vm.Domain = "";

        vm.BuildUpdate().Should().Be(new SiteKeysUpdate(null, ""));
        vm.CanSave.Should().BeTrue();
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
    public void TokenAndDomain_TogetherGoIntoOneUpdate()
    {
        var (vm, _, _) = Make(new SiteKeysState(false, ""));

        vm.TunaInput = "  tunatoken1234 ";
        vm.Domain = "crm.example.ru";

        vm.BuildUpdate().Should().Be(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"));
    }

    [Fact]
    public async Task Save_SendsTheDomain_AndReloadsTheSavedState()
    {
        var (vm, service, _) = Make();
        service.ApplyHandler = (update, _, _) =>
        {
            service.Keys = new SiteKeysState(true, update.TunaDomain ?? "");
            return Task.FromResult(SiteOperationResult.Ok("Настройки сохранены и применены."));
        };
        vm.Domain = "crm.example.ru";

        await vm.SaveAsync();

        service.Applied.Should().ContainSingle().Which.Should().Be(new SiteKeysUpdate(null, "crm.example.ru"));
        vm.Completed.Should().BeTrue();
        vm.Domain.Should().Be("crm.example.ru");
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void HelpButtons_OpenTheRightPages()
    {
        var (vm, _, shell) = Make();

        vm.OpenPunycodeCommand.Execute(null);
        vm.OpenTunaDomainsCommand.Execute(null);
        vm.OpenTunaHelpCommand.Execute(null);

        shell.Opened.Should().Equal(
            "https://www.reg.ru/web-tools/punycode",
            "https://my.tuna.am/domains",
            "https://my.tuna.am");
    }

    [Fact]
    public void GuideButton_IsOnlyThereWhenTheCallerAllowsTheGuide()
    {
        var service = new SiteFakeService();
        var opened = 0;

        var withGuide = new SiteKeysViewModel(service, new SiteFakeShell(), () => opened++);
        var withoutGuide = new SiteKeysViewModel(service, new SiteFakeShell());

        withGuide.CanOpenGuide.Should().BeTrue();
        withGuide.OpenGuideCommand.CanExecute(null).Should().BeTrue();
        withGuide.OpenGuideCommand.Execute(null);
        opened.Should().Be(1);

        withoutGuide.CanOpenGuide.Should().BeFalse();
        withoutGuide.OpenGuideCommand.CanExecute(null).Should().BeFalse();
        withoutGuide.OpenGuideCommand.Execute(null);
        opened.Should().Be(1);
    }
}
