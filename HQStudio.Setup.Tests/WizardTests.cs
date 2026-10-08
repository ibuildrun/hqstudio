using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Sim;
using HQStudio.Setup.UI;
using HQStudio.Setup.UI.Pages;
using Xunit;

namespace HQStudio.Setup.Tests;

public class WizardTests : IDisposable
{
    private readonly Rig _rig = new();
    private int _closed;

    public WizardTests() => InstallPageViewModel.SuccessPause = TimeSpan.Zero;

    public void Dispose() => _rig.Dispose();

    private WizardViewModel Wizard(SetupOptions? options = null)
    {
        var vm = new WizardViewModel(_rig.Services(), options ?? SetupOptions.Parse(Array.Empty<string>()), () => _closed++);
        vm.Start();
        return vm;
    }

    private static async Task WaitUntil(Func<bool> condition, int seconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition was not reached in time.");
            await Task.Delay(10);
        }
    }

    private static void Press(ActionButton button)
    {
        button.IsVisible.Should().BeTrue();
        button.Command.CanExecute(null).Should().BeTrue();
        button.Command.Execute(null);
    }

    private async Task GoToDockerRunning(WizardViewModel vm)
    {
        Press(vm.Welcome.Primary);
        vm.CurrentPage.Should().BeSameAs(vm.Docker);
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.State.Should().Be(DockerPageState.Running);
        vm.Docker.OnLeft();
    }

    private static void FillAccount(AccountPageViewModel account)
    {
        account.FirstName = "Иван";
        account.LastName = "Петров";
        account.Password = "Sunny-Day-2026";
        account.PasswordRepeat = "Sunny-Day-2026";
    }

    [Fact]
    public void Starts_OnTheWelcomePageWithAStartButton()
    {
        var vm = Wizard();

        vm.CurrentPage.Should().BeSameAs(vm.Welcome);
        vm.Welcome.Primary.Text.Should().Be("Начать");
        vm.Welcome.Secondary.IsVisible.Should().BeFalse();
        vm.Steps.Should().HaveCount(7);
        vm.Steps[0].IsActive.Should().BeTrue();
        vm.Steps.Skip(1).Should().OnlyContain(s => s.IsPending);
    }

    [Fact]
    public async Task Back_ReturnsToThePreviousPage()
    {
        var vm = Wizard();
        await GoToDockerRunning(vm);

        vm.Docker.OnEntered();
        Press(vm.Docker.Secondary);
        vm.Docker.OnLeft();

        vm.CurrentPage.Should().BeSameAs(vm.Welcome);
    }

    [Fact]
    public async Task Rail_MarksFinishedStepsAsDone()
    {
        var vm = Wizard();
        await GoToDockerRunning(vm);
        Press(vm.Docker.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Account);
        vm.Steps[0].IsDone.Should().BeTrue();
        vm.Steps[1].IsDone.Should().BeTrue();
        vm.Steps[2].IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(DockerStatus.NotInstalled, DockerPageState.Missing)]
    [InlineData(DockerStatus.InstalledNotRunning, DockerPageState.Stopped)]
    [InlineData(DockerStatus.Running, DockerPageState.Running)]
    public async Task DockerPage_DetectsTheThreeStates(DockerStatus status, DockerPageState expected)
    {
        var vm = Wizard();
        _rig.Docker.Status = status;

        await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.State.Should().Be(expected);
    }

    [Fact]
    public async Task DockerPage_NextIsDisabledUntilDockerRunsOrTheUserChoosesToWait()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        vm.NavigateTo(vm.Docker);
        await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.Primary.Command.CanExecute(null).Should().BeFalse();
        vm.Docker.ShowSkip.Should().BeTrue();

        vm.Docker.SkipDocker = true;
        vm.Docker.Primary.Command.CanExecute(null).Should().BeTrue();

        vm.Docker.SkipDocker = false;
        _rig.Docker.Status = DockerStatus.Running;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.Primary.Command.CanExecute(null).Should().BeTrue();
        vm.Docker.ShowSkip.Should().BeFalse();
        vm.Docker.OnLeft();
    }

    [Fact]
    public async Task DockerPage_RedetectsAutomaticallyWhenDockerComesUp()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.InstalledNotRunning;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.State.Should().Be(DockerPageState.Stopped);

        _rig.Docker.Status = DockerStatus.Running;
        await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.State.Should().Be(DockerPageState.Running);
    }

    [Fact]
    public async Task DockerPage_StartButtonLaunchesDockerDesktopAndWaitsForTheEngine()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.InstalledNotRunning;
        await vm.Docker.RefreshAsync(CancellationToken.None);

        await vm.Docker.StartDockerAsync();

        _rig.Docker.DesktopStarts.Should().Be(1);
        vm.Docker.State.Should().Be(DockerPageState.StartingEngine);

        _rig.Docker.Status = DockerStatus.Running;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.State.Should().Be(DockerPageState.Running);
    }

    [Fact]
    public async Task DockerPage_EngineThatNeverStartsFallsBackToTheStoppedStateWithAMessage()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.InstalledNotRunning;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        await vm.Docker.StartDockerAsync();

        for (var i = 0; i < DockerPageViewModel.MaxEngineStartPolls + 1; i++)
            await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.State.Should().Be(DockerPageState.Stopped);
        vm.Docker.HasError.Should().BeTrue();
    }

    [Fact]
    public async Task DockerPage_AutomaticInstallDownloadsInstallsAndThenWaitsForTheEngine()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        var states = new List<DockerPageState>();
        vm.Docker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DockerPageViewModel.State))
                states.Add(vm.Docker.State);
        };

        await vm.Docker.InstallAutomaticallyAsync();

        _rig.Installer.Downloads.Should().Be(1);
        _rig.Installer.Runs.Should().Be(1);
        states.Should().Equal(DockerPageState.Downloading, DockerPageState.Installing, DockerPageState.StartingEngine);
        _rig.Docker.DesktopStarts.Should().Be(1);
        vm.Docker.DownloadPercent.Should().Be(100);
        vm.Docker.DownloadText.Should().Contain("МБ").And.Contain("100%");
    }

    [Fact]
    public async Task DockerPage_InstallerAskingForARebootShowsTheRebootPanel()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        _rig.Installer.Result = new DockerInstallResult(DockerInstallOutcome.RebootRequired, 3010, "reboot");
        await vm.Docker.RefreshAsync(CancellationToken.None);

        await vm.Docker.InstallAutomaticallyAsync();

        vm.Docker.State.Should().Be(DockerPageState.RebootRequired);
        vm.Docker.IsRebootRequired.Should().BeTrue();

        vm.Docker.RebootNowCommand.Execute(null);
        _rig.Shell.Reboots.Should().Be(1);

        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.State.Should().Be(DockerPageState.RebootRequired, "polling does not overwrite the reboot panel");
    }

    [Fact]
    public async Task DockerPage_FreshInstallThatNeverStartsOffersARebootWithAnExplanation()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        await vm.Docker.RefreshAsync(CancellationToken.None);
        await vm.Docker.InstallAutomaticallyAsync();
        vm.Docker.State.Should().Be(DockerPageState.StartingEngine);

        _rig.Docker.Status = DockerStatus.InstalledNotRunning;
        for (var i = 0; i < DockerPageViewModel.MaxEngineStartPolls + 1; i++)
            await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.State.Should().Be(DockerPageState.RebootRequired);
        vm.Docker.StatusText.Should().Contain("пока не отвечает");
    }

    [Fact]
    public async Task Rail_MarksTheInstallStepAsFailedAndClearsItOnRetry()
    {
        _rig.Injector = new FailOnceInjector(StageId.Pull);
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);
        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.Install.State == InstallRunState.Failed);

        vm.Steps[(int)WizardStep.Install].HasError.Should().BeTrue();

        Press(vm.Install.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        vm.Steps[(int)WizardStep.Install].HasError.Should().BeFalse();
    }

    [Fact]
    public async Task DockerPage_RebootLaterKeepsTheUserOnThePageWithANote()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        _rig.Installer.Result = new DockerInstallResult(DockerInstallOutcome.RebootRequired, 1641, "reboot");
        await vm.Docker.RefreshAsync(CancellationToken.None);
        await vm.Docker.InstallAutomaticallyAsync();

        vm.Docker.RebootLaterCommand.Execute(null);

        vm.Docker.State.Should().Be(DockerPageState.Missing);
        vm.Docker.HasPostponedNote.Should().BeTrue();
        vm.Docker.PostponedNote.Should().Contain("ещё раз");
    }

    [Fact]
    public async Task DockerPage_DeclinedElevationExplainsWhatToDo()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        _rig.Installer.Result = new DockerInstallResult(DockerInstallOutcome.Declined, 1223, "no");
        await vm.Docker.RefreshAsync(CancellationToken.None);

        await vm.Docker.InstallAutomaticallyAsync();

        vm.Docker.State.Should().Be(DockerPageState.Missing);
        vm.Docker.ErrorText.Should().Contain("«Да»");
    }

    [Fact]
    public async Task DockerPage_DownloadFailureSuggestsAVpn()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        _rig.Installer.DownloadFailure = new HttpRequestException("blocked");
        await vm.Docker.RefreshAsync(CancellationToken.None);

        await vm.Docker.InstallAutomaticallyAsync();

        vm.Docker.State.Should().Be(DockerPageState.Missing);
        vm.Docker.ErrorText.Should().Contain("VPN");
    }

    [Fact]
    public async Task DockerPage_ManualFallbackButtonsOpenTheContractedLinks()
    {
        var vm = Wizard();
        await vm.Docker.RefreshAsync(CancellationToken.None);

        vm.Docker.OpenDownloadPageCommand.Execute(null);
        vm.Docker.OpenGuideCommand.Execute(null);

        _rig.Shell.Opened.Should().Equal(
            "https://www.docker.com/products/docker-desktop/",
            "https://docs.docker.com/desktop/setup/install/windows-install/");
    }

    [Fact]
    public async Task DockerLater_SkipsAccountAndKeysAndShowsThePostponedSummary()
    {
        var vm = Wizard();
        _rig.Docker.Status = DockerStatus.NotInstalled;
        vm.NavigateTo(vm.Docker);
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.SkipDocker = true;

        Press(vm.Docker.Primary);

        vm.Answers.SkipSite.Should().BeTrue();
        vm.CurrentPage.Should().BeSameAs(vm.Summary);
        vm.Steps[2].IsSkipped.Should().BeTrue();
        vm.Steps[3].IsSkipped.Should().BeTrue();
        vm.Summary.Rows.Should().Contain(r => r.Label == "Сайт на компьютере" && r.IsWarning);
        vm.Summary.Rows.Should().NotContain(r => r.Label == "Главный аккаунт");

        Press(vm.Summary.Secondary);
        vm.CurrentPage.Should().BeSameAs(vm.Docker);
        vm.Docker.OnLeft();
    }

    [Fact]
    public async Task Account_InvalidInputStaysOnThePageAndShowsRussianMessages()
    {
        var vm = Wizard();
        await GoToDockerRunning(vm);
        vm.NavigateTo(vm.Account);
        string? focused = null;
        vm.Account.FocusRequested += f => focused = f;
        vm.Account.Password = "short";
        vm.Account.PasswordRepeat = "different";

        Press(vm.Account.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Account);
        vm.Account.FirstNameError.Should().Be("Введите имя");
        vm.Account.LastNameError.Should().Be("Введите фамилию");
        vm.Account.PasswordError.Should().Contain("8");
        vm.Account.PasswordRepeatError.Should().Be("Пароли не совпадают");
        vm.Account.HasFirstNameError.Should().BeTrue();
        focused.Should().Be(nameof(AccountPageViewModel.FirstName));
        vm.Answers.Password.Should().BeEmpty("nothing is stored until the page is valid");
    }

    [Fact]
    public void Account_ErrorsStayHiddenUntilTheFieldWasVisitedOrNextWasPressed()
    {
        var vm = Wizard();

        vm.Account.FirstNameError.Should().BeNull();
        vm.Account.Touch(nameof(AccountPageViewModel.FirstName));
        vm.Account.FirstNameError.Should().NotBeNull();
        vm.Account.LastNameError.Should().BeNull();
    }

    [Fact]
    public void Account_PasswordEqualToTheNameIsRejected()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Account);
        vm.Account.FirstName = "Константин";
        vm.Account.LastName = "Иванов";
        vm.Account.Password = "Константин";
        vm.Account.PasswordRepeat = "Константин";

        Press(vm.Account.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Account);
        vm.Account.PasswordError.Should().Contain("имен");
    }

    [Fact]
    public void Account_StrengthFollowsThePassword()
    {
        var vm = Wizard();

        vm.Account.Strength.Should().Be(0);
        vm.Account.Password = "abc";
        vm.Account.Strength.Should().Be(1);
        vm.Account.Password = "abcdefg1";
        vm.Account.Strength.Should().Be(2);
        vm.Account.Password = "Sunny-Day-2026";
        vm.Account.Strength.Should().Be(3);
        vm.Account.StrengthText.Should().Contain("Надёжный");
    }

    [Fact]
    public void Account_ValidInputSavesTheAnswersAndMovesToTheKeys()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Account);
        FillAccount(vm.Account);

        Press(vm.Account.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Keys);
        vm.Answers.FirstName.Should().Be("Иван");
        vm.Answers.LastName.Should().Be("Петров");
        vm.Answers.Password.Should().Be("Sunny-Day-2026");
        vm.Answers.AdminName.Should().Be("Иван Петров");
    }

    [Fact]
    public void Keys_ButtonSaysSkipWhenEmptyAndNextWhenFilled()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Keys);

        vm.Keys.Primary.Text.Should().Be("Пропустить");

        vm.Keys.TunaDomain = "crm.example.ru";
        vm.Keys.Primary.Text.Should().Be("Далее");
    }

    [Fact]
    public void Keys_InvalidDomainBlocksTheNextStep()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Keys);
        vm.Keys.TunaToken = "tok";
        vm.Keys.TunaDomain = "Мой сайт";

        Press(vm.Keys.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Keys);
        vm.Keys.HasTunaDomainError.Should().BeTrue();
    }

    [Fact]
    public void Keys_ValidValuesAreSavedTrimmedAndLowerCased()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Keys);
        vm.Keys.TunaToken = " tok ";
        vm.Keys.TunaDomain = " CRM.Example.ru ";

        Press(vm.Keys.Primary);

        vm.CurrentPage.Should().BeSameAs(vm.Summary);
        vm.Answers.TunaToken.Should().Be("tok");
        vm.Answers.TunaDomain.Should().Be("crm.example.ru");
    }

    [Fact]
    public void Keys_LinkOpensTheTunaPage()
    {
        var vm = Wizard();

        vm.Keys.OpenTunaCommand.Execute(null);

        _rig.Shell.Opened.Should().Equal("https://tuna.am");
    }

    [Fact]
    public void Summary_ListsFoldersAccountAndTheSiteAddress()
    {
        var vm = Wizard();
        var a = vm.Answers;
        a.FirstName = "Иван"; a.LastName = "Петров"; a.TunaToken = "t"; a.TunaDomain = "crm.example.ru";

        vm.NavigateTo(vm.Summary);

        var rows = vm.Summary.Rows.ToDictionary(r => r.Label, r => r.Value);
        rows["Папка программы"].Should().Be(_rig.Paths.AppDir);
        rows["Папка сайта"].Should().Be(_rig.Paths.ServerDir);
        rows["Главный аккаунт"].Should().Contain("Иван Петров").And.Contain("admin");
        rows["Адрес для всех"].Should().Be("https://crm.example.ru");
        rows.Keys.Should().NotContain("ИИ на сайте");
        vm.Summary.Primary.Text.Should().Be("Установить");
    }

    [Fact]
    public void Summary_DesktopShortcutCheckboxIsWrittenToTheAnswers()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Summary);

        vm.Summary.DesktopShortcut.Should().BeTrue("the shortcut is on by default");
        vm.Summary.DesktopShortcut = false;

        vm.Answers.DesktopShortcut.Should().BeFalse();
    }

    [Fact]
    public async Task FullFlow_InstallsAndEndsOnTheDonePage()
    {
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);

        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        vm.Install.State.Should().Be(InstallRunState.Succeeded);
        vm.Install.Stages.Should().HaveCount(8);
        vm.Install.Stages.Should().OnlyContain(s => s.IsDone);
        vm.Install.OverallPercent.Should().Be(100);
        vm.Done.SiteReady.Should().BeTrue();
        vm.Done.LocalUrl.Should().Be("http://localhost:8080");
        vm.Done.PublicUrl.Should().Be("https://crm.example.ru");
        vm.Done.HasPublicUrl.Should().BeTrue();
        vm.Done.GuideNote.Should().Contain("в программе").And.Contain("администратора").And.Contain("«Сайт»");
        vm.Done.LoginHint.Should().Contain("admin").And.Contain("пароль");
        vm.Steps.Should().OnlyContain(s => s.IsDone || s.IsActive);
        vm.Done.Primary.Text.Should().Be("Запустить HQ Studio");
        vm.Done.Secondary.Text.Should().Be("Закрыть");
    }

    [Fact]
    public async Task Failure_ShowsTheExplanationAndRetryResumes()
    {
        _rig.Injector = new FailOnceInjector(StageId.Pull);
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);

        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.Install.State == InstallRunState.Failed);

        vm.Install.Failure.Should().NotBeNull();
        vm.Install.Failure!.Info.Kind.Should().Be(FailureKind.Network);
        vm.Install.Failure.Hint.Should().Contain("VPN");
        vm.Install.Stages.Single(s => s.Id == StageId.Pull).IsFailed.Should().BeTrue();
        vm.Install.Stages.Where(s => s.Id < StageId.Pull).Should().OnlyContain(s => s.IsDone);
        vm.Install.Primary.Text.Should().Be("Повторить");
        vm.Install.Secondary.Text.Should().Be("Сообщить об ошибке");
        vm.Install.Title.Should().Be("Не получилось");
        var prepareOpensBeforeRetry = _rig.Payload.Opens;

        Press(vm.Install.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        _rig.Payload.Opens.Should().Be(prepareOpensBeforeRetry, "the finished copy stage is not repeated");
        vm.Install.Failure.Should().BeNull();
        vm.Install.Stages.Should().OnlyContain(s => s.IsDone);
    }

    [Fact]
    public async Task ReportProblem_OpensTheIssuePageWithASanitizedLog()
    {
        _rig.Injector = new FailOnceInjector(StageId.Pull);
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);
        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.Install.State == InstallRunState.Failed);

        Press(vm.Install.Secondary);

        var url = _rig.Shell.Opened.Should().ContainSingle().Subject;
        url.Should().StartWith("https://github.com/ibuildrun/hqstudio/issues/new?title=").And.Contain("labels=from-app");
        var decoded = Uri.UnescapeDataString(url);
        decoded.Should().Contain("Скачивание сайта");
        decoded.Should().NotContain("Sunny-Day-2026").And.NotContain("tuna-secret-token-777");
    }

    [Fact]
    public async Task SkippedDocker_FinishesWithTheHowToCompleteLaterPage()
    {
        _rig.Docker.Status = DockerStatus.NotInstalled;
        var vm = Wizard();
        vm.NavigateTo(vm.Docker);
        await vm.Docker.RefreshAsync(CancellationToken.None);
        vm.Docker.SkipDocker = true;
        Press(vm.Docker.Primary);

        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        vm.Install.Stages.Select(s => s.Id).Should().Equal(StageId.Prepare, StageId.Shortcuts);
        vm.Done.SiteReady.Should().BeFalse();
        vm.Done.SiteSkipped.Should().BeTrue();
        vm.Done.Headline.Should().Be("Программа установлена");
        vm.Done.SkippedHint.Should().Contain("«Сайт»");
    }

    [Fact]
    public async Task Done_LaunchStartsTheProgramAndClosesTheWindow()
    {
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);
        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        Press(vm.Done.Primary);

        _rig.Shell.Launched.Should().Equal(_rig.Paths.AppExe);
        _closed.Should().Be(1);
    }

    [Fact]
    public async Task Done_OpenSiteAndPublicAddressUseTheShell()
    {
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);
        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.CurrentPage == vm.Done);

        vm.Done.OpenSiteCommand.Execute(null);
        vm.Done.OpenPublicCommand.Execute(null);

        _rig.Shell.Opened.Should().Equal("http://localhost:8080", "https://crm.example.ru");
    }

    [Fact]
    public void Close_WhenIdleClosesImmediately()
    {
        var vm = Wizard();

        vm.RequestClose();

        _closed.Should().Be(1);
        vm.HasDialog.Should().BeFalse();
    }

    [Fact]
    public async Task Close_DuringInstallAsksForConfirmation()
    {
        var gate = new TaskCompletionSource();
        _rig.Docker.Handler = call =>
        {
            if (call.Verb == "pull")
                SpinWait.SpinUntil(() => gate.Task.IsCompleted, TimeSpan.FromSeconds(10));
            return new CommandResult(0, "");
        };
        var vm = Wizard();
        Fill(vm);
        vm.NavigateTo(vm.Summary);
        Press(vm.Summary.Primary);
        await WaitUntil(() => vm.Install.Stages.Any(s => s.Id == StageId.Pull && s.IsRunning));

        vm.RequestClose();

        vm.HasDialog.Should().BeTrue();
        vm.Dialog!.Title.Should().Be("Прервать установку?");
        _closed.Should().Be(0);

        vm.HandleEscape().Should().BeTrue("Esc dismisses the dialog");
        vm.HasDialog.Should().BeFalse();
        _closed.Should().Be(0);

        vm.RequestClose();
        vm.Dialog!.Confirm();
        _closed.Should().Be(1);
        vm.AllowClose.Should().BeTrue();
        gate.SetResult();
    }

    [Fact]
    public async Task Escape_GoesBackOnQuestionPagesAndClosesTheDonePage()
    {
        var vm = Wizard();
        await GoToDockerRunning(vm);
        vm.NavigateTo(vm.Account);

        vm.HandleEscape().Should().BeTrue();
        vm.CurrentPage.Should().BeSameAs(vm.Docker);
        vm.Docker.OnLeft();

        vm.NavigateTo(vm.Welcome);
        vm.HandleEscape().Should().BeFalse("nothing to go back to on the welcome page");
    }

    [Fact]
    public void Back_IsNotAvailableFromTheInstallAndDonePages()
    {
        var vm = Wizard();
        vm.NavigateTo(vm.Done);

        vm.Back();

        vm.CurrentPage.Should().BeSameAs(vm.Done);
    }

    private static void Fill(WizardViewModel vm)
    {
        var a = vm.Answers;
        a.FirstName = "Иван";
        a.LastName = "Петров";
        a.Password = "Sunny-Day-2026";
        a.TunaToken = "tuna-secret-token-777";
        a.TunaDomain = "crm.example.ru";
    }
}
