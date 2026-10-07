using System.Text.Json;
using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Sim;
using Xunit;

namespace HQStudio.Setup.Tests;

public class EngineTests
{
    private static readonly StageId[] AllStages =
    {
        StageId.Prepare, StageId.Configure, StageId.DockerReady, StageId.Pull,
        StageId.Start, StageId.Health, StageId.PublicUrl, StageId.Shortcuts
    };

    private static (InstallEngine Engine, InstallContext Context, List<StageId> Started) Create(Rig rig, InstallAnswers answers, Dictionary<StageId, int>? counts = null)
    {
        var context = rig.Context(answers);
        var stages = InstallEngine.DefaultStages().Select(s => counts == null ? s : new CountingStage(s, counts)).ToList();
        var engine = new InstallEngine(context, stages);
        var started = new List<StageId>();
        engine.StageChanged += info =>
        {
            if (info.State == StageState.Running && !started.Contains(info.Id))
                started.Add(info.Id);
        };
        return (engine, context, started);
    }

    [Fact]
    public async Task HappyPath_RunsAllStagesInOrder()
    {
        using var rig = new Rig();
        var (engine, context, started) = Create(rig, Rig.Answers(tuna: true));

        var outcome = await engine.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        started.Should().Equal(AllStages);
        engine.Stages.Select(s => s.State).Should().OnlyContain(s => s == StageState.Done);
        context.SiteInstalled.Should().BeTrue();
        context.PublicUrl.Should().Be("https://mystudio.ru.tuna.am");
    }

    [Fact]
    public async Task HappyPath_WritesEnvWithSecretsAndBlanksTheAdminPasswordAfterSuccess()
    {
        using var rig = new Rig();
        var (engine, _, _) = Create(rig, Rig.Answers(tuna: true));

        await engine.RunAsync(CancellationToken.None);

        var env = File.ReadAllText(rig.Paths.EnvFile);
        EnvFile.Get(env, "ADMIN_PASSWORD").Should().BeEmpty("the plain password is removed once the site works");
        EnvFile.Get(env, "ADMIN_NAME").Should().Be("Иван Петров");
        EnvFile.Get(env, "POSTGRES_PASSWORD").Should().HaveLength(24);
        EnvFile.Get(env, "JWT_KEY").Should().HaveLength(48);
        EnvFile.Get(env, "HQ_PORT").Should().Be("8080");
        EnvFile.Get(env, "HQSTUDIO_VERSION").Should().Be("1.20.0");
        EnvFile.Get(env, "TUNA_SUBDOMAIN").Should().Be("mystudio");
        EnvFile.Get(env, "PUBLIC_URL").Should().Be("https://mystudio.ru.tuna.am");
        File.ReadAllText(rig.Paths.PublicUrlFile).Should().Be("https://mystudio.ru.tuna.am");
    }

    [Fact]
    public async Task AdminPasswordIsInTheEnvFileUntilTheSiteAnswers()
    {
        using var rig = new Rig();
        string? passwordDuringHealthCheck = null;
        rig.Health.Responder = (_, _) =>
        {
            passwordDuringHealthCheck = EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "ADMIN_PASSWORD");
            return true;
        };
        var (engine, _, _) = Create(rig, Rig.Answers());

        await engine.RunAsync(CancellationToken.None);

