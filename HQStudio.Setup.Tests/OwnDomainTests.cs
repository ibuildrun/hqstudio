using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Sim;
using HQStudio.Setup.UI;
using HQStudio.Setup.UI.Pages;
using Xunit;

namespace HQStudio.Setup.Tests;

public class DomainValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("crm.example.ru")]
    [InlineData("a.b")]
    [InlineData("sub.crm.example.ru")]
    [InlineData("my-site.example.com")]
    [InlineData("xn--80aswg.xn--p1ai")]
    [InlineData("  crm.example.ru  ")]
    [InlineData("Crm.Example.RU")]
    public void Domain_AcceptsHostNames(string value)
    {
        Validators.Domain(value).Should().BeNull();
    }

    [Theory]
    [InlineData("example")]
    [InlineData("http://crm.example.ru")]
    [InlineData("https://crm.example.ru")]
    [InlineData("crm.example.ru/login")]
    [InlineData("crm.example.ru?x=1")]
    [InlineData("crm.example.ru:8080")]
    [InlineData("user@crm.example.ru")]
    [InlineData("crm example.ru")]
    [InlineData("crm.example.ru.")]
    [InlineData(".example.ru")]
    [InlineData("crm..example.ru")]
    [InlineData("-crm.example.ru")]
    [InlineData("crm-.example.ru")]
    [InlineData("crm_x.example.ru")]
    [InlineData("192.168.0.1")]
    public void Domain_RejectsEverythingThatIsNotAPlainHostName(string value)
    {
        Validators.Domain(value).Should().NotBeNullOrEmpty();
        Validators.IsNonAsciiDomain(value).Should().BeFalse();
    }

    [Theory]
    [InlineData("кафе.рф")]
    [InlineData("crm.пример.ru")]
    [InlineData("Мой-сайт.ru")]
    public void Domain_NonAsciiAsksForThePunycodeForm(string value)
    {
        Validators.Domain(value).Should().Be(Validators.PunycodeMessage);
        Validators.PunycodeMessage.Should().Contain("xn--").And.Contain("reg.ru");
        Validators.IsNonAsciiDomain(value).Should().BeTrue();
    }

    [Fact]
    public void Domain_SchemeWithNonAsciiNameIsReportedAsSchemeFirst()
    {
        Validators.Domain("https://кафе.рф").Should().NotBe(Validators.PunycodeMessage);
    }

    [Fact]
    public void Domain_LengthLimits()
    {
        Validators.Domain(new string('a', 63) + ".ru").Should().BeNull();
        Validators.Domain(new string('a', 64) + ".ru").Should().NotBeNull();
        Validators.Domain(string.Join('.', Enumerable.Repeat(new string('a', 50), 6)) + ".ru").Should().NotBeNull();
    }

    [Fact]
    public void NormalizeDomain_TrimsAndLowersTheCase()
    {
        Validators.NormalizeDomain("  Crm.Example.RU ").Should().Be("crm.example.ru");
        Validators.NormalizeDomain(null).Should().BeEmpty();
    }
}

public class DomainPlannerTests
{
    private static readonly string Example = SimPayloadSource.DefaultEnvExample;

    private static InstallAnswers Answers(string token = "tok-1", string domain = "") => new()
    {
        FirstName = "Иван", LastName = "Петров", Password = "Sunny-Day-2026",
        TunaToken = token, TunaDomain = domain
    };

