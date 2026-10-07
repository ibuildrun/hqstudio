using FluentAssertions;
using HQStudio.Services.Updates;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateViewModelTests
{
    private static UpdateViewModel Vm(UpdateRig rig, List<string>? asked = null, bool confirm = true, List<string>? opened = null) =>
        new(rig.Coordinator, (title, text) => { asked?.Add(title + "|" + text); return confirm; }, url => opened?.Add(url));

    [Fact]
    public void InitialState_ShowsInstalledVersions_AndNoUpdateInfo()
    {
        using var rig = new UpdateRig();
        var vm = Vm(rig);

        vm.InstalledAppVersion.Should().Be("1.19.6");
        vm.InstalledServerVersion.Should().Be("1.19.6");
        vm.LatestVersion.Should().Be("не проверялось");
        vm.HasReleaseInfo.Should().BeFalse();
        vm.IsBusy.Should().BeFalse();
        vm.AppStateText.Should().BeEmpty();
        vm.SiteHint.Should().BeEmpty();
    }

    [Fact]
    public void SiteNotInstalled_DisablesSiteButtons_AndShowsHint()
    {
        using var rig = new UpdateRig(siteInstalled: false);
        var vm = Vm(rig);

        vm.SiteInstalled.Should().BeFalse();
        vm.SiteHint.Should().Be("Сайт не установлен на этом компьютере");
        vm.InstalledServerVersion.Should().Be("не установлен");
        vm.ServerStateText.Should().Be("Не установлен");
        vm.UpdateServerCommand.CanExecute(null).Should().BeFalse();
        vm.UpdateAllCommand.CanExecute(null).Should().BeFalse();
        vm.UpdateAppCommand.CanExecute(null).Should().BeTrue();
        vm.CheckCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Check_FillsVersionsStatesAndNotes()
    {
        using var rig = new UpdateRig();
        var vm = Vm(rig);

        await vm.CheckAsync();

        vm.LatestVersion.Should().Be("1.20.0");
        vm.AppStateText.Should().Be("Доступно обновление");
        vm.ServerStateText.Should().Be("Доступно обновление");
        vm.AnyUpdateAvailable.Should().BeTrue();
        vm.HasReleaseInfo.Should().BeTrue();
        vm.ReleaseTitle.Should().Be("Что нового в версии 1.20.0");
        vm.ReleaseNotes.Should().Contain("one").And.NotContain("##");
        vm.StatusMessage.Should().Contain("Доступно обновление");
        vm.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task Check_NetworkError_ShowsRussianError()
    {
        using var rig = new UpdateRig();
        rig.Github.Respond = _ => throw new System.Net.Http.HttpRequestException("offline");
        var vm = Vm(rig);

        await vm.CheckAsync();

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("интернету");
        vm.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateServer_AsksForConfirmation_AndShowsSuccess()
    {
        using var rig = new UpdateRig();
        var asked = new List<string>();
        var vm = Vm(rig, asked);

        await vm.UpdateServerAsync();

        asked.Should().ContainSingle().Which.Should().Contain("Обновление сайта").And.Contain("Docker Desktop");
        rig.Runner.Calls.Should().Equal("docker version", "docker compose pull", "docker compose up -d --remove-orphans");
        vm.StatusMessage.Should().Contain("Сайт обновлён до версии 1.20.0");
        vm.HasError.Should().BeFalse();
        vm.IsBusy.Should().BeFalse();
        vm.ServerStateText.Should().Be("Актуальная версия");
        vm.LogText.Should().Contain("Docker работает");
    }

    [Fact]
    public async Task UpdateServer_DeclinedConfirmation_DoesNothing()
    {
        using var rig = new UpdateRig();
        var vm = Vm(rig, confirm: false);

        await vm.UpdateServerAsync();

        rig.Runner.Calls.Should().BeEmpty();
        rig.Github.Requests.Should().BeEmpty();
        vm.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateServer_DockerNotRunning_ShowsErrorWithHelpLink()
    {
        using var rig = new UpdateRig();
        rig.Runner.Scripts["version"] = new UpdateFakeScript(ExitCode: 1, StdErr: "error during connect");
        var opened = new List<string>();
        var vm = Vm(rig, opened: opened);

        await vm.UpdateServerAsync();

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Docker Desktop");
        vm.HasHelpUrl.Should().BeTrue();
        vm.OpenHelpCommand.CanExecute(null).Should().BeTrue();
        vm.OpenHelpCommand.Execute(null);
        opened.Should().Equal(UpdateLinks.DockerInstallUrl);
        vm.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAll_ConfirmationMentionsOrder_AndRestartsApp()
    {
        using var rig = new UpdateRig();
        var asked = new List<string>();
        var vm = Vm(rig, asked);
        await vm.CheckAsync();

        await vm.UpdateAllAsync();

        asked.Single().Should().Contain("Сначала обновится сайт").And.Contain("1.20.0");
        rig.Events.TakeLast(2).Should().Equal("launch-helper", "shutdown");
    }

    [Fact]
    public async Task Progress_IsExposedWhileRunning_AndLogIsStreamed()
    {
        using var rig = new UpdateRig();
        var vm = Vm(rig);
        var seen = new List<double>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UpdateViewModel.Progress)) seen.Add(vm.Progress);
        };

        await vm.UpdateServerAsync();

        seen.Should().NotBeEmpty();
        seen.Should().BeInAscendingOrder();
        seen.Max().Should().BeGreaterThan(90);
        vm.ProgressPercentText.Should().EndWith("%");
        vm.LogText.Should().Contain("Сайт обновлён");
    }

    [Fact]
    public void ToggleLog_FlipsVisibilityAndLabel()
    {
        using var rig = new UpdateRig();
        var vm = Vm(rig);
        vm.IsLogVisible.Should().BeFalse();
        vm.LogToggleText.Should().Be("Показать подробности");

        vm.ToggleLogCommand.Execute(null);

        vm.IsLogVisible.Should().BeTrue();
        vm.LogToggleText.Should().Be("Скрыть подробности");
    }

    [Fact]
    public async Task NewViewModel_ReplaysLogOfEarlierOperations()
    {
        using var rig = new UpdateRig();
        await Vm(rig).CheckAsync();

        var second = Vm(rig);

        second.LogText.Should().Contain("Проверяю обновления");
    }
}