        passwordDuringHealthCheck.Should().Be("Sunny-Day-2026");
    }

    [Fact]
    public async Task HappyPath_WritesInstallJsonShortcutsRegistryAndSettings()
    {
        using var rig = new Rig();
        var (engine, _, _) = Create(rig, Rig.Answers());

        await engine.RunAsync(CancellationToken.None);

        using var install = JsonDocument.Parse(File.ReadAllText(rig.Paths.InstallJson));
        install.RootElement.GetProperty("serverDir").GetString().Should().Be(rig.Paths.ServerDir);
        install.RootElement.GetProperty("webUrl").GetString().Should().Be("http://localhost:8080");
        install.RootElement.GetProperty("apiUrl").GetString().Should().Be("http://localhost:8080");
        install.RootElement.GetProperty("repo").GetString().Should().Be("ibuildrun/hqstudio");

        File.ReadAllText(rig.Paths.SettingsJson).Should().Contain("\"ApiUrl\":\"http://localhost:8080\"");

        rig.Shortcuts.Created.Select(s => s.Path).Should().Equal(rig.Paths.StartMenuShortcut, rig.Paths.DesktopShortcut);
        rig.Shortcuts.Created.Should().OnlyContain(s => s.Target == rig.Paths.AppExe);

        var entry = rig.Registry.Entries.Should().ContainSingle().Subject;
        entry.DisplayName.Should().Be("HQ Studio");
        entry.DisplayVersion.Should().Be("1.20.0");
        entry.Publisher.Should().Be("HQ Studio");
        entry.InstallLocation.Should().Be(rig.Paths.AppDir);
        entry.DisplayIcon.Should().Be(rig.Paths.AppExe);
        entry.UninstallString.Should().Be($"\"{rig.Paths.AppExe}\" --uninstall");
    }

    [Fact]
    public async Task DesktopShortcut_IsSkippedWhenTheUserUnticksIt()
    {
        using var rig = new Rig();
        var (engine, _, _) = Create(rig, Rig.Answers(desktopShortcut: false));

        await engine.RunAsync(CancellationToken.None);

        rig.Shortcuts.Created.Select(s => s.Path).Should().Equal(rig.Paths.StartMenuShortcut);
    }

    [Fact]
    public async Task ExistingSettingsJson_IsNotOverwritten()
    {
        using var rig = new Rig();
        Directory.CreateDirectory(rig.Paths.SettingsDir);
        File.WriteAllText(rig.Paths.SettingsJson, "{\"Theme\":\"Light\"}");
        var (engine, _, _) = Create(rig, Rig.Answers());

        await engine.RunAsync(CancellationToken.None);

        File.ReadAllText(rig.Paths.SettingsJson).Should().Be("{\"Theme\":\"Light\"}");
    }

    [Fact]
    public async Task Compose_IsCalledWithTheTunnelProfileOnlyWhenATokenWasGiven()
    {
        using var withTuna = new Rig();
        var (engineA, _, _) = Create(withTuna, Rig.Answers(tuna: true));
        await engineA.RunAsync(CancellationToken.None);

        using var withoutTuna = new Rig();
        var (engineB, _, _) = Create(withoutTuna, Rig.Answers(tuna: false));
        await engineB.RunAsync(CancellationToken.None);

        withTuna.Docker.Calls.Should().OnlyContain(c => c.Tunnel);
        withTuna.Docker.CountOf("logs").Should().BeGreaterThan(0);
        withoutTuna.Docker.Calls.Should().OnlyContain(c => !c.Tunnel);
        withoutTuna.Docker.CountOf("logs").Should().Be(0);
    }

    [Fact]
    public void TunnelStage_IsListedOnlyWhenATokenWasGiven()
    {
        using var rig = new Rig();

        new InstallEngine(rig.Context(Rig.Answers(tuna: true))).Stages.Select(s => s.Id).Should().Contain(StageId.PublicUrl);
        new InstallEngine(rig.Context(Rig.Answers(tuna: false))).Stages.Select(s => s.Id).Should().NotContain(StageId.PublicUrl);
    }

    [Fact]
    public void ExistingTunaTokenInEnv_KeepsTheTunnelStageOnReinstall()
    {
        using var rig = new Rig();
        Directory.CreateDirectory(rig.Paths.ServerDir);
        File.WriteAllText(rig.Paths.EnvFile, "TUNA_TOKEN=saved-token-12345\n");

        var engine = new InstallEngine(rig.Context(Rig.Answers(tuna: false)));

        engine.Stages.Select(s => s.Id).Should().Contain(StageId.PublicUrl);
    }

    [Fact]
    public async Task DockerSkipped_OnlyInstallsTheProgram()
    {
        using var rig = new Rig();
        var (engine, context, started) = Create(rig, Rig.Answers(tuna: true, skipSite: true));

        var outcome = await engine.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        started.Should().Equal(StageId.Prepare, StageId.Shortcuts);
        rig.Docker.Calls.Should().BeEmpty();
        rig.Docker.StatusCalls.Should().Be(0);
        rig.Health.Probes.Should().BeEmpty();
        File.Exists(rig.Paths.EnvFile).Should().BeFalse("no secrets are written when the site is skipped");
        File.Exists(rig.Paths.InstallJson).Should().BeFalse();
        File.Exists(rig.Paths.AppExe).Should().BeTrue();
        File.Exists(rig.Paths.SettingsJson).Should().BeTrue();
        rig.Registry.Entries.Should().ContainSingle();
        context.SiteInstalled.Should().BeFalse();
    }

    [Theory]
    [InlineData(StageId.Prepare)]
    [InlineData(StageId.Configure)]
    [InlineData(StageId.DockerReady)]
    [InlineData(StageId.Pull)]
    [InlineData(StageId.Start)]
    [InlineData(StageId.Health)]
    [InlineData(StageId.PublicUrl)]
    [InlineData(StageId.Shortcuts)]
    public async Task FailureAtAnyStage_ThenRetry_ResumesWithoutRepeatingFinishedStages(StageId failing)
    {
        using var rig = new Rig { Injector = new FailOnceInjector(failing) };
        var counts = new Dictionary<StageId, int>();
        var (engine, _, _) = Create(rig, Rig.Answers(tuna: true), counts);

        var first = await engine.RunAsync(CancellationToken.None);

        first.Should().Be(InstallOutcome.Failed);
        engine.LastFailure.Should().NotBeNull();
        engine.LastFailure!.Stage.Should().Be(failing);
        engine.Stages.Single(s => s.Id == failing).State.Should().Be(StageState.Failed);
        foreach (var earlier in AllStages.TakeWhile(s => s != failing))
            engine.Stages.Single(s => s.Id == earlier).State.Should().Be(StageState.Done);
        foreach (var later in AllStages.SkipWhile(s => s != failing).Skip(1))
            engine.Stages.Single(s => s.Id == later).State.Should().Be(StageState.Pending);

        var second = await engine.RunAsync(CancellationToken.None);

        second.Should().Be(InstallOutcome.Success);
        engine.LastFailure.Should().BeNull();
        engine.Stages.Should().OnlyContain(s => s.State == StageState.Done);
        foreach (var stage in AllStages)
            counts[stage].Should().Be(1, $"{stage} must run exactly once: finished stages are not repeated after the retry");
    }

    [Fact]
    public async Task InjectedFailureCarriesAHumanExplanation()
    {
        using var rig = new Rig { Injector = new FailOnceInjector(StageId.Pull) };
        var (engine, _, _) = Create(rig, Rig.Answers());

        await engine.RunAsync(CancellationToken.None);

        engine.LastFailure!.Kind.Should().Be(FailureKind.Network);
        engine.LastFailure.Hint.Should().Contain("VPN");
    }

    [Fact]
    public async Task Progress_GrowsMonotonicallyAndEndsAtOne()
    {
        using var rig = new Rig();
        var (engine, _, _) = Create(rig, Rig.Answers(tuna: true));
        var values = new List<double>();
        engine.ProgressChanged += (fraction, _) => values.Add(fraction);

        await engine.RunAsync(CancellationToken.None);

        values.Should().BeInAscendingOrder();
        values.Last().Should().Be(1.0);
        values.Should().Contain(v => v > 0 && v < 1);
    }

    [Fact]
    public async Task Cancellation_StopsTheRunAndLeavesTheStagePending()
    {
        using var rig = new Rig();
        using var cts = new CancellationTokenSource();
        rig.Docker.Handler = call =>
        {
            if (call.Verb == "pull")
                cts.Cancel();
            return new CommandResult(0, "");
        };
        var (engine, _, _) = Create(rig, Rig.Answers());

        var outcome = await engine.RunAsync(cts.Token);

        outcome.Should().Be(InstallOutcome.Cancelled);
        engine.Stages.Single(s => s.Id == StageId.Start).State.Should().Be(StageState.Pending);
        engine.LastFailure.Should().BeNull();
    }

    [Fact]
    public async Task Reinstall_KeepsSecretsAndDoesNotTouchTheDatabaseKeys()
    {
        using var rig = new Rig();
        var (first, _, _) = Create(rig, Rig.Answers());
        await first.RunAsync(CancellationToken.None);
        var envBefore = File.ReadAllText(rig.Paths.EnvFile);

        var (second, context, _) = Create(rig, Rig.Answers());
        var outcome = await second.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        var envAfter = File.ReadAllText(rig.Paths.EnvFile);
        EnvFile.Get(envAfter, "POSTGRES_PASSWORD").Should().Be(EnvFile.Get(envBefore, "POSTGRES_PASSWORD"));
        EnvFile.Get(envAfter, "JWT_KEY").Should().Be(EnvFile.Get(envBefore, "JWT_KEY"));
        context.KeptExistingDatabase.Should().BeTrue();
    }

    [Fact]
    public async Task Log_NeverContainsThePasswordOrKeys()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "up"
            ? new CommandResult(0, "creating db with password Sunny-Day-2026 and token tuna-secret-token-777 key AIzaSyTestKey123456")
            : new CommandResult(0, "");
        var log = new SetupLog(rig.Paths.SetupLog);
        var answers = Rig.Answers(tuna: true);
        var context = rig.Context(answers, log);
        var engine = new InstallEngine(context);

        await engine.RunAsync(CancellationToken.None);

        var fileText = File.ReadAllText(rig.Paths.SetupLog);
        var env = File.ReadAllText(rig.Paths.EnvFile);
        foreach (var secret in new[] { "Sunny-Day-2026", "AIzaSyTestKey123456", "tuna-secret-token-777", EnvFile.Get(env, "POSTGRES_PASSWORD")!, EnvFile.Get(env, "JWT_KEY")! })
        {
            fileText.Should().NotContain(secret);
            string.Join("\n", log.Snapshot()).Should().NotContain(secret);
        }
        fileText.Should().Contain("Установка завершена");
    }
}