    [Fact]
    public void DomainGiven_WritesTheDomainAndThePublicUrl()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(domain: "CRM.Example.ru"), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        EnvFile.Get(plan.Text, "PUBLIC_URL").Should().Be("https://crm.example.ru", "the API learns its public address at the first start");
    }

    [Fact]
    public void TokenWithoutADomain_KeepsTheDomainAndThePublicUrlEmpty()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "TUNA_TOKEN").Should().Be("tok-1");
        EnvFile.Get(plan.Text, "TUNA_DOMAIN").Should().BeEmpty();
        EnvFile.Get(plan.Text, "PUBLIC_URL").Should().BeEmpty();
    }

    [Fact]
    public void BothBlank_WritesBothEmpty()
    {
        var plan = EnvPlanner.Build(null, Example, Answers(token: ""), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "TUNA_TOKEN").Should().BeEmpty();
        EnvFile.Get(plan.Text, "TUNA_DOMAIN").Should().BeEmpty();
    }

    [Fact]
    public void BlankDomainKeepsTheExistingOne()
    {
        var existing = "POSTGRES_PASSWORD=realpassword123\nTUNA_TOKEN=old-token\nTUNA_DOMAIN=old.example.ru\n";

        var plan = EnvPlanner.Build(existing, Example, Answers(token: ""), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "TUNA_DOMAIN").Should().Be("old.example.ru");
        EnvFile.Get(plan.Text, "TUNA_TOKEN").Should().Be("old-token");
        EnvFile.Get(plan.Text, "PUBLIC_URL").Should().Be("https://old.example.ru");
    }

    [Fact]
    public void NewDomainReplacesTheExistingOne()
    {
        var existing = "TUNA_DOMAIN=old.example.ru\n";

        var plan = EnvPlanner.Build(existing, Example, Answers(domain: "new.example.ru"), 8080, "1.0.0");

        EnvFile.Get(plan.Text, "TUNA_DOMAIN").Should().Be("new.example.ru");
    }

    [Fact]
    public void ExpectedPublicUrl_IsHttpsOfTheDomain() => EnvPlanner.PublicUrlFor("crm.example.ru").Should().Be("https://crm.example.ru");
}

public class OwnDomainFlowTests : IDisposable
{
    private readonly Rig _rig = new();

    public void Dispose() => _rig.Dispose();

    private static InstallAnswers Answers(string token = "tuna-secret-token-777", string domain = "crm.example.ru") => new()
    {
        FirstName = "Иван", LastName = "Петров", Password = "Sunny-Day-2026",
        TunaToken = token, TunaDomain = domain
    };

    [Fact]
    public async Task LogAddressIsPreferredOverTheExpectedOne()
    {
        _rig.Docker.Handler = call => call.Verb == "logs"
            ? new CommandResult(0, "Forwarding https://real.example.ru -> proxy:80")
            : new CommandResult(0, "");
        var context = _rig.Context(Answers());
        var engine = new InstallEngine(context);

        await engine.RunAsync(CancellationToken.None);

        engine.LastFailure.Should().BeNull();
        context.PublicUrl.Should().Be("https://real.example.ru");
        engine.Stages.Single(s => s.Id == StageId.PublicUrl).State.Should().Be(StageState.Done);
        EnvFile.Get(File.ReadAllText(_rig.Paths.EnvFile), "PUBLIC_URL").Should().Be("https://real.example.ru");
    }

