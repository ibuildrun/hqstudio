using System.ComponentModel;
using FluentAssertions;
using HQStudio.Services.Site;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class SiteManagerStartStopTests
{
    private static string Dir => SiteTestEnv.ServerDir;

    [Fact]
    public async Task Start_ChecksDocker_ThenUp_ThenWaitsForHealth()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Сайт запущен.");
        env.Events.Should().Equal(
            "run:version --format {{.Server.Version}}",
            $"run:compose --project-directory {Dir} -f {Dir}\\docker-compose.yml up -d --remove-orphans");
        env.Probe.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Start_WithoutToken_NeverUsesTunnelProfile()
    {
        var env = new SiteTestEnv();

        await env.Manager.StartAsync(null, CancellationToken.None);

        env.Runner.ComposeCommands(Dir).Should().Equal("up -d --remove-orphans");
    }

    [Theory]
    [InlineData("tunatoken1234", "")]
    [InlineData("", "crm.example.ru")]
    public async Task Start_WithOnlyTokenOrOnlyDomain_NeverUsesTunnelProfile(string token, string domain)
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken(token, domain));

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Сайт запущен.");
        env.Runner.ComposeCommands(Dir).Should().Equal("up -d --remove-orphans");
    }

    [Fact]
    public async Task Start_WithTokenAndDomain_AddsTunnelProfile_ChecksTunnelContainer_AndReportsTheDomain()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken()).WithPs(SiteTestEnv.AllRunning(tunnel: true));

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("https://crm.example.ru");
        env.Runner.ComposeCommands(Dir).Should().Equal(
            "[tunnel] up -d --remove-orphans",
            "[tunnel] ps --all --format json",
            "[tunnel] ps --all --format json");
        env.Files.Files[SiteTestEnv.PublicUrlPath].Should().Be("https://crm.example.ru");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://crm.example.ru");
    }

    [Fact]
    public async Task Start_PublicUrlAlreadyMatchesTheDomain_DoesNotRewriteEnv()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken().Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://crm.example.ru\r\n"))
            .WithPs(SiteTestEnv.AllRunning(tunnel: true));

        await env.Manager.StartAsync(null, CancellationToken.None);

        env.Files.Log.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_TunnelContainerDoesNotStayUp_SucceedsButExplainsWhatToCheck()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken())
            .WithPs(SiteTestEnv.Entry("db"), SiteTestEnv.Entry("api"), SiteTestEnv.Entry("web"),
                SiteTestEnv.Entry("proxy", health: ""), SiteTestEnv.Entry("tuna", "restarting", "", "Restarting (1) 5 seconds ago"));

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("пока не заработал").And.Contain("my.tuna.am/domains").And.Contain("токен Tuna");
        result.Message.Should().NotContain("Адрес в интернете: https");
    }

    [Fact]
    public async Task Start_TunnelCheckSeesRunningOnlyOnce_IsNotEnough()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());
        var polls = 0;
        env.Runner.When(call => call.Contains(" ps --all --format json"), () => ++polls == 2
            ? new SiteProcessResult(0, SiteTestEnv.PsJson(SiteTestEnv.AllRunning(tunnel: true)), "")
            : new SiteProcessResult(0, SiteTestEnv.PsJson(SiteTestEnv.Entry("tuna", "restarting", "", "Restarting")), ""));

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Message.Should().Contain("пока не заработал");
    }

    [Fact]
    public async Task Start_WhenEngineIsDown_StartsDockerDesktop_AndWaitsForIt()
    {
        var env = new SiteTestEnv();
        var versionCalls = 0;
        env.Runner.When(call => call.StartsWith("version"), () =>
            ++versionCalls < 3 ? SiteTestEnv.DockerDown() : new SiteProcessResult(0, "27.0", ""));
        var statuses = new List<string>();

        var result = await env.Manager.StartAsync(statuses.Add, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Runner.Detached.Should().ContainSingle().Which.File.Should().Be(@"C:\docker\Docker Desktop.exe");
        statuses.Should().Contain(s => s.StartsWith("Запускаю Docker")).And.Contain("Запускаю сайт");
        env.Runner.ComposeCommands(Dir).Should().Contain("up -d --remove-orphans");
    }

    [Fact]
    public async Task Start_DockerNeverComesUp_FailsWithTimeout_AndNeverCallsCompose()
    {
        var env = new SiteTestEnv();
        env.Runner.When("version", 1, "", "error during connect: open //./pipe/dockerDesktopLinuxEngine");

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.Timeout);
        env.Runner.ComposeCommands(Dir).Should().BeEmpty();
    }

    [Fact]
    public async Task Start_DockerMissing_ReportsIt()
    {
        var env = new SiteTestEnv();
        env.Locator.Docker = null;

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.DockerMissing);
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_DockerDesktopExecutableMissing_ReportsDockerMissing()
    {
        var env = new SiteTestEnv();
        env.Locator.Desktop = null;
        env.Runner.When("version", 1, "", "error during connect");

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.DockerMissing);
    }

    [Fact]
    public async Task Start_ComposeFailsBecauseOfBusyPort_GivesFriendlyMessage()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" up -d", 1, "", "Error response from daemon: Bind for 127.0.0.1:8080 failed: port is already allocated");

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.PortBusy);
        result.Failure.Details.Should().Contain("port is already allocated");
        env.Probe.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Start_SiteNeverAnswers_FailsWithTimeoutAfterTheConfiguredAttempts()
    {
        var env = new SiteTestEnv();
        env.Probe.Healthy = _ => false;

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.Timeout);
        env.Probe.Calls.Should().Be(3);
    }

    [Fact]
    public async Task Start_SiteAnswersOnTheSecondTry_Succeeds()
    {
        var env = new SiteTestEnv();
        env.Probe.Healthy = attempt => attempt >= 2;

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Probe.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Refresh_UsesCustomPortFromEnv()
    {
        var env = new SiteTestEnv(SiteTestEnv.BaseEnv.Replace("HQ_PORT=8080", "HQ_PORT=9090")).WithPs(SiteTestEnv.AllRunning());

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.LocalUrl.Should().Be("http://localhost:9090");
    }

    [Theory]
    [InlineData("HQ_PORT=abc")]
    [InlineData("HQ_PORT=0")]
    [InlineData("HQ_PORT=70000")]
    [InlineData("")]
    public async Task Refresh_BadPortFallsBackTo8080(string portLine)
    {
        var env = new SiteTestEnv(SiteTestEnv.BaseEnv.Replace("HQ_PORT=8080", portLine)).WithPs(SiteTestEnv.AllRunning());

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.LocalUrl.Should().Be("http://localhost:8080");
    }

    [Fact]
    public async Task Start_Cancelled_ReturnsCancelledNotError()
    {
        var env = new SiteTestEnv();
        using var cts = new CancellationTokenSource();
        env.Runner.OnCall = call =>
        {
            if (call.Contains(" up -d"))
                cts.Cancel();
        };
        env.Runner.When(" up -d", 0);
        env.Probe.Healthy = _ => false;

        var result = await env.Manager.StartAsync(null, cts.Token);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.Cancelled);
    }

    [Fact]
    public async Task Start_NotInstalled_ReturnsFriendlyFailure()
    {
        var env = new SiteTestEnv();
        env.Install.Result = new SiteInstallReadResult(null, null);

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.NotInstalled);
    }

    [Fact]
    public async Task Start_EnvFileMissing_ExplainsReinstall()
    {
        var env = new SiteTestEnv();
        env.Files.Files.Clear();

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("установщик");
    }

    [Fact]
    public async Task Stop_RunsComposeStop()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.StopAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Сайт остановлен.");
        env.Runner.ComposeCommands(Dir).Should().Equal("stop");
    }

    [Fact]
    public async Task Stop_WithTokenAndDomain_IncludesTunnelProfile_SoTheTunnelStopsToo()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());

        await env.Manager.StopAsync(null, CancellationToken.None);

        env.Runner.ComposeCommands(Dir).Should().Equal("[tunnel] stop");
    }

    [Fact]
    public async Task Stop_WhenDockerIsOff_IsAlreadyStopped()
    {
        var env = new SiteTestEnv();
        env.Runner.When("version", 1, "", "error during connect");

        var result = await env.Manager.StopAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("уже остановлен");
        env.Runner.ComposeCommands(Dir).Should().BeEmpty();
    }

    [Fact]
    public async Task Stop_ComposeFailure_IsReported()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" stop", 1, "", "something odd");

        var result = await env.Manager.StopAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.ComposeFailed);
    }

    [Fact]
    public async Task Restart_StopsFirst_ThenStartsAndWaits()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.RestartAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Сайт перезапущен.");
        env.Runner.ComposeCommands(Dir).Should().Equal("stop", "up -d --remove-orphans");
        env.Probe.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Restart_DoesNotStartWhenStopFailed()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" stop", 1, "", "boom");

        var result = await env.Manager.RestartAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        env.Runner.ComposeCommands(Dir).Should().Equal("stop");
    }

    [Fact]
    public async Task StartDocker_AlreadyRunning_DoesNotLaunchAnything()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.StartDockerAsync(null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Runner.Detached.Should().BeEmpty();
    }

    [Fact]
    public async Task StartDocker_LaunchesDockerDesktopOnce_AndReportsProgress()
    {
        var env = new SiteTestEnv();
        var versionCalls = 0;
        env.Runner.When(call => call.StartsWith("version"), () =>
            ++versionCalls < 4 ? SiteTestEnv.DockerDown() : new SiteProcessResult(0, "27.0", ""));
        var statuses = new List<string>();

        var result = await env.Manager.StartDockerAsync(statuses.Add, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Runner.Detached.Should().ContainSingle();
        statuses.Should().Contain(s => s.Contains("Жду, пока Docker включится"));
    }

    [Fact]
    public async Task StartDocker_CanBeCancelled()
    {
        var env = new SiteTestEnv();
        env.Runner.When("version", 1, "", "error during connect");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await env.Manager.StartDockerAsync(null, cts.Token);

        result.Failure!.Kind.Should().Be(SiteFailureKind.Cancelled);
    }

    [Fact]
    public async Task Operations_DoNotOverlap()
    {
        var env = new SiteTestEnv();
        var gate = new TaskCompletionSource();
        env.Runner.Gate = gate.Task;

        var first = env.Manager.StartAsync(null, CancellationToken.None);
        var second = await env.Manager.StopAsync(null, CancellationToken.None);
        gate.SetResult();
        await first;

        second.Success.Should().BeFalse();
        second.Message.Should().Contain("ещё выполняется");
    }

    [Fact]
    public async Task Start_ProcessThatCannotBeLaunched_MeansDockerMissing()
    {
        var env = new SiteTestEnv();
        env.Runner.ThrowOnRun = new Win32Exception(2, "docker.exe not found");

        var result = await env.Manager.StartAsync(null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.DockerMissing);
    }
}