public class StageBehaviourTests
{
    private static async Task<(InstallEngine Engine, InstallContext Context)> RunAsync(Rig rig, InstallAnswers answers)
    {
        var context = rig.Context(answers);
        var engine = new InstallEngine(context);
        await engine.RunAsync(CancellationToken.None);
        return (engine, context);
    }

    [Fact]
    public async Task Port8080Busy_SelectsPort8081AndWritesItEverywhere()
    {
        using var rig = new Rig();
        rig.Ports.Busy.Add(8080);

        var (engine, context) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        context.Port.Should().Be(8081);
        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "HQ_PORT").Should().Be("8081");
        rig.Health.Probes.Should().Contain(8081);
        File.ReadAllText(rig.Paths.InstallJson).Should().Contain("http://localhost:8081");
        File.ReadAllText(rig.Paths.SettingsJson).Should().Contain("http://localhost:8081");
    }

    [Fact]
    public async Task AllPortsBusy_FailsOnConfigureWithPortBusy()
    {
        using var rig = new Rig();
        foreach (var port in Enumerable.Range(8080, 11))
            rig.Ports.Busy.Add(port);

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure!.Stage.Should().Be(StageId.Configure);
        engine.LastFailure.Kind.Should().Be(FailureKind.PortBusy);
        rig.Docker.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task PortTakenAtStartTime_MovesToTheNextPortBeforeFailing()
    {
        using var rig = new Rig();
        var upCalls = 0;
        rig.Docker.Handler = call =>
        {
            if (call.Verb == "up" && ++upCalls == 1)
                return new CommandResult(1, "Error response from daemon: Ports are not available: listen tcp 127.0.0.1:8080: bind: address already in use");
            return new CommandResult(0, "");
        };

        var (engine, context) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        upCalls.Should().Be(2);
        context.Port.Should().Be(8081);
        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "HQ_PORT").Should().Be("8081");
    }

    [Fact]
    public async Task PortConflictOnEveryPort_EndsInPortBusyAfterTryingTheWholeRange()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "up"
            ? new CommandResult(1, "Bind for 127.0.0.1 failed: port is already allocated")
            : new CommandResult(0, "");

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure!.Stage.Should().Be(StageId.Start);
        engine.LastFailure.Kind.Should().Be(FailureKind.PortBusy);
        rig.Docker.CountOf("up").Should().Be(11, "8080 through 8090 are each tried once");
    }

    [Fact]
    public async Task ReinstallOnTheSamePort_RecognisesItsOwnRunningSite()
    {
        using var rig = new Rig();
        await RunAsync(rig, Rig.Answers());
        // The first install's proxy now holds 8080 and answers /api/health.
        rig.Ports.Busy.Add(8080);

        var (engine, context) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        context.Port.Should().Be(8080);
    }

    [Fact]
    public async Task Pull_RetriesThreeTimesThenReportsNetwork()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "pull"
            ? new CommandResult(1, "Error response from daemon: Get \"https://ghcr.io/v2/\": dial tcp: lookup ghcr.io: no such host")
            : new CommandResult(0, "");

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        rig.Docker.CountOf("pull").Should().Be(3);
        engine.LastFailure!.Stage.Should().Be(StageId.Pull);
        engine.LastFailure.Kind.Should().Be(FailureKind.Network);
    }

    [Fact]
    public async Task Pull_SucceedsOnTheSecondAttempt()
    {
        using var rig = new Rig();
        var pulls = 0;
        rig.Docker.Handler = call => call.Verb == "pull" && ++pulls == 1 ? new CommandResult(1, "i/o timeout") : new CommandResult(0, "");

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        pulls.Should().Be(2);
    }

    [Fact]
    public async Task Pull_DoesNotRetryWhenDockerIsNotRunning()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "pull"
            ? new CommandResult(1, "error during connect: the docker daemon is not running")
            : new CommandResult(0, "");

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        rig.Docker.CountOf("pull").Should().Be(1);
        engine.LastFailure!.Kind.Should().Be(FailureKind.DockerNotRunning);
    }

    [Fact]
    public async Task Pull_ReportsLayerAndMegabyteProgressToTheStageDetail()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "pull"
            ? new CommandResult(0,
                " Image ghcr.io/x/api:1 Pulling \n aaaaaaaaaaaa Pulling fs layer \n bbbbbbbbbbbb Pulling fs layer \n" +
                " aaaaaaaaaaaa Downloading [=>   ]  10.0MB/40.0MB\n aaaaaaaaaaaa Pull complete \n bbbbbbbbbbbb Pull complete \n Image ghcr.io/x/api:1 Pulled \n")
            : new CommandResult(0, "");
        var context = rig.Context(Rig.Answers());
        var engine = new InstallEngine(context);
        var details = new List<string>();
        engine.StageChanged += info =>
        {
            if (info.Id == StageId.Pull && info.State == StageState.Running && info.Detail != null)
                details.Add(info.Detail);
        };

        await engine.RunAsync(CancellationToken.None);

        details.Should().Contain(d => d.Contains("МБ") && d.Contains("из 2"));
    }

    [Fact]
    public async Task DockerStopped_IsStartedAndWaitedFor()
    {
        using var rig = new Rig();
        rig.Docker.QueueStatuses(DockerStatus.InstalledNotRunning, DockerStatus.InstalledNotRunning, DockerStatus.InstalledNotRunning, DockerStatus.Running);

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        rig.Docker.DesktopStarts.Should().Be(1);
        rig.Delay.Calls.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DockerNeverStarts_FailsWithDockerNotRunning()
    {
        using var rig = new Rig();
        rig.Docker.Status = DockerStatus.InstalledNotRunning;

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure!.Stage.Should().Be(StageId.DockerReady);
        engine.LastFailure.Kind.Should().Be(FailureKind.DockerNotRunning);
        rig.Docker.StatusCalls.Should().BeGreaterThan(40);
    }

    [Fact]
    public async Task DockerMissing_FailsWithDockerNotInstalled()
    {
        using var rig = new Rig();
        rig.Docker.Status = DockerStatus.NotInstalled;

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure!.Kind.Should().Be(FailureKind.DockerNotInstalled);
    }

    [Fact]
    public async Task HealthNeverAnswers_FailsAndKeepsTheAdminPasswordForTheRetry()
    {
        using var rig = new Rig();
        rig.Health.Responder = (_, _) => false;

        var (engine, context) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure!.Stage.Should().Be(StageId.Health);
        engine.LastFailure.Kind.Should().Be(FailureKind.HealthTimeout);
        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "ADMIN_PASSWORD").Should().Be("Sunny-Day-2026");
        File.Exists(rig.Paths.InstallJson).Should().BeFalse();
        context.SiteInstalled.Should().BeFalse();
        rig.Docker.CountOf("ps").Should().Be(1, "container state is collected for the bug report");
    }

    [Fact]
    public async Task HealthAnswersAfterSomeTime_Succeeds()
    {
        using var rig = new Rig();
        rig.Health.Responder = (_, call) => call >= 5;

        var (engine, _) = await RunAsync(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        rig.Health.Probes.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task TunnelAddressMissing_IsAWarningNotAFailure()
    {
        using var rig = new Rig();
        rig.Docker.Handler = call => call.Verb == "logs" ? new CommandResult(0, "tuna-1 | connecting...") : new CommandResult(0, "");

        var (engine, context) = await RunAsync(rig, Rig.Answers(tuna: true));

        engine.LastFailure.Should().BeNull();
        engine.Stages.Single(s => s.Id == StageId.PublicUrl).State.Should().Be(StageState.Warning);
        engine.Stages.Single(s => s.Id == StageId.Shortcuts).State.Should().Be(StageState.Done);
        context.PublicUrl.Should().BeNull();
        context.PublicUrlNote.Should().Contain("токен");
    }

    [Fact]
    public async Task MissingPayload_ReportsAClearMessageOnTheFirstStage()
    {
        using var rig = new Rig();
        var services = new SetupServices
        {
            Paths = rig.Paths, Docker = rig.Docker, DockerInstaller = rig.Installer, Health = rig.Health, Ports = rig.Ports,
            Shortcuts = rig.Shortcuts, Registry = rig.Registry, Shell = rig.Shell, Payload = new FakePayload(null),
            Delay = rig.Delay, Version = "1.0.0"
        };
        var engine = new InstallEngine(new InstallContext(Rig.Answers(), services, new SetupLog(null)));

        var outcome = await engine.RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Failed);
        engine.LastFailure!.Stage.Should().Be(StageId.Prepare);
        engine.LastFailure.Kind.Should().Be(FailureKind.PayloadMissing);
        engine.LastFailure.Message.Should().Contain("payload.zip");
    }

    [Fact]
    public async Task DevVersion_UsesTheLatestImageTag()
    {
        using var rig = new Rig(version: "0.0.0-dev");

        await RunAsync(rig, Rig.Answers());

        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "HQSTUDIO_VERSION").Should().Be("latest");
    }

    [Theory]
    [InlineData("1.20.0", "1.20.0")]
    [InlineData("0.0.0-dev", "latest")]
    [InlineData("", "latest")]
    [InlineData("2.0.0-dev.3", "latest")]
    public void ImageTag_FollowsTheInstallerVersion(string version, string expected)
    {
        VersionInfo.ImageTag(version).Should().Be(expected);
    }
}
