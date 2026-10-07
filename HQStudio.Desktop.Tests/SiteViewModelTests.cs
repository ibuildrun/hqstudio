using FluentAssertions;
using HQStudio.Services.Site;
using HQStudio.ViewModels;
using Xunit;

namespace HQStudio.Desktop.Tests;

internal static class SiteWait
{
    /// <summary>Ждёт условие, не гадая о времени: тесты не должны зависеть от скорости пула потоков.</summary>
    public static async Task Until(Func<bool> condition, int timeoutMs = 5000)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Условие не выполнилось вовремя.");
            await Task.Delay(5);
        }
    }
}

public class SiteViewModelTests
{
    private sealed class Rig
    {
        public SiteFakeService Service { get; } = new();
        public SiteFakeShell Shell { get; } = new();
        public SiteFakeDialogs Dialogs { get; } = new();
        public SiteFakeUninstallHost Host { get; } = new();
        public SiteFakeNotifier Notifier { get; } = new();
        public SiteViewModel Vm { get; }

        public Rig() => Vm = new SiteViewModel(Service, Shell, Dialogs, Host, Notifier);
    }

    private static SiteSnapshot Snap(SitePill pill, SiteDockerState docker = SiteDockerState.Running, bool installed = true,
        string? publicUrl = null, bool tunnel = false) =>
        new(installed, docker, new SiteOverview(pill, pill.ToString(), "Объяснение"), SiteStatusEvaluator.PlaceholderServices(),
            "1.19.6", installed ? "http://localhost:8080" : "", publicUrl, tunnel, null);

    // ---------------------------------------------------------------- состояние