public class SiteManagerKeysTests
{
    private static string Dir => SiteTestEnv.ServerDir;

    private static SiteTestEnv RunningTunnelEnv(string envText)
    {
        var env = new SiteTestEnv(envText);
        env.WithPs(SiteTestEnv.AllRunning(tunnel: true));
        return env;
    }

    [Fact]
    public async Task Apply_WritesEnv_ThenUp_ThenChecksTheTunnelContainer()
    {
        var env = RunningTunnelEnv(SiteTestEnv.BaseEnv);
        var timeline = new List<string>();

        var result = await ApplyAndMergeTimeline(env, timeline, new SiteKeysUpdate("tunatoken1234", "crm.example.ru"));

        result.Success.Should().BeTrue();
        var order = timeline.Where(e => e is "write:.env" or "run:version --format {{.Server.Version}}"
                                         || e.StartsWith("run:[tunnel] up")
                                         || e.StartsWith("run:[tunnel] ps")
                                         || e == "write:public-url.txt").ToList();
        order.Should().StartWith(new[]
        {
            "write:.env",
            "write:public-url.txt",
            "run:version --format {{.Server.Version}}",
            "run:[tunnel] up -d --remove-orphans",
            "run:[tunnel] ps --all --format json"
        });
    }

    // Совмещает вызовы процессов и записи файлов в одну ленту в порядке выполнения.
    private static async Task<SiteOperationResult> ApplyAndMergeTimeline(SiteTestEnv env, List<string> timeline,
        SiteKeysUpdate update)
    {
        var prefix = $"compose --project-directory {Dir} -f {Dir}\\docker-compose.yml ";
        env.Runner.OnCall = call =>
        {
            var text = call.StartsWith(prefix, StringComparison.Ordinal) ? call[prefix.Length..] : call;
            text = text.StartsWith("--profile tunnel ") ? "[tunnel] " + text["--profile tunnel ".Length..] : text;
            timeline.Add("run:" + text);
        };
        env.Files.Timeline = timeline;
        return await env.Manager.ApplyKeysAsync(update, null, CancellationToken.None);
    }

