using FluentAssertions;
using HQStudio.Services;
using HQStudio.Services.BugReport;
using HQStudio.Services.Guide;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class AdminAccessTests
{
    [Theory]
    [InlineData("Admin", true)]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("  Admin ", true)]
    [InlineData("Manager", false)]
    [InlineData("Worker", false)]
    [InlineData("Editor", false)]
    [InlineData("Administrator", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAdmin_OnlyTheAdminRoleCounts(string? role, bool expected)
    {
        AdminAccess.IsAdmin(role).Should().Be(expected);
    }
}

public class GuideMarkupTests
{
    [Fact]
    public void Parse_SplitsBoldParts()
    {
        var runs = GuideMarkup.Parse("Нажмите **Ключи** и **Сохранить**.");

        runs.Should().Equal(
            new GuideRun("Нажмите ", false),
            new GuideRun("Ключи", true),
            new GuideRun(" и ", false),
            new GuideRun("Сохранить", true),
            new GuideRun(".", false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parse_Empty_GivesNothing(string? text)
    {
        GuideMarkup.Parse(text).Should().BeEmpty();
    }

    [Fact]
    public void Strip_RemovesTheMarkers()
    {
        GuideMarkup.Strip("Откройте **Сайт**, затем **Ключи**").Should().Be("Откройте Сайт, затем Ключи");
    }

    [Theory]
    [InlineData("без разметки", true)]
    [InlineData("**жирный**", true)]
    [InlineData("**а** и **б**", true)]
    [InlineData("**не закрыто", false)]
    [InlineData("", true)]
    public void IsBalanced_NeedsPairedMarkers(string text, bool expected)
    {
        GuideMarkup.IsBalanced(text).Should().Be(expected);
    }
}

public class SiteGuideContentTests
{
    private static string Everything => string.Join("\n", SiteGuideContent.AllText());

    [Fact]
    public void Chapters_AreTheSixTopicsInTheAgreedOrder()
    {
        SiteGuideContent.Sections.Select(s => s.Id).Should().Equal(
            "site", "first-start", "domain", "updates", "bug-report", "troubleshooting");
        SiteGuideContent.Sections.Should().OnlyContain(s => s.Title.Length > 0 && s.Summary.Length > 0);
    }

    [Fact]
    public void EveryChapter_HasNumberedShortSteps()
    {
        foreach (var section in SiteGuideContent.Sections)
        {
            section.Steps.Should().NotBeEmpty(section.Id);
            section.Steps.Select(s => s.Number).Should().Equal(Enumerable.Range(1, section.Steps.Count), section.Id);
            section.Steps.Should().OnlyContain(s => s.Title.Length > 0 && s.Title.Length <= 60, section.Id);
            // Шаг не должен превращаться в стену текста.
            section.Steps.Should().OnlyContain(s => s.Text.Length <= 520 && s.Note.Length <= 520, section.Id);
            section.Steps.Should().OnlyContain(s => s.BulletList.Count <= 6, section.Id);
        }
    }

    [Fact]
    public void Find_ReturnsTheChapterById()
    {
        SiteGuideContent.Find("domain")!.Title.Should().Be("Свой адрес сайта");
        SiteGuideContent.Find("DOMAIN").Should().BeNull();
        SiteGuideContent.Find(null).Should().BeNull();
        SiteGuideContent.Find("nothing").Should().BeNull();
        SiteGuideContent.DomainSectionId.Should().Be("domain");
    }

    [Fact]
    public void Text_HasNoRemovedFeatures()
    {
        foreach (var word in RemovedFeatureWords.All)
            Everything.Should().NotContainEquivalentOf(word);
    }

    [Fact]
    public void Text_HasNothingAboutTheFreeTariffTheTemporaryAddressOrTheRandomName()
    {
        foreach (var word in RemovedFeatureWords.FreeTariff)
            Everything.Should().NotContainEquivalentOf(word);
    }

    [Fact]
    public void Text_HasNoEmoji()
    {
        Everything.Any(c => char.IsSurrogate(c) || (c >= 0x2600 && c <= 0x27BF)).Should().BeFalse();
    }

    [Fact]
    public void BoldMarkers_AreAlwaysPaired()
    {
        SiteGuideContent.AllText().Should().OnlyContain(text => GuideMarkup.IsBalanced(text));
    }

    [Fact]
    public void Links_AreSecureWebAddressesNotLocalFiles()
    {
        var urls = SiteGuideContent.AllUrls().ToList();

        urls.Should().NotBeEmpty();
        urls.Should().OnlyContain(u => u.StartsWith("https://") && !u.EndsWith(".html"));
        urls.Should().OnlyContain(u => Uri.IsWellFormedUriString(u, UriKind.Absolute));
    }

    [Fact]
    public void DomainChapter_WalksThroughRegRuTunaDnsVerificationAndTheProgram()
    {
        var section = SiteGuideContent.Find(SiteGuideContent.DomainSectionId)!;
        var titles = section.Steps.Select(s => s.Title).ToList();
        int At(string part) => titles.IndexOf(titles.Single(t => t.Contains(part)));

        At("Купите домен").Should().BeLessThan(At("Добавьте домен в Tuna"));
        At("Добавьте домен в Tuna").Should().BeLessThan(At("запись на Рег.ру"));
        At("запись на Рег.ру").Should().BeLessThan(At("Дождитесь проверки"));
        At("Дождитесь проверки").Should().BeLessThan(At("Впишите токен и домен"));
        titles.Should().Contain(t => t.Contains("токен"));

        var text = string.Join("\n", section.AllText());
        text.Should().Contain("reg.ru").And.Contain("Проверить").And.Contain("В корзину")
            .And.Contain("my.tuna.am/domains").And.Contain("Добавить домен").And.Contain("Свой домен")
            .And.Contain("CNAME").And.Contain("Субдомен").And.Contain("AAAA")
            .And.Contain("Сохранить и применить").And.Contain("Ключи").And.Contain("Токен Tuna");
    }

    [Fact]
    public void DomainChapter_PromisesTheOwnAddress_NotAnAddressNameField()
    {
        var section = SiteGuideContent.Find(SiteGuideContent.DomainSectionId)!;

        section.Intro.Should().Contain("собственному адресу");
        string.Join("\n", section.AllText()).Should().NotContain("Имя адреса");
    }

    [Fact]
    public void UpdatesChapter_NamesTheThreeButtons()
    {
        var text = string.Join("\n", SiteGuideContent.Find(SiteGuideContent.UpdatesId)!.AllText());

        text.Should().Contain("Обновить приложение").And.Contain("Обновить сайт").And.Contain("Обновить всё");
    }

    [Fact]
    public void BugReportChapter_ExplainsTheGitHubFlow()
    {
        var section = SiteGuideContent.Find(SiteGuideContent.BugReportId)!;
        var text = string.Join("\n", section.AllText());

        text.Should().Contain("GitHub").And.Contain("Что случилось").And.Contain("Скопировать код")
            .And.Contain("Открыть страницу GitHub").And.Contain("Отправить через браузер").And.Contain("Submit new issue");
        section.Steps.SelectMany(s => s.LinkList).Select(l => l.Url).Should().Contain(BugReportConfig.NewIssueWebUrl);
    }

    [Fact]
    public void TroubleshootingChapter_CoversDockerRestartAndLogs()
    {
        var text = string.Join("\n", SiteGuideContent.Find(SiteGuideContent.TroubleshootingId)!.AllText());

        text.Should().Contain("Docker").And.Contain("Запустить Docker").And.Contain("Перезапустить")
            .And.Contain("Логи").And.Contain("Запустить");
    }

    [Fact]
    public void FirstStartChapter_NamesTheMainAdministratorAccount()
    {
        var text = string.Join("\n", SiteGuideContent.Find(SiteGuideContent.FirstStartId)!.AllText());

        text.Should().Contain("admin").And.Contain("администратор");
    }

    [Fact]
    public void Steps_ExposeTheirOptionalPartsOnlyWhenFilled()
    {
        var plain = new GuideStep("Заголовок", "Текст");
        var rich = new GuideStep("Заголовок", Bullets: new[] { "а" }, Rows: new[] { new GuideRow("б", "в") },
            Links: new[] { new GuideLink("г", "https://example.org") }, Note: "д");

        plain.HasText.Should().BeTrue();
        plain.HasBullets.Should().BeFalse();
        plain.HasRows.Should().BeFalse();
        plain.HasLinks.Should().BeFalse();
        plain.HasNote.Should().BeFalse();
        rich.HasText.Should().BeFalse();
        rich.HasBullets.Should().BeTrue();
        rich.HasRows.Should().BeTrue();
        rich.HasLinks.Should().BeTrue();
        rich.HasNote.Should().BeTrue();
    }
}

public class SiteGuideViewModelTests
{
    private static (SiteGuideViewModel Vm, SiteFakeShell Shell, SiteFakeNotifier Notifier) Make(string? start = null)
    {
        var shell = new SiteFakeShell();
        var notifier = new SiteFakeNotifier();
        return (new SiteGuideViewModel(SiteGuideContent.Sections, shell, notifier, start), shell, notifier);
    }

    [Fact]
    public void Opens_OnTheFirstChapter_ByDefault()
    {
        var (vm, _, _) = Make();

        vm.SelectedSection.Id.Should().Be("site");
        vm.Choices.Should().HaveCount(6);
        vm.Choices.Single(c => c.IsSelected).Id.Should().Be("site");
        vm.ProgressText.Should().Be("Глава 1 из 6");
        vm.CanGoPrevious.Should().BeFalse();
        vm.CanGoNext.Should().BeTrue();
    }

    [Fact]
    public void Opens_OnTheRequestedChapter()
    {
        var (vm, _, _) = Make("domain");

        vm.SelectedSection.Id.Should().Be("domain");
        vm.Title.Should().Be("Свой адрес сайта");
        vm.HasIntro.Should().BeTrue();
        vm.Steps.Should().HaveCountGreaterThan(5);
        vm.Choices.Single(c => c.IsSelected).Id.Should().Be("domain");
    }

    [Fact]
    public void UnknownStartChapter_FallsBackToTheFirst()
    {
        var (vm, _, _) = Make("no-such-chapter");

        vm.SelectedSection.Id.Should().Be("site");
    }

    [Fact]
    public void NextAndPrevious_WalkThroughTheChapters_AndStopAtTheEnds()
    {
        var (vm, _, _) = Make();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.NextCommand.Execute(null);
        vm.SelectedSection.Id.Should().Be("first-start");
        vm.PreviousCommand.CanExecute(null).Should().BeTrue();
        changed.Should().Contain(nameof(SiteGuideViewModel.SelectedSection)).And.Contain(nameof(SiteGuideViewModel.Steps));

        vm.PreviousCommand.Execute(null);
        vm.PreviousCommand.Execute(null);
        vm.SelectedSection.Id.Should().Be("site");

        for (var i = 0; i < 10; i++)
            vm.NextCommand.Execute(null);
        vm.SelectedSection.Id.Should().Be("troubleshooting");
        vm.CanGoNext.Should().BeFalse();
        vm.NextCommand.CanExecute(null).Should().BeFalse();
        vm.ProgressText.Should().Be("Глава 6 из 6");
    }

    [Fact]
    public void SelectCommand_JumpsToAChapter_AndIgnoresUnknownOnes()
    {
        var (vm, _, _) = Make();

        vm.SelectCommand.Execute("updates");
        vm.SelectedSection.Id.Should().Be("updates");
        vm.Choices.Single(c => c.IsSelected).Id.Should().Be("updates");

        vm.SelectCommand.Execute("unknown");
        vm.SelectCommand.Execute(42);
        vm.SelectedSection.Id.Should().Be("updates");
        vm.Select("unknown").Should().BeFalse();
        vm.Select("domain").Should().BeTrue();
    }

    [Fact]
    public void ReportBugButton_IsOnlyOnTheBugReportChapter_AndRaisesTheEvent()
    {
        var (vm, _, _) = Make();
        var raised = 0;
        vm.ReportBugRequested += () => raised++;

        vm.ShowReportBug.Should().BeFalse();
        vm.Select("bug-report");
        vm.ShowReportBug.Should().BeTrue();

        vm.ReportBugCommand.Execute(null);
        raised.Should().Be(1);
    }

    [Fact]
    public void Links_OpenThroughTheShell_AndFailureIsAWarning()
    {
        var (vm, shell, notifier) = Make();

        vm.OpenLinkCommand.Execute("https://www.reg.ru");
        shell.Opened.Should().Equal("https://www.reg.ru");
        notifier.Messages.Should().BeEmpty();

        shell.OpenResult = false;
        vm.OpenLinkCommand.Execute("https://my.tuna.am/domains");

        notifier.Messages.Should().ContainSingle().Which.Should().StartWith("warning:");
    }

    [Fact]
    public void NoChapters_IsAProgrammingError()
    {
        var act = () => new SiteGuideViewModel(Array.Empty<GuideSection>(), new SiteFakeShell(), new SiteFakeNotifier());

        act.Should().Throw<ArgumentException>();
    }
}