    [Fact]
    public void InitialState_IsCheckingWithFivePlaceholderCards()
    {
        var vm = new Rig().Vm;

        vm.Pill.Should().Be(SitePill.Checking);
        vm.Services.Should().HaveCount(5);
        vm.IsBusy.Should().BeFalse();
        vm.StartCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Apply_Running_EnablesStopRestartAndOpen_NotStart()
    {
        var vm = new Rig().Vm;

        vm.Apply(SiteTestEnv.RunningSnapshot());

        vm.PillText.Should().Be("Работает");
        vm.Version.Should().Be("1.19.6");
        vm.LocalUrl.Should().Be("http://localhost:8080");
        vm.StartCommand.CanExecute(null).Should().BeFalse();
        vm.StopCommand.CanExecute(null).Should().BeTrue();
        vm.RestartCommand.CanExecute(null).Should().BeTrue();
        vm.OpenSiteCommand.CanExecute(null).Should().BeTrue();
        vm.KeysCommand.CanExecute(null).Should().BeTrue();
        vm.LogsCommand.CanExecute(null).Should().BeTrue();
        vm.UpdatesCommand.CanExecute(null).Should().BeTrue();
        vm.UninstallCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Apply_Stopped_EnablesStartOnly()
    {
        var vm = new Rig().Vm;

        vm.Apply(Snap(SitePill.Stopped));

        vm.StartCommand.CanExecute(null).Should().BeTrue();
        vm.StopCommand.CanExecute(null).Should().BeFalse();
        vm.RestartCommand.CanExecute(null).Should().BeFalse();
        vm.OpenSiteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Apply_DockerNotRunning_ShowsStartDockerButton_AndStillAllowsStart()
    {
        var vm = new Rig().Vm;

        vm.Apply(Snap(SitePill.DockerDown, SiteDockerState.NotRunning));

        vm.ShowStartDockerBanner.Should().BeTrue();
        vm.StartDockerCommand.CanExecute(null).Should().BeTrue();
        vm.StartCommand.CanExecute(null).Should().BeTrue();
        vm.StopCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Apply_DockerMissing_HasNoStartDockerButton()
    {
        var vm = new Rig().Vm;

        vm.Apply(Snap(SitePill.DockerDown, SiteDockerState.Missing));

        vm.ShowStartDockerBanner.Should().BeFalse();
        vm.StartDockerCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Apply_NotInstalled_ShowsFriendlyCard_AndDisablesEverything()
    {
        var vm = new Rig().Vm;

        vm.Apply(Snap(SitePill.NotInstalled, SiteDockerState.Unknown, installed: false));

        vm.ShowNotInstalled.Should().BeTrue();
        vm.ShowSiteContent.Should().BeFalse();
        vm.StartCommand.CanExecute(null).Should().BeFalse();
        vm.KeysCommand.CanExecute(null).Should().BeFalse();
        vm.LogsCommand.CanExecute(null).Should().BeFalse();
        vm.OpenSiteCommand.CanExecute(null).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://hq.ru.tuna.am", true, "https://hq.ru.tuna.am", true)]
    [InlineData(null, true, "Адрес ещё не получен", false)]
    [InlineData(null, false, "Не настроен", false)]
    public void PublicUrlText_ExplainsEachCase(string? url, bool tunnel, string expected, bool hasUrl)
    {
        var vm = new Rig().Vm;

        vm.Apply(Snap(SitePill.Running, publicUrl: url, tunnel: tunnel));

        vm.PublicUrlText.Should().Be(expected);
        vm.HasPublicUrl.Should().Be(hasUrl);
        vm.CopyPublicCommand.CanExecute(null).Should().Be(hasUrl);
    }

    [Fact]
    public void Apply_SameServicesTwice_DoesNotRebuildCards()
    {
        var vm = new Rig().Vm;
        var raised = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SiteViewModel.Services))
                raised++;
        };
        var snapshot = SiteTestEnv.RunningSnapshot();

        vm.Apply(snapshot);
        vm.Apply(snapshot with { Version = "1.20.0" });

        raised.Should().Be(1);
        vm.Version.Should().Be("1.20.0");
    }

    [Fact]
    public void ShowPreview_SetsBusyAndErrorState()
    {
        var vm = new Rig().Vm;

        vm.ShowPreview(SiteTestEnv.RunningSnapshot(), "Идёт работа", true,
            new SiteFailure(SiteFailureKind.PortBusy, "Порт занят", "Закройте программу", "details"));

        vm.IsBusy.Should().BeTrue();
        vm.BusyText.Should().Be("Идёт работа");
        vm.CanCancel.Should().BeTrue();
        vm.ErrorTitle.Should().Be("Порт занят");
        vm.HasErrorDetails.Should().BeTrue();
    }

    // ---------------------------------------------------------------- опрос

    [Fact]
    public async Task Refresh_AppliesTheServiceSnapshot()
    {
        var rig = new Rig();
        rig.Service.Snapshot = Snap(SitePill.Stopped);

        await rig.Vm.RefreshAsync();

        rig.Vm.Pill.Should().Be(SitePill.Stopped);
    }

    [Fact]
    public async Task Refresh_OverlappingCalls_AreCoalescedIntoOneFollowUp()
    {
        var rig = new Rig();
        var gate = new TaskCompletionSource<SiteSnapshot>();
        var first = true;
        rig.Service.Refresh = _ =>
        {
            if (!first)
                return Task.FromResult(Snap(SitePill.Stopped));
            first = false;
            return gate.Task;
        };

        var running = rig.Vm.RefreshAsync();
        await SiteWait.Until(() => rig.Service.RefreshCalls == 1);
        await rig.Vm.RefreshAsync();
        await rig.Vm.RefreshAsync();
        await rig.Vm.RefreshAsync();
        rig.Service.RefreshCalls.Should().Be(1);

        gate.SetResult(Snap(SitePill.Running));
        await running;

        rig.Service.RefreshCalls.Should().Be(2);
        rig.Vm.Pill.Should().Be(SitePill.Stopped);
    }

    [Fact]
    public async Task Refresh_ServiceThrows_KeepsPreviousState_AndDoesNotCrash()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Running));
        rig.Service.Refresh = _ => throw new InvalidOperationException("boom");

        var act = () => rig.Vm.RefreshAsync();

        await act.Should().NotThrowAsync();
        rig.Vm.Pill.Should().Be(SitePill.Running);
    }

    [Fact]
    public async Task Refresh_AfterAFailure_WorksAgain()
    {
        var rig = new Rig();
        rig.Service.Refresh = _ => throw new InvalidOperationException("boom");
        await rig.Vm.RefreshAsync();
        rig.Service.Refresh = null;
        rig.Service.Snapshot = Snap(SitePill.Stopped);

        await rig.Vm.RefreshAsync();

        rig.Vm.Pill.Should().Be(SitePill.Stopped);
    }