    [Fact]
    public async Task Apply_SavesTokenAndDomain_AndKeepsEveryOtherLineOfEnv()
    {
        var env = RunningTunnelEnv(SiteTestEnv.BaseEnv + SiteTestEnv.LegacyLines);

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("tunatoken1234");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        env.Env.Should().Contain("# HQ Studio settings\r\n")
            .And.Contain("POSTGRES_PASSWORD=pgsecret123\r\n")
            .And.Contain("JWT_KEY=jwtsecret456789\r\n")
            .And.Contain("HQSTUDIO_VERSION=1.19.6\r\n")
            .And.Contain(SiteTestEnv.LegacyLines);
    }

    [Fact]
    public async Task Apply_SecretsNeverReachProgressTextOrResultMessages()
    {
        var env = RunningTunnelEnv(SiteTestEnv.BaseEnv);
        var statuses = new List<string>();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), statuses.Add, CancellationToken.None);

        string.Join("|", statuses).Should().NotContain("tunatoken1234");
        result.Message.Should().NotContain("tunatoken1234");
        env.Runner.Calls.Should().NotContain(c => c.Contains("tunatoken1234"));
    }

    [Fact]
    public async Task Apply_Domain_WritesPublicUrlToEnvAndFile_WithASingleUp()
    {
        var env = RunningTunnelEnv(SiteTestEnv.BaseEnv);

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), null, CancellationToken.None);

        result.Message.Should().Contain("https://crm.example.ru");
        env.Files.Files[SiteTestEnv.PublicUrlPath].Should().Be("https://crm.example.ru");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://crm.example.ru");
        env.Runner.ComposeCommands(Dir).Where(c => c.Contains("up -d")).Should().ContainSingle();
    }

    [Fact]
    public async Task Apply_TunnelDoesNotStayUp_StillSucceeds_WithHint()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("пока не заработал").And.Contain("my.tuna.am/domains");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
    }

    [Fact]
    public async Task Apply_TokenWithoutDomain_NoTunnel_ExplainsThatTheDomainIsNeeded()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", null), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("свой домен");
        env.Runner.ComposeCommands(Dir).Should().Contain("up -d --remove-orphans")
            .And.NotContain(c => c.StartsWith("[tunnel] up") || c.Contains(" ps "));
    }

    [Fact]
    public async Task Apply_DomainWithoutToken_NoTunnel_ExplainsThatTheTokenIsNeeded()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("токен Tuna");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://crm.example.ru");
        env.Runner.ComposeCommands(Dir).Should().Equal("up -d --remove-orphans");
    }

    [Fact]
    public async Task Apply_NothingChanged_TouchesNothing()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, null), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        env.Files.Log.Should().BeEmpty();
        env.Runner.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("dollar$")]
    [InlineData("quo\"te")]
    public async Task Apply_TokenWithBreakingCharacters_IsRejected(string token)
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(token, null), null, CancellationToken.None);

        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        env.Files.Log.Should().BeEmpty();
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_DockerOff_SavesAndExplainsItWillApplyOnNextStart()
    {
        var env = new SiteTestEnv();
        env.Runner.When("version", 1, "", "error during connect");

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("когда вы запустите сайт");
        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("tunatoken1234");
        env.Runner.ComposeCommands(Dir).Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_DockerMissing_SavesButReportsNotApplied()
    {
        var env = new SiteTestEnv();
        env.Locator.Docker = null;

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", null), null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ConfigSaved.Should().BeTrue();
        result.Message.Should().StartWith("Настройки сохранены, но применить их не получилось.");
        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("tunatoken1234");
    }

    [Fact]
    public async Task Apply_UpFails_KeepsSavedSettings_AndSaysSo()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" up -d", 1, "", "lookup registry-1.docker.io: no such host");

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", null), null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ConfigSaved.Should().BeTrue();
        result.Failure!.Kind.Should().Be(SiteFailureKind.NoInternet);
        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("tunatoken1234");
    }

    [Fact]
    public async Task Apply_EnvWriteFails_ReportsAndRunsNothing()
    {
        var env = new SiteTestEnv();
        env.Files.FailWrite = _ => true;

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", null), null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ConfigSaved.Should().BeFalse();
        env.Runner.Calls.Should().BeEmpty();
        env.Env.Should().Be(SiteTestEnv.BaseEnv);
    }

    [Fact]
    public async Task Apply_RemovingDomain_StopsTunnelContainer_AndClearsPublicAddress()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken().Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://crm.example.ru\r\n"));
        env.Files.Add(SiteTestEnv.PublicUrlPath, "https://crm.example.ru");

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, ""), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("отключён");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("");
        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("tunatoken1234");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("");
        env.Files.Files.Should().NotContainKey(SiteTestEnv.PublicUrlPath);
        env.Runner.ComposeCommands(Dir).Should().Contain("[tunnel] rm --stop --force tuna");
        env.Runner.ComposeCommands(Dir).First().Should().Be("up -d --remove-orphans");
    }

    [Fact]
    public async Task Apply_RemovingToken_StopsTunnelContainer_ButKeepsTheDomain()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken().Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://crm.example.ru\r\n"));

        var result = await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("", null), null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("отключён");
        SiteEnvFile.GetValue(env.Env, "TUNA_TOKEN").Should().Be("");
        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("crm.example.ru");
        env.Runner.ComposeCommands(Dir).Should().Contain("[tunnel] rm --stop --force tuna");
    }

    [Fact]
    public async Task Apply_RemovingDomain_LeavesForeignPublicUrlAlone()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken().Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://hq.example.org\r\n"));

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, ""), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://hq.example.org");
    }

    [Fact]
    public async Task Apply_ChangingTheDomain_ReplacesPublicUrl()
    {
        var env = RunningTunnelEnv(SiteTestEnv.EnvWithToken().Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://crm.example.ru\r\n"));

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate(null, "new.example.ru"), null, CancellationToken.None);

        SiteEnvFile.GetValue(env.Env, "TUNA_DOMAIN").Should().Be("new.example.ru");
        SiteEnvFile.GetValue(env.Env, "PUBLIC_URL").Should().Be("https://new.example.ru");
        env.Files.Files[SiteTestEnv.PublicUrlPath].Should().Be("https://new.example.ru");
    }

    [Fact]
    public async Task Apply_LinesTheProgramNoLongerKnows_StayExactlyAsTheyWere()
    {
        var env = RunningTunnelEnv(SiteTestEnv.BaseEnv + SiteTestEnv.LegacyLines);

        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("tunatoken1234", "crm.example.ru"), null, CancellationToken.None);
        await env.Manager.ApplyKeysAsync(new SiteKeysUpdate("", ""), null, CancellationToken.None);

        env.Env.Should().Contain(SiteTestEnv.LegacyLines);
    }

    [Fact]
    public void ReadKeysState_ReportsPresenceButNeverTheValues()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "crm.example.ru"));

        var state = env.Manager.ReadKeysState();

        state.Should().Be(new SiteKeysState(true, "crm.example.ru"));
        state!.ToString().Should().NotContain("tunatoken1234");
    }

    [Fact]
    public void ReadKeysState_FreshInstall_HasNothing()
    {
        new SiteTestEnv().Manager.ReadKeysState().Should().Be(new SiteKeysState(false, ""));
    }

    [Fact]
    public void ReadKeysState_EnvWithUnknownLegacyLines_IsStillReadable()
    {
        var env = new SiteTestEnv(SiteTestEnv.BaseEnv + SiteTestEnv.LegacyLines);

        env.Manager.ReadKeysState().Should().Be(new SiteKeysState(false, ""));
    }

    [Fact]
    public void ReadKeysState_NotInstalled_ReturnsNull()
    {
        var env = new SiteTestEnv();
        env.Install.Result = new SiteInstallReadResult(null, null);

        env.Manager.ReadKeysState().Should().BeNull();
    }
}

