using System.IO;
using FluentAssertions;
using HQStudio.Services.Site;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UninstallViewModelTests
{
    private const string AppDir = @"C:\Tools\HQ Studio";
    private const string LocalRoot = @"C:\Users\x\AppData\Local\HQStudio";
    private static readonly string ServerDir = Path.Combine(LocalRoot, "server");

    private sealed class Rig
    {
        public SiteFakeFiles Files { get; } = new();
        public SiteFakeRunner Runner { get; } = new();
        public SiteFakeRegistry Registry { get; } = new();
        public UninstallViewModel Vm { get; }
        public int CloseRequests { get; private set; }

        public Rig()
        {
            Files.Add(Path.Combine(AppDir, "HQStudio.exe"));
            Files.Add(Path.Combine(ServerDir, ".env"), SiteTestEnv.BaseEnv);
            var install = new SiteFakeInstallStore
            {
                Result = new SiteInstallReadResult(new SiteInstallInfo(ServerDir, "u", "u", "r"), null)
            };
            var locations = new UninstallLocations(AppDir, AppDir, @"C:\Temp\", @"C:\Start", @"C:\Desktop",
                new SitePaths(LocalRoot), new[] { @"C:\Windows" });
            var service = new UninstallService(Files, Runner, new SiteFakeLocator(), install, Registry, locations);
            Vm = new UninstallViewModel(service, AppDir);
            Vm.CloseRequested += (_, _) => CloseRequests++;
        }
    }

    [Fact]
    public void Initial_ConfirmPhase_WithFivePendingStages_AndDataKeptByDefault()
    {
        var vm = new Rig().Vm;

        vm.IsConfirm.Should().BeTrue();
        vm.IsRunning.Should().BeFalse();
        vm.IsDone.Should().BeFalse();
        vm.ShowStages.Should().BeFalse();
        vm.DeleteData.Should().BeFalse();
        vm.ShowDataWarning.Should().BeFalse();
        vm.Stages.Should().HaveCount(5);
        vm.Stages.Should().OnlyContain(s => s.State == UninstallStageState.Pending && s.StateText == "Ожидает");
        vm.StartCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void TickingDeleteData_ShowsTheWarning()
    {
        var vm = new Rig().Vm;

        vm.DeleteData = true;
        vm.ShowDataWarning.Should().BeTrue();

        vm.DeleteData = false;
        vm.ShowDataWarning.Should().BeFalse();
    }

    [Fact]
    public void StageTitles_AreInRussianInTheDocumentedOrder()
    {
        var vm = new Rig().Vm;

        vm.Stages.Select(s => s.Stage).Should().Equal(
            UninstallStage.StopSite, UninstallStage.Shortcuts, UninstallStage.Registry, UninstallStage.SiteFiles, UninstallStage.Program);
        vm.Stages.Should().OnlyContain(s => s.Title.Length > 0);
    }

    [Fact]
    public async Task Run_KeepData_FinishesWithDoneState_AndKeepsDataMessage()
    {
        var rig = new Rig();

        await rig.Vm.RunAsync();

        var vm = rig.Vm;
        vm.IsDone.Should().BeTrue();
        vm.Headline.Should().Be("Готово");
        vm.Progress.Should().Be(100);
        vm.HasProblems.Should().BeFalse();
        vm.Stages.Should().OnlyContain(s => s.State == UninstallStageState.Done);
        vm.Summary.Should().Contain("Данные сайта (клиенты, заказы) остались на этом компьютере");
        rig.Runner.ComposeCommands(ServerDir).Should().Equal("down --remove-orphans");
    }

    [Fact]
    public async Task Run_DeleteData_UsesVolumes_AndSaysDataIsGone()
    {
        var rig = new Rig();
        rig.Vm.DeleteData = true;

        await rig.Vm.RunAsync();

        rig.Runner.ComposeCommands(ServerDir).Should().Equal("down --remove-orphans --volumes");
        rig.Vm.Summary.Should().Contain("Программа и данные сайта удалены.");
    }

    [Fact]
    public async Task Run_DockerOff_FinishesWithRemarks_AndDoesNotClaimTheDataIsGone()
    {
        var rig = new Rig();
        rig.Runner.When("down", 1, "", "error during connect: open //./pipe/dockerDesktopLinuxEngine");
        rig.Vm.DeleteData = true;

        await rig.Vm.RunAsync();

        rig.Vm.Headline.Should().Be("Готово, но есть замечания");
        rig.Vm.HasProblems.Should().BeTrue();
        rig.Vm.Stages.First().State.Should().Be(UninstallStageState.Skipped);
        rig.Vm.Stages.First().Note.Should().Contain("Docker выключен");
        rig.Vm.Summary.Should().StartWith("Программа удалена.").And.NotContain("Программа и данные сайта удалены.");
        rig.Vm.Summary.Should().Contain("Docker выключен");
    }

    [Fact]
    public async Task Run_StageFailure_IsShownAsErrors()
    {
        var rig = new Rig();
        rig.Registry.OnDelete = _ => throw new UnauthorizedAccessException("denied");

        await rig.Vm.RunAsync();

        rig.Vm.Headline.Should().Be("Удаление завершено с ошибками");
        rig.Vm.Stages.Single(s => s.Stage == UninstallStage.Registry).State.Should().Be(UninstallStageState.Failed);
        rig.Vm.Stages.Single(s => s.Stage == UninstallStage.Registry).StateText.Should().Be("Ошибка");
        rig.Vm.HasProblems.Should().BeTrue();
    }

    [Fact]
    public async Task Run_PhaseGoesConfirmRunningDone_AndProgressNeverGoesBack()
    {
        var rig = new Rig();
        var phases = new List<UninstallPhase>();
        var progress = new List<double>();
        rig.Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UninstallViewModel.Phase))
                phases.Add(rig.Vm.Phase);
            if (e.PropertyName == nameof(UninstallViewModel.Progress))
                progress.Add(rig.Vm.Progress);
        };

        await rig.Vm.RunAsync();

        phases.Should().Equal(UninstallPhase.Running, UninstallPhase.Done);
        progress.Should().BeInAscendingOrder().And.EndWith(100);
    }

    [Fact]
    public async Task Run_WhileRunning_CannotBeStartedAgain_NorCancelled()
    {
        var rig = new Rig();
        var gate = new TaskCompletionSource();
        rig.Runner.Gate = gate.Task;

        var running = rig.Vm.RunAsync();

        rig.Vm.IsRunning.Should().BeTrue();
        rig.Vm.ShowStages.Should().BeTrue();
        rig.Vm.StartCommand.CanExecute(null).Should().BeFalse();
        rig.Vm.CancelCommand.CanExecute(null).Should().BeFalse();
        await rig.Vm.RunAsync();
        rig.Runner.Calls.Should().HaveCountLessThanOrEqualTo(1);

        gate.SetResult();
        await running;
        rig.Vm.IsDone.Should().BeTrue();
    }

    [Fact]
    public void Cancel_InConfirmPhase_AsksTheWindowToClose()
    {
        var rig = new Rig();

        rig.Vm.CancelCommand.Execute(null);

        rig.CloseRequests.Should().Be(1);
    }

    [Fact]
    public async Task AfterDone_CloseRaisesTheCloseEvent()
    {
        var rig = new Rig();
        await rig.Vm.RunAsync();

        rig.Vm.Close();

        rig.CloseRequests.Should().Be(1);
    }

    [Fact]
    public async Task Run_NeverTouchesAnythingOutsideTheFakes()
    {
        var rig = new Rig();

        await rig.Vm.RunAsync();

        rig.Runner.Detached.Should().ContainSingle().Which.File.Should().Be("cmd.exe");
        rig.Registry.Deleted.Should().Equal("HQStudio");
    }
}