    [Fact]
    public async Task ActivateDeactivate_TogglesTimer_AndStopsIt()
    {
        var rig = new Rig();

        rig.Vm.Activate();
        rig.Vm.Activate();
        rig.Vm.IsActive.Should().BeTrue();
        await SiteWait.Until(() => rig.Service.RefreshCalls >= 1);

        rig.Vm.Deactivate();
        rig.Vm.IsActive.Should().BeFalse();
        rig.Vm.Deactivate();
        rig.Vm.IsActive.Should().BeFalse();
    }

    // ---------------------------------------------------------------- действия

    [Fact]
    public async Task Start_Success_ShowsToast_AndPassesStartingToRefresh()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));

        await rig.Vm.StartSiteAsync();

        rig.Notifier.Messages.Should().Contain("success:Сайт запущен.");
        rig.Vm.IsBusy.Should().BeFalse();
        rig.Vm.HasError.Should().BeFalse();
        await SiteWait.Until(() => rig.Service.RefreshOperations.Contains(SiteOperation.Starting));
    }

    [Fact]
    public async Task Start_WhileRunning_ShowsBusyState_ThenReleases()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        var gate = new TaskCompletionSource<SiteOperationResult>();
        rig.Service.StartHandler = (_, _) => gate.Task;

        var running = rig.Vm.StartSiteAsync();

        rig.Vm.IsBusy.Should().BeTrue();
        rig.Vm.BusyText.Should().Be("Запускаю сайт");
        rig.Vm.CanCancel.Should().BeTrue();
        rig.Vm.StartCommand.CanExecute(null).Should().BeFalse();
        rig.Vm.KeysCommand.CanExecute(null).Should().BeFalse();
        rig.Vm.UninstallCommand.CanExecute(null).Should().BeFalse();

        gate.SetResult(SiteOperationResult.Ok("Сайт запущен."));
        await running;

        rig.Vm.IsBusy.Should().BeFalse();
        rig.Vm.CanCancel.Should().BeFalse();
    }

    [Fact]
    public async Task Start_ProgressTextFromTheService_ReachesTheBusyLine()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        var gate = new TaskCompletionSource<SiteOperationResult>();
        rig.Service.StartHandler = (status, _) =>
        {
            status?.Invoke("Жду, пока сайт ответит");
            return gate.Task;
        };

        var running = rig.Vm.StartSiteAsync();
        await SiteWait.Until(() => rig.Vm.BusyText == "Жду, пока сайт ответит");
        gate.SetResult(SiteOperationResult.Ok("ok"));
        await running;
    }

    [Fact]
    public async Task Start_Failure_ShowsErrorCard_WithDetails_AndErrorToast()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        rig.Service.StartHandler = (_, _) => Task.FromResult(SiteOperationResult.Fail(
            new SiteFailure(SiteFailureKind.PortBusy, "Порт занят", "Сайт не смог занять свой порт.", "Bind for 127.0.0.1:8080 failed")));

        await rig.Vm.StartSiteAsync();

        rig.Vm.HasError.Should().BeTrue();
        rig.Vm.ErrorTitle.Should().Be("Порт занят");
        rig.Vm.ErrorMessage.Should().Be("Сайт не смог занять свой порт.");
        rig.Vm.ErrorDetails.Should().Contain("Bind for");
        rig.Vm.HasErrorDetails.Should().BeTrue();
        rig.Notifier.Messages.Should().Contain("error:Порт занят");
    }

    [Fact]
    public async Task NewOperation_ClearsThePreviousError()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        rig.Service.StartHandler = (_, _) => Task.FromResult(SiteOperationResult.Fail(SiteErrorMapper.DockerMissing()));
        await rig.Vm.StartSiteAsync();
        rig.Vm.HasError.Should().BeTrue();
        rig.Service.StartHandler = (_, _) => Task.FromResult(SiteOperationResult.Ok("ok"));

        await rig.Vm.StartSiteAsync();

        rig.Vm.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task DismissError_ClearsTheCard()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        rig.Service.StartHandler = (_, _) => Task.FromResult(SiteOperationResult.Fail(SiteErrorMapper.DockerMissing()));
        await rig.Vm.StartSiteAsync();

        rig.Vm.DismissError();

        rig.Vm.HasError.Should().BeFalse();
        rig.Vm.ErrorTitle.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancelled_IsShownAsInfo_NotAsError()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        var started = new TaskCompletionSource();
        rig.Service.StartHandler = async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return SiteOperationResult.Ok("never");
        };

        var running = rig.Vm.StartSiteAsync();
        await started.Task;
        rig.Vm.CancelCommand.CanExecute(null).Should().BeTrue();
        rig.Vm.CancelCommand.Execute(null);
        await running;

        rig.Vm.HasError.Should().BeFalse();
        rig.Notifier.Messages.Should().ContainSingle().Which.Should().StartWith("info:");
        rig.Vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task SecondStartWhileBusy_IsIgnored()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Stopped));
        var gate = new TaskCompletionSource<SiteOperationResult>();
        var calls = 0;
        rig.Service.StartHandler = (_, _) =>
        {
            calls++;
            return gate.Task;
        };

        var first = rig.Vm.StartSiteAsync();
        await rig.Vm.StartSiteAsync();
        gate.SetResult(SiteOperationResult.Ok("ok"));
        await first;

        calls.Should().Be(1);
    }

    [Fact]
    public async Task Stop_UsesStopHandler_AndToasts()
    {
        var rig = new Rig();
        rig.Vm.Apply(SiteTestEnv.RunningSnapshot());

        await rig.Vm.StopSiteAsync();

        rig.Notifier.Messages.Should().Contain("success:Сайт остановлен.");
        await SiteWait.Until(() => rig.Service.RefreshOperations.Contains(SiteOperation.Stopping));
    }

    // ---------------------------------------------------------------- адреса, окна, удаление

    [Fact]
    public void CopyAndOpen_UseTheShell()
    {
        var rig = new Rig();
        rig.Vm.Apply(Snap(SitePill.Running, publicUrl: "https://hq.ru.tuna.am", tunnel: true));

        rig.Vm.CopyLocalCommand.Execute(null);
        rig.Vm.CopyPublicCommand.Execute(null);
        rig.Vm.OpenLocalCommand.Execute(null);
        rig.Vm.OpenPublicCommand.Execute(null);
        rig.Vm.OpenSiteCommand.Execute(null);

        rig.Shell.Copied.Should().Equal("http://localhost:8080", "https://hq.ru.tuna.am");
        rig.Shell.Opened.Should().Equal("http://localhost:8080", "https://hq.ru.tuna.am", "http://localhost:8080");
        rig.Notifier.Messages.Should().Contain("success:Адрес скопирован");
    }

    [Fact]
    public void CopyFailure_AndOpenFailure_AreWarnings()
    {
        var rig = new Rig();
        rig.Shell.CopyResult = false;
        rig.Shell.OpenResult = false;
        rig.Vm.Apply(Snap(SitePill.Running));

        rig.Vm.CopyLocalCommand.Execute(null);
        rig.Vm.OpenLocalCommand.Execute(null);

        rig.Notifier.Messages.Should().HaveCount(2).And.OnlyContain(m => m.StartsWith("warning:"));
    }

    [Fact]
    public async Task KeysLogsUpdates_OpenTheirDialogs()
    {
        var rig = new Rig();
        rig.Vm.Apply(SiteTestEnv.RunningSnapshot());

        rig.Vm.KeysCommand.Execute(null);
        rig.Vm.LogsCommand.Execute(null);
        rig.Vm.UpdatesCommand.Execute(null);
        await SiteWait.Until(() => rig.Service.RefreshCalls >= 2);

        rig.Dialogs.KeysShown.Should().Be(1);
        rig.Dialogs.LogsShown.Should().Be(1);
        rig.Dialogs.UpdatesShown.Should().Be(1);
    }

    [Fact]
    public async Task Uninstall_Declined_DoesNothing()
    {
        var rig = new Rig();
        rig.Dialogs.ConfirmResult = false;

        rig.Vm.UninstallCommand.Execute(null);
        await Task.Delay(30);

        rig.Host.Launched.Should().Be(0);
        rig.Host.ShutDown.Should().Be(0);
    }

    [Fact]
    public async Task Uninstall_Confirmed_LaunchesTheCopy_ThenShutsTheAppDown()
    {
        var rig = new Rig();

        rig.Vm.UninstallCommand.Execute(null);
        await SiteWait.Until(() => rig.Host.ShutDown == 1);

        rig.Host.Launched.Should().Be(1);
    }

    [Fact]
    public async Task Uninstall_LaunchFails_ShowsErrorAndKeepsTheAppOpen()
    {
        var rig = new Rig();
        rig.Host.Result = SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.Other, "Не удалось начать удаление",
            "Не получилось подготовить удаление.", "details"));

        rig.Vm.UninstallCommand.Execute(null);
        await SiteWait.Until(() => rig.Vm.HasError);

        rig.Host.ShutDown.Should().Be(0);
        rig.Vm.ErrorTitle.Should().Be("Не удалось начать удаление");
    }
}