public class SiteManagerLogsAndRefreshTests
{
    private static string Dir => SiteTestEnv.ServerDir;

    [Fact]
    public async Task GetLogs_RequestsLast300Lines_OfTheChosenService()
    {
        var env = new SiteTestEnv();
        env.Runner.When("logs --no-color --tail 300 api", 0, "api-1  | started\napi-1  | listening");

        var result = await env.Manager.GetLogsAsync("api", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Text.Should().Contain("started");
        env.Runner.ComposeCommands(Dir).Should().Equal("logs --no-color --tail 300 api");
    }

    [Fact]
    public async Task GetLogs_HidesSecretsFromEnvAndFromPatterns()
    {
        var env = new SiteTestEnv();
        env.Runner.When("logs --no-color --tail 300 api", 0,
            "connecting to db with pgsecret123\n" +
            "Jwt__Key: jwtsecret456789\n" +
            "Authorization: Bearer abcdefghijkl");

        var result = await env.Manager.GetLogsAsync("api", CancellationToken.None);

        result.Text.Should().NotContain("pgsecret123").And.NotContain("jwtsecret456789").And.NotContain("abcdefghijkl");
        result.Text.Should().Contain("connecting to db with ***");
    }

    [Fact]
    public async Task GetLogs_AlsoHidesValuesOfSecretLookingLinesTheProgramNoLongerKnows()
    {
        var env = new SiteTestEnv(SiteTestEnv.BaseEnv + SiteTestEnv.LegacyLines);
        env.Runner.When("logs --no-color --tail 300 api", 0, "calling service with legacykey98765 now\nname is oldvalue");

        var result = await env.Manager.GetLogsAsync("api", CancellationToken.None);

        result.Text.Should().NotContain("legacykey98765");
        result.Text.Should().Contain("name is oldvalue");
    }

    [Fact]
    public async Task GetLogs_UnknownService_IsRejected_WithoutRunningAnything()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.GetLogsAsync("api; calc.exe", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.InvalidInput);
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogs_TunnelNotConfigured_ExplainsInsteadOfFailing()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.GetLogsAsync("tuna", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Text.Should().Contain("не настроен").And.Contain("свой домен");
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogs_TunnelWithTokenButNoDomain_ExplainsToo()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken(domain: ""));

        var result = await env.Manager.GetLogsAsync("tuna", CancellationToken.None);

        result.Text.Should().Contain("не настроен");
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogs_TunnelWithTokenAndDomain_UsesProfile()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken());
        env.Runner.When("logs", 0, "tuna-1  | tunnel is up");