    [Fact]
    public async Task WithoutALogAddress_TheExpectedDomainUrlIsUsedWithAWarning()
    {
        _rig.Docker.Handler = call => call.Verb == "logs" ? new CommandResult(0, "tuna-1 | domain is not verified") : new CommandResult(0, "");
        var context = _rig.Context(Answers());
        var engine = new InstallEngine(context);

        var outcome = await engine.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        context.ExpectedPublicUrl.Should().Be("https://crm.example.ru");
        context.PublicUrl.Should().Be("https://crm.example.ru");
        engine.Stages.Single(s => s.Id == StageId.PublicUrl).State.Should().Be(StageState.Warning);
        context.PublicUrlNote.Should().Contain("my.tuna.am/domains");
        File.ReadAllText(_rig.Paths.PublicUrlFile).Should().Be("https://crm.example.ru");
        var env = File.ReadAllText(_rig.Paths.EnvFile);
        EnvFile.Get(env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        EnvFile.Get(env, "PUBLIC_URL").Should().Be("https://crm.example.ru");
    }

    [Fact]
    public async Task WithoutAToken_NoTunnelRunsAndNoDomainIsExpected()
    {
        var context = _rig.Context(Answers(token: ""));
        var engine = new InstallEngine(context);

        await engine.RunAsync(CancellationToken.None);

        context.TunnelEnabled.Should().BeFalse();
        context.ExpectedPublicUrl.Should().BeNull();
        engine.Stages.Select(s => s.Id).Should().NotContain(StageId.PublicUrl);
    }

    [Fact]
    public async Task WithoutADomain_NoTunnelRunsEvenWithAToken()
    {
        var context = _rig.Context(Answers(domain: ""));
        var engine = new InstallEngine(context);

        var outcome = await engine.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        context.TunnelEnabled.Should().BeFalse();
        context.ExpectedPublicUrl.Should().BeNull();
        context.PublicUrl.Should().BeNull();
        engine.Stages.Select(s => s.Id).Should().NotContain(StageId.PublicUrl);
        _rig.Docker.Calls.Should().OnlyContain(c => !c.Tunnel);
        _rig.Docker.CountOf("logs").Should().Be(0);
        EnvFile.Get(File.ReadAllText(_rig.Paths.EnvFile), "TUNA_TOKEN").Should().Be("tuna-secret-token-777", "the token is kept for a domain added later");
    }

    [Fact]
    public async Task DoneAddress_IsTheDomainInTheFullFlow()
    {
        InstallPageViewModel.SuccessPause = TimeSpan.Zero;
        _rig.Docker.Handler = call => call.Verb == "logs" ? new CommandResult(0, "Forwarding https://crm.example.ru -> proxy:80") : new CommandResult(0, "");
        var vm = new WizardViewModel(_rig.Services(), SetupOptions.Parse(Array.Empty<string>()), () => { });
        vm.Start();
        var a = vm.Answers;
        a.FirstName = "Иван"; a.LastName = "Петров"; a.Password = "Sunny-Day-2026";
        a.TunaToken = "tuna-secret-token-777"; a.TunaDomain = "crm.example.ru";
        vm.NavigateTo(vm.Summary);

        vm.Summary.Primary.Command.Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (vm.CurrentPage != vm.Done && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        vm.CurrentPage.Should().BeSameAs(vm.Done);
        vm.Done.PublicUrl.Should().Be("https://crm.example.ru");
        vm.Done.HasPublicUrl.Should().BeTrue();
    }

    [Fact]
    public async Task DoneAddress_IsEmptyWithoutADomain()
    {
        InstallPageViewModel.SuccessPause = TimeSpan.Zero;
        var vm = new WizardViewModel(_rig.Services(), SetupOptions.Parse(Array.Empty<string>()), () => { });
        vm.Start();
        var a = vm.Answers;
        a.FirstName = "Иван"; a.LastName = "Петров"; a.Password = "Sunny-Day-2026";
        vm.NavigateTo(vm.Summary);

        vm.Summary.Primary.Command.Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (vm.CurrentPage != vm.Done && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        vm.CurrentPage.Should().BeSameAs(vm.Done);
        vm.Done.HasPublicUrl.Should().BeFalse();
    }
}

public class OwnDomainKeysPageTests : IDisposable
{
    private readonly Rig _rig = new();

    public void Dispose() => _rig.Dispose();

    private WizardViewModel Wizard()
    {
        var vm = new WizardViewModel(_rig.Services(), SetupOptions.Parse(Array.Empty<string>()), () => { });
        vm.Start();
        vm.NavigateTo(vm.Keys);
        return vm;
    }

    [Fact]
    public void ValidDomain_IsSavedNormalized()
    {
        var vm = Wizard();
        vm.Keys.TunaToken = "tok";
        vm.Keys.TunaDomain = "  CRM.Example.ru ";

        vm.Keys.Primary.Command.Execute(null);

        vm.CurrentPage.Should().BeSameAs(vm.Summary);
        vm.Answers.TunaToken.Should().Be("tok");
        vm.Answers.TunaDomain.Should().Be("crm.example.ru");
    }

    [Fact]
    public void TokenWithoutADomain_MovesOnAndLeavesTheDomainEmpty()
    {
        var vm = Wizard();
        vm.Keys.TunaToken = "  tok  ";

        vm.Keys.Primary.Command.Execute(null);

        vm.CurrentPage.Should().BeSameAs(vm.Summary);
        vm.Answers.TunaToken.Should().Be("tok");
        vm.Answers.TunaDomain.Should().BeEmpty();
    }

    [Fact]
    public void NonAsciiDomain_BlocksAndOffersThePunycodeConverter()
    {
        var vm = Wizard();
        vm.Keys.TunaToken = "tok";
        vm.Keys.TunaDomain = "кафе.рф";

        vm.Keys.Primary.Command.Execute(null);

        vm.CurrentPage.Should().BeSameAs(vm.Keys);
        vm.Keys.HasTunaDomainError.Should().BeTrue();
        vm.Keys.TunaDomainError.Should().Contain("xn--");
        vm.Keys.DomainNeedsPunycode.Should().BeTrue();

        vm.Keys.OpenPunycodeCommand.Execute(null);
        _rig.Shell.Opened.Should().Equal("https://www.reg.ru/web-tools/punycode");
    }

    [Fact]
    public void AsciiError_DoesNotOfferThePunycodeConverter()
    {
        var vm = Wizard();
        vm.Keys.TunaToken = "tok";
        vm.Keys.TunaDomain = "https://crm.example.ru/login";

        vm.Keys.DomainNeedsPunycode.Should().BeFalse();
        vm.Keys.HasTunaDomainError.Should().BeTrue();
    }

    [Fact]
    public void Domain_WithoutAToken_IsRefusedWithAnExplanationOnTheTokenField()
    {
        var vm = Wizard();
        vm.Keys.TunaDomain = "crm.example.ru";

        vm.Keys.Primary.Command.Execute(null);

        vm.CurrentPage.Should().BeSameAs(vm.Keys);
        vm.Keys.HasTunaTokenError.Should().BeTrue();
        vm.Keys.TunaTokenError.Should().Contain("токен");
        vm.Keys.HasTunaDomainError.Should().BeFalse();
    }

    [Fact]
    public void PrimaryButton_SaysNextWhenOnlyTheDomainIsFilled()
    {
        var vm = Wizard();
        vm.Keys.Primary.Text.Should().Be("Пропустить");

        vm.Keys.TunaDomain = "crm.example.ru";

        vm.Keys.Primary.Text.Should().Be("Далее");
    }

    [Fact]
    public void Links_OpenTunaAndThePunycodeConverter()
    {
        var vm = Wizard();

        vm.Keys.OpenTunaCommand.Execute(null);
        vm.Keys.OpenPunycodeCommand.Execute(null);

        _rig.Shell.Opened.Should().Equal("https://tuna.am", "https://www.reg.ru/web-tools/punycode");
    }

    [Fact]
    public void DomainHint_SaysTheDomainCanBeAttachedLaterInTheProgram()
    {
        var vm = Wizard();

        vm.Keys.DomainHintText.Should().Be(
            "Домен можно не вводить сейчас: подключить его можно позже в самой программе, раздел «Сайт» - «Инструкция». " +
            "Без домена сайт работает только на этом компьютере.");
        vm.Keys.TokenHintText.Should().Contain("подписка");
    }

    [Fact]
    public void Summary_ShowsTheHttpsAddressOfTheOwnDomain()
    {
        var vm = Wizard();
        vm.Answers.FirstName = "Иван"; vm.Answers.LastName = "Петров";
        vm.Answers.TunaToken = "tok";
        vm.Answers.TunaDomain = "CRM.example.ru";

        vm.NavigateTo(vm.Summary);

        vm.Summary.Rows.Single(r => r.Label == "Адрес для всех").Value.Should().Be("https://crm.example.ru");
    }

    [Fact]
    public void Summary_WithoutAnAddress_SaysTheSiteIsOnlyOnThisComputer()
    {
        var vm = Wizard();
        vm.Answers.TunaToken = "tok";
        vm.NavigateTo(vm.Summary);
        vm.Summary.Rows.Single(r => r.Label == "Адрес для всех").Value.Should().Contain("только на этом компьютере");

        vm.Answers.TunaToken = "";
        vm.Answers.TunaDomain = "crm.example.ru";
        vm.NavigateTo(vm.Summary);
        vm.Summary.Rows.Single(r => r.Label == "Адрес для всех").Value.Should().Contain("только на этом компьютере");
    }
}