public class SiteKeysViewModelTests
{
    private static (SiteKeysViewModel Vm, SiteFakeService Service, SiteFakeShell Shell) Make(SiteKeysState? state = null)
    {
        var service = new SiteFakeService { Keys = state ?? new SiteKeysState(false, false, "") };
        var shell = new SiteFakeShell();
        return (new SiteKeysViewModel(service, shell), service, shell);
    }

    [Fact]
    public void Fresh_BothFieldsAreInEditMode_AndNothingToSave()
    {
        var (vm, _, _) = Make();

        vm.ShowGeminiInput.Should().BeTrue();
        vm.ShowTunaInput.Should().BeTrue();
        vm.ShowGeminiSaved.Should().BeFalse();
        vm.HasChanges.Should().BeFalse();
        vm.CanSave.Should().BeFalse();
        vm.BuildUpdate().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Saved_ShowsMaskedRow_AndNeverExposesValues()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "hq"));

        vm.ShowGeminiSaved.Should().BeTrue();
        vm.ShowTunaSaved.Should().BeTrue();
        vm.ShowGeminiInput.Should().BeFalse();
        vm.GeminiInput.Should().BeEmpty();
        vm.TunaInput.Should().BeEmpty();
        vm.Subdomain.Should().Be("hq");
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void Change_SwitchesToInput_AndKeepGoesBack()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, false, ""));

        vm.ChangeGeminiCommand.Execute(null);
        vm.ShowGeminiInput.Should().BeTrue();
        vm.CanCancelGeminiEdit.Should().BeTrue();
        vm.GeminiInput = "typed";

        vm.KeepGeminiCommand.Execute(null);

        vm.ShowGeminiSaved.Should().BeTrue();
        vm.GeminiInput.Should().BeEmpty();
        vm.BuildUpdate().GeminiKey.Should().BeNull();
    }

    [Fact]
    public void BuildUpdate_EditTrimsAndOnlyIncludesChangedFields()
    {
        var (vm, _, _) = Make();

        vm.GeminiInput = "  AIzaKey123  ";
        vm.Subdomain = "my-site";

        var update = vm.BuildUpdate();
        update.GeminiKey.Should().Be("AIzaKey123");
        update.TunaToken.Should().BeNull();
        update.TunaSubdomain.Should().Be("my-site");
        vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void BuildUpdate_ClearMeansEmptyString()
    {
        var (vm, _, _) = Make(new SiteKeysState(true, true, "hq"));

        vm.ClearGeminiCommand.Execute(null);
        vm.ClearTunaCommand.Execute(null);

        var update = vm.BuildUpdate();
        update.GeminiKey.Should().Be("");
        update.TunaToken.Should().Be("");
        vm.ShowGeminiCleared.Should().BeTrue();
        vm.ShowTunaCleared.Should().BeTrue();
        vm.CanSave.Should().BeTrue();

        vm.KeepTunaCommand.Execute(null);
        vm.BuildUpdate().TunaToken.Should().BeNull();
    }

    [Fact]
    public void BuildUpdate_SubdomainUnchanged_IsNotSent_ClearingItIs()
    {
        var (vm, _, _) = Make(new SiteKeysState(false, true, "hq"));
        vm.BuildUpdate().TunaSubdomain.Should().BeNull();

        vm.Subdomain = "";

        vm.BuildUpdate().TunaSubdomain.Should().Be("");
    }

    [Theory]
    [InlineData("Мой Сайт")]
    [InlineData("UPPER")]
    [InlineData("with space")]
    [InlineData("-dash")]
    public void InvalidSubdomain_ShowsMessage_AndBlocksSave(string value)
    {
        var (vm, _, _) = Make();
        vm.GeminiInput = "AIzaKey123";

        vm.Subdomain = value;

        vm.SubdomainError.Should().Contain("латинские буквы");
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void KeyWithSpaces_ShowsMessage_AndBlocksSave()
    {
        var (vm, _, _) = Make();

        vm.GeminiInput = "has space";
        vm.GeminiInputError.Should().NotBeEmpty();
        vm.CanSave.Should().BeFalse();

        vm.GeminiInput = "";
        vm.TunaInput = "bad$token";
        vm.TunaInputError.Should().NotBeEmpty();
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public async Task Save_SendsTheUpdate_AndReloadsState()
    {
        var (vm, service, _) = Make();
        service.ApplyHandler = (_, _, _) =>
        {
            service.Keys = new SiteKeysState(true, false, "");
            return Task.FromResult(SiteOperationResult.Ok("Настройки сохранены и применены."));
        };
        vm.GeminiInput = "AIzaKey123";

        await vm.SaveAsync();

        service.Applied.Should().ContainSingle().Which.Should().Be(new SiteKeysUpdate("AIzaKey123", null, null));
        vm.ResultMessage.Should().Be("Настройки сохранены и применены.");
        vm.ResultIsError.Should().BeFalse();
        vm.Completed.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
        vm.ShowGeminiSaved.Should().BeTrue();
        vm.GeminiInput.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_Failure_KeepsTheTypedValues_AndShowsDetails()
    {
        var (vm, service, _) = Make();
        service.ApplyHandler = (_, _, _) => Task.FromResult(SiteOperationResult.Fail(
            new SiteFailure(SiteFailureKind.InvalidInput, "Проверьте данные", "Токен неверный.", "подробности")));
        vm.TunaInput = "badtoken";

        await vm.SaveAsync();

        vm.ResultIsError.Should().BeTrue();
        vm.ResultMessage.Should().Be("Токен неверный.");
        vm.ErrorDetails.Should().Be("подробности");
        vm.Completed.Should().BeFalse();
        vm.TunaInput.Should().Be("badtoken");
    }

    [Fact]
    public async Task Save_SavedButNotApplied_StillCountsAsCompleted()
    {
        var (vm, service, _) = Make();
        service.ApplyHandler = (_, _, _) => Task.FromResult(new SiteOperationResult(false,
            "Настройки сохранены, но применить их не получилось.", SiteErrorMapper.DockerMissing(), ConfigSaved: true));
        vm.GeminiInput = "AIzaKey123";

        await vm.SaveAsync();

        vm.Completed.Should().BeTrue();
        vm.ResultIsError.Should().BeTrue();
    }

    [Fact]
    public async Task Save_ShowsProgressText_AndBlocksSecondSave()
    {
        var (vm, service, _) = Make();
        var gate = new TaskCompletionSource<SiteOperationResult>();
        service.ApplyHandler = (_, status, _) =>
        {
            status?.Invoke("Применяю настройки");
            return gate.Task;
        };
        vm.GeminiInput = "AIzaKey123";

        var saving = vm.SaveAsync();

        vm.IsBusy.Should().BeTrue();
        vm.CanSave.Should().BeFalse();
        await SiteWait.Until(() => vm.StatusText == "Применяю настройки");
        await vm.SaveAsync();
        service.Applied.Should().HaveCount(1);

        gate.SetResult(SiteOperationResult.Ok("ok"));
        await saving;
        vm.StatusText.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_StopsTheRunningSave()
    {
        var (vm, service, _) = Make();
        var started = new TaskCompletionSource();
        service.ApplyHandler = async (_, _, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return SiteOperationResult.Ok("never");
        };
        vm.GeminiInput = "AIzaKey123";

        var saving = vm.SaveAsync();
        await started.Task;
        vm.Cancel();
        await saving;

        vm.ResultIsError.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void HelpLinks_OpenTheRightPages()
    {
        var (vm, _, shell) = Make();

        vm.OpenGeminiHelpCommand.Execute(null);
        vm.OpenTunaHelpCommand.Execute(null);

        shell.Opened.Should().Equal("https://aistudio.google.com/apikey", "https://tuna.am");
    }

    [Fact]
    public void NotInstalled_StateIsNull_ViewModelStillWorks()
    {
        var service = new SiteFakeService { Keys = null };

        var vm = new SiteKeysViewModel(service, new SiteFakeShell());

        vm.HasGemini.Should().BeFalse();
        vm.ShowGeminiInput.Should().BeTrue();
    }
}

public class SiteLogsViewModelTests
{
    private static (SiteLogsViewModel Vm, SiteFakeService Service, SiteFakeShell Shell, SiteFakeNotifier Notifier) Make()
    {
        var service = new SiteFakeService();
        var shell = new SiteFakeShell();
        var notifier = new SiteFakeNotifier();
        return (new SiteLogsViewModel(service, shell, notifier), service, shell, notifier);
    }

    [Fact]
    public void Starts_WithFiveServices_ApiSelected()
    {
        var (vm, _, _, _) = Make();

        vm.Services.Select(s => s.Id).Should().Equal("db", "api", "web", "proxy", "tuna");
        vm.Services.Single(s => s.IsSelected).Id.Should().Be("api");
    }

    [Fact]
    public async Task Refresh_LoadsTextAndShowsLineCount()
    {
        var (vm, service, _, _) = Make();
        service.LogsHandler = _ => Task.FromResult(new SiteLogsResult(true, "one\ntwo\nthree", null));

        await vm.RefreshAsync();

        vm.LogText.Should().Be("one\ntwo\nthree");
        vm.StatusText.Should().StartWith("Строк: 3");
        vm.IsLoading.Should().BeFalse();
        vm.HasError.Should().BeFalse();
        service.LogRequests.Should().Equal("api");
    }

    [Fact]
    public async Task Refresh_Failure_ShowsFriendlyMessage_AndClearsText()
    {
        var (vm, service, _, _) = Make();
        await vm.RefreshAsync();
        service.LogsHandler = _ => Task.FromResult(new SiteLogsResult(false, "", SiteErrorMapper.DockerNotRunning()));

        await vm.RefreshAsync();

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Запустить Docker");
        vm.LogText.Should().BeEmpty();
        vm.CopyCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Selecting_AnotherService_LoadsItsLog_AndMovesTheHighlight()
    {
        var (vm, service, _, _) = Make();

        vm.SelectedService = "web";
        await SiteWait.Until(() => !vm.IsLoading && vm.LogText == "log of web");

        service.LogRequests.Should().Contain("web");
        vm.Services.Single(s => s.IsSelected).Id.Should().Be("web");
    }

    [Fact]
    public void SelectCommand_ChangesTheSelection()
    {
        var (vm, _, _, _) = Make();

        vm.SelectCommand.Execute("proxy");

        vm.SelectedService.Should().Be("proxy");
    }

    [Fact]
    public async Task StaleResponse_DoesNotOverwriteNewerChoice()
    {
        var (vm, service, _, _) = Make();
        var slow = new TaskCompletionSource<SiteLogsResult>();
        service.LogsHandler = id => id == "api" ? slow.Task : Task.FromResult(new SiteLogsResult(true, "fast web log", null));

        var first = vm.RefreshAsync();
        await SiteWait.Until(() => service.LogRequests.Count == 1);
        vm.SelectedService = "web";
        await SiteWait.Until(() => vm.LogText == "fast web log");
        slow.SetResult(new SiteLogsResult(true, "slow api log", null));
        await first;

        vm.LogText.Should().Be("fast web log");
    }

    [Fact]
    public async Task Copy_PutsTheLogOnTheClipboard()
    {
        var (vm, _, shell, notifier) = Make();
        await vm.RefreshAsync();

        vm.CopyCommand.Execute(null);

        shell.Copied.Should().Equal("log of api");
        notifier.Messages.Should().Contain("success:Журнал скопирован");
    }

    [Fact]
    public async Task Copy_Failure_IsAWarning()
    {
        var (vm, _, shell, notifier) = Make();
        shell.CopyResult = false;
        await vm.RefreshAsync();

        vm.CopyCommand.Execute(null);

        notifier.Messages.Should().ContainSingle().Which.Should().StartWith("warning:");
    }
}