        await env.Manager.GetLogsAsync("tuna", CancellationToken.None);

        env.Runner.ComposeCommands(Dir).Should().Equal("[tunnel] logs --no-color --tail 300 tuna");
    }

    [Fact]
    public async Task GetLogs_EmptyOutput_SaysLogIsEmpty()
    {
        var env = new SiteTestEnv();

        var result = await env.Manager.GetLogsAsync("db", CancellationToken.None);

        result.Text.Should().Be("Журнал пока пуст.");
    }

    [Fact]
    public async Task GetLogs_DockerOff_GivesFriendlyFailure()
    {
        var env = new SiteTestEnv();
        env.Runner.When("logs", 1, "", "error during connect: open //./pipe/dockerDesktopLinuxEngine");

        var result = await env.Manager.GetLogsAsync("api", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failure!.Kind.Should().Be(SiteFailureKind.DockerNotRunning);
    }

    [Fact]
    public async Task Refresh_AllRunning_ReportsRunningWithAddressesAndVersion()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken()).WithPs(SiteTestEnv.AllRunning(tunnel: true));

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Running);
        snapshot.Version.Should().Be("1.19.6");
        snapshot.LocalUrl.Should().Be("http://localhost:8080");
        snapshot.PublicUrl.Should().Be("https://crm.example.ru");
        snapshot.TunnelConfigured.Should().BeTrue();
        snapshot.Services.Should().HaveCount(5).And.OnlyContain(s => s.Level == ServiceLevel.Ok);
        env.Runner.ComposeCommands(Dir).Should().Equal("[tunnel] ps --all --format json");
    }

    [Fact]
    public async Task Refresh_WithoutDomain_HasNoPublicAddress_EvenIfOldAddressIsLeftInEnv()
    {
        var env = new SiteTestEnv(SiteTestEnv.BaseEnv.Replace("PUBLIC_URL=\r\n", "PUBLIC_URL=https://old.example.org\r\n"))
            .WithPs(SiteTestEnv.AllRunning());
        env.Files.Add(SiteTestEnv.PublicUrlPath, "https://old.example.org");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.PublicUrl.Should().BeNull();
        snapshot.TunnelConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_DomainWithoutToken_ShowsTheAddress_ButTheTunnelIsNotConfigured()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("", "crm.example.ru")).WithPs(SiteTestEnv.AllRunning());

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.PublicUrl.Should().Be("https://crm.example.ru");
        snapshot.TunnelConfigured.Should().BeFalse();
        snapshot.Services.Last().Level.Should().Be(ServiceLevel.NotConfigured);
        env.Runner.ComposeCommands(Dir).Should().Equal("ps --all --format json");
    }

    [Fact]
    public async Task Refresh_TokenWithoutDomain_HasNoTunnelAndNoAddress()
    {
        var env = new SiteTestEnv(SiteTestEnv.EnvWithToken("tunatoken1234", "")).WithPs(SiteTestEnv.AllRunning());

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.PublicUrl.Should().BeNull();
        snapshot.TunnelConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_NotInstalled()
    {
        var env = new SiteTestEnv();
        env.Install.Result = new SiteInstallReadResult(null, null);

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Installed.Should().BeFalse();
        snapshot.Overview.Pill.Should().Be(SitePill.NotInstalled);
        env.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_DockerEngineOff_ShowsDockerDown_AndSkipsHealthProbe()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" ps ", 1, "", "error during connect: open //./pipe/dockerDesktopLinuxEngine");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Docker.Should().Be(SiteDockerState.NotRunning);
        snapshot.Overview.Pill.Should().Be(SitePill.DockerDown);
        env.Probe.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_DockerMissing()
    {
        var env = new SiteTestEnv();
        env.Locator.Docker = null;

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Docker.Should().Be(SiteDockerState.Missing);
        snapshot.Overview.PillText.Should().Be("Docker не установлен");
    }

    [Fact]
    public async Task Refresh_DockerExecutableCannotStart_IsMissing()
    {
        var env = new SiteTestEnv();
        env.Runner.ThrowOnRun = new Win32Exception(2, "not found");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Docker.Should().Be(SiteDockerState.Missing);
    }

    [Fact]
    public async Task Refresh_ComposeFailsForOtherReason_ShowsError()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" ps ", 1, "", "no configuration file provided: not found");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Docker.Should().Be(SiteDockerState.Running);
        snapshot.Overview.Pill.Should().Be(SitePill.Error);
    }

    [Fact]
    public async Task Refresh_NoContainers_IsStopped()
    {
        var env = new SiteTestEnv().WithPs();

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Stopped);
        env.Probe.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_ContainersUpButSiteSilentForTooLong_BecomesError()
    {
        var env = new SiteTestEnv().WithPs(SiteTestEnv.AllRunning());
        env.Probe.Healthy = _ => false;
        SiteSnapshot? snapshot = null;

        for (var i = 0; i < SiteStatusEvaluator.HealthFailureLimit; i++)
        {
            snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);
            if (i < SiteStatusEvaluator.HealthFailureLimit - 1)
                snapshot.Overview.Pill.Should().Be(SitePill.Starting);
        }

        snapshot!.Overview.Pill.Should().Be(SitePill.Error);
    }

    [Fact]
    public async Task Refresh_HealthRecovery_ResetsTheFailureCounter()
    {
        var env = new SiteTestEnv().WithPs(SiteTestEnv.AllRunning());
        var healthy = false;
        env.Probe.Healthy = _ => healthy;

        for (var i = 0; i < SiteStatusEvaluator.HealthFailureLimit - 1; i++)
            await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);
        healthy = true;
        (await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None)).Overview.Pill.Should().Be(SitePill.Running);
        healthy = false;

        (await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None)).Overview.Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public async Task Refresh_ReadsPsAsArrayToo()
    {
        var env = new SiteTestEnv();
        env.Runner.When(" ps ", 0, "[" + string.Join(",", new[] { "db", "api", "web", "proxy" }.Select(s =>
            $"{{\"Service\":\"{s}\",\"State\":\"running\",\"Health\":\"healthy\",\"Status\":\"Up\"}}")) + "]");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Running);
    }

    [Fact]
    public async Task Refresh_DuringStartOperation_ShowsStartingWhileContainersAreMissing()
    {
        var env = new SiteTestEnv().WithPs();

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.Starting, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public async Task Refresh_BrokenInstallFile_IsError()
    {
        var env = new SiteTestEnv();
        env.Install.Result = new SiteInstallReadResult(null, "Файл установки сайта имеет неверный формат.");

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Error);
    }

    [Fact]
    public async Task Refresh_EnvMissing_IsErrorWithReinstallHint()
    {
        var env = new SiteTestEnv();
        env.Files.Files.Clear();

        var snapshot = await env.Manager.RefreshAsync(SiteOperation.None, CancellationToken.None);

        snapshot.Overview.Pill.Should().Be(SitePill.Error);
        snapshot.Overview.Explanation.Should().Contain("установщик");
    }
}

public class SiteInstallStoreTests
{
    private const string Path = @"C:\Users\x\AppData\Local\HQStudio\install.json";

    [Fact]
    public void Read_MissingFile_MeansNotInstalled()
    {
        var result = SiteInstallStore.Parse(new SiteFakeFiles(), Path);

        result.NotInstalled.Should().BeTrue();
    }

    [Fact]
    public void Read_ValidFile_ReturnsInfo()
    {
        var files = new SiteFakeFiles().Add(Path,
            "{\"serverDir\":\"C:\\\\hq\\\\server\",\"webUrl\":\"http://localhost:8080\",\"apiUrl\":\"http://localhost:8080/api\",\"repo\":\"ibuildrun/hqstudio\"}");

        var result = SiteInstallStore.Parse(files, Path);

        result.Info.Should().Be(new SiteInstallInfo(@"C:\hq\server", "http://localhost:8080", "http://localhost:8080/api", "ibuildrun/hqstudio"));
    }

    [Fact]
    public void Read_KeysAreCaseInsensitive_AndApiUrlFallsBackToWebUrl()
    {
        var files = new SiteFakeFiles().Add(Path, "{\"ServerDir\":\"C:\\\\hq\",\"WebUrl\":\"http://localhost:8080\"}");

        var info = SiteInstallStore.Parse(files, Path).Info!;

        info.ServerDir.Should().Be(@"C:\hq");
        info.ApiUrl.Should().Be("http://localhost:8080");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"webUrl\":\"http://localhost\"}")]
    public void Read_BrokenFile_ReportsProblem(string content)
    {
        var result = SiteInstallStore.Parse(new SiteFakeFiles().Add(Path, content), Path);

        result.Info.Should().BeNull();
        result.Problem.Should().NotBeNullOrEmpty();
        result.NotInstalled.Should().BeFalse();
    }
}
