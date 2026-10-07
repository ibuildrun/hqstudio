using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Sim;
using Xunit;

namespace HQStudio.Setup.Tests;

/// <summary>
/// The app's uninstall keeps a copy of the server .env (site-env.backup) when the user keeps the site data,
/// because the database password lives only there.
/// </summary>
public class EnvBackupTests
{
    private const string OldPostgres = "OldPostgresPassword12345";
    private static readonly string OldJwt = new('j', 48);

    private static string BackupText(string extra = "") =>
        "# saved by uninstall\n" +
        $"POSTGRES_PASSWORD={OldPostgres}\nJWT_KEY={OldJwt}\nHQ_PORT=8083\nADMIN_PASSWORD=\n" + extra;

    private static void WriteBackup(Rig rig, string text)
    {
        Directory.CreateDirectory(rig.Paths.DataDir);
        File.WriteAllText(rig.Paths.EnvBackup, text);
    }

    private static async Task<(InstallEngine Engine, InstallContext Context)> Run(Rig rig, InstallAnswers answers)
    {
        var context = rig.Context(answers);
        var engine = new InstallEngine(context);
        await engine.RunAsync(CancellationToken.None);
        return (engine, context);
    }

    [Fact]
    public void EnvBackupPath_IsInTheDataFolderNextToInstallJson()
    {
        var paths = InstallPaths.ForCurrentUser();

        paths.EnvBackup.Should().Be(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio", "site-env.backup"));
        Path.GetDirectoryName(paths.EnvBackup).Should().Be(Path.GetDirectoryName(paths.InstallJson));
    }

    [Fact]
    public void Reader_NoEnvAndNoBackup_ReturnsNull()
    {
        using var rig = new Rig();

        ExistingEnvReader.Read(rig.Paths).Should().BeNull();
    }

    [Fact]
    public void Reader_UsesTheBackupOnlyWhenThereIsNoRealEnv()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText());

        var fromBackup = ExistingEnvReader.Read(rig.Paths)!;
        fromBackup.FromBackup.Should().BeTrue();
        EnvFile.Get(fromBackup.Text, "POSTGRES_PASSWORD").Should().Be(OldPostgres);

        Directory.CreateDirectory(rig.Paths.ServerDir);
        File.WriteAllText(rig.Paths.EnvFile, "POSTGRES_PASSWORD=RealOne1234567890\n");

        var real = ExistingEnvReader.Read(rig.Paths)!;
        real.FromBackup.Should().BeFalse();
        EnvFile.Get(real.Text, "POSTGRES_PASSWORD").Should().Be("RealOne1234567890");
    }

    [Fact]
    public async Task Install_ReusesDatabaseSecretsFromTheBackup()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText());

        var (engine, context) = await Run(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        var env = File.ReadAllText(rig.Paths.EnvFile);
        EnvFile.Get(env, "POSTGRES_PASSWORD").Should().Be(OldPostgres);
        EnvFile.Get(env, "JWT_KEY").Should().Be(OldJwt);
        context.UsesEnvBackup.Should().BeTrue();
        context.KeptExistingDatabase.Should().BeTrue("the old database is kept, so the admin password does not change");
        EnvFile.Get(env, "ADMIN_NAME").Should().Be("Иван Петров", "other values still come from the wizard");
    }

    [Fact]
    public async Task Install_BlankKeysKeepGeminiAndTunaFromTheBackup_AndTheTunnelStageAppears()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText("GEMINI_API_KEY=old-gemini-key\nTUNA_TOKEN=old-tuna-token\nTUNA_SUBDOMAIN=oldsub\n"));
        var answers = Rig.Answers(tuna: false);
        answers.GeminiKey = "";

        var (engine, context) = await Run(rig, answers);

        engine.LastFailure.Should().BeNull();
        engine.Stages.Select(s => s.Id).Should().Contain(StageId.PublicUrl);
        var env = File.ReadAllText(rig.Paths.EnvFile);
        EnvFile.Get(env, "GEMINI_API_KEY").Should().Be("old-gemini-key");
        EnvFile.Get(env, "TUNA_TOKEN").Should().Be("old-tuna-token");
        EnvFile.Get(env, "TUNA_SUBDOMAIN").Should().Be("oldsub");
        context.TunnelEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Install_TypedKeysOverrideTheBackup()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText("GEMINI_API_KEY=old-gemini-key\n"));

        await Run(rig, Rig.Answers());

        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "GEMINI_API_KEY").Should().Be("AIzaSyTestKey123456");
    }

    [Fact]
    public async Task Backup_IsDeletedAfterASuccessfulInstall()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText());

        var (engine, _) = await Run(rig, Rig.Answers());

        engine.LastFailure.Should().BeNull();
        File.Exists(rig.Paths.EnvBackup).Should().BeFalse();
        File.Exists(rig.Paths.EnvFile).Should().BeTrue("the real .env now carries the secrets");
    }

    [Fact]
    public async Task Backup_SurvivesAFailedInstallAndIsDeletedAfterTheRetrySucceeds()
    {
        using var rig = new Rig { Injector = new FailOnceInjector(StageId.Health) };
        WriteBackup(rig, BackupText());
        var context = rig.Context(Rig.Answers());
        var engine = new InstallEngine(context);

        (await engine.RunAsync(CancellationToken.None)).Should().Be(InstallOutcome.Failed);
        File.Exists(rig.Paths.EnvBackup).Should().BeTrue();

        (await engine.RunAsync(CancellationToken.None)).Should().Be(InstallOutcome.Success);
        File.Exists(rig.Paths.EnvBackup).Should().BeFalse();
        EnvFile.Get(File.ReadAllText(rig.Paths.EnvFile), "POSTGRES_PASSWORD").Should().Be(OldPostgres);
    }

    [Fact]
    public async Task Backup_IsNotUsedWhenARealEnvExists_AndIsLeftUntouched()
    {
        using var rig = new Rig();
        var realPostgres = "RealPostgresPassword99999";
        var realJwt = new string('r', 48);
        Directory.CreateDirectory(rig.Paths.ServerDir);
        File.WriteAllText(rig.Paths.EnvFile, $"POSTGRES_PASSWORD={realPostgres}\nJWT_KEY={realJwt}\n");
        var backupText = BackupText("GEMINI_API_KEY=backup-gemini\n");
        WriteBackup(rig, backupText);

        var answers = Rig.Answers();
        answers.GeminiKey = "";

        var (engine, context) = await Run(rig, answers);

        engine.LastFailure.Should().BeNull();
        context.UsesEnvBackup.Should().BeFalse();
        var env = File.ReadAllText(rig.Paths.EnvFile);
        EnvFile.Get(env, "POSTGRES_PASSWORD").Should().Be(realPostgres);
        EnvFile.Get(env, "JWT_KEY").Should().Be(realJwt);
        EnvFile.Get(env, "GEMINI_API_KEY").Should().BeEmpty("nothing from the backup leaks into the real settings");
        File.ReadAllText(rig.Paths.EnvBackup).Should().Be(backupText);
    }

    [Fact]
    public async Task Backup_IsKeptWhenTheSiteIsSkipped()
    {
        using var rig = new Rig();
        var backupText = BackupText();
        WriteBackup(rig, backupText);

        var (engine, _) = await Run(rig, Rig.Answers(skipSite: true));

        engine.LastFailure.Should().BeNull();
        File.ReadAllText(rig.Paths.EnvBackup).Should().Be(backupText, "the site is not installed yet, so its secrets are still needed");
        File.Exists(rig.Paths.EnvFile).Should().BeFalse();
    }

    [Fact]
    public async Task Log_NeverContainsSecretsFromTheBackup()
    {
        using var rig = new Rig();
        WriteBackup(rig, BackupText("TUNA_TOKEN=old-tuna-token-xyz\n"));
        rig.Docker.Handler = call => call.Verb == "up"
            ? new CommandResult(0, $"db password {OldPostgres} jwt {OldJwt} tuna old-tuna-token-xyz")
            : new CommandResult(0, "");
        var log = new SetupLog(null);
        var engine = new InstallEngine(rig.Context(Rig.Answers(), log));

        await engine.RunAsync(CancellationToken.None);

        var text = string.Join("\n", log.Snapshot());
        text.Should().NotContain(OldPostgres).And.NotContain(OldJwt).And.NotContain("old-tuna-token-xyz");
    }

    [Fact]
    public async Task Simulation_WorksOnItsOwnTempTreeAndNeverTouchesTheRealBackup()
    {
        var real = InstallPaths.ForCurrentUser().EnvBackup;
        var before = Snapshot(real);

        using var simRoot = new TempDir();
        using var rig = new Rig();
        var sim = ServiceFactory.CreateSimulated(SetupOptions.Parse(new[] { "--simulate" }), _ => { }, simRoot.Path);

        foreach (var path in new[]
                 {
                     sim.Paths.AppDir, sim.Paths.ServerDir, sim.Paths.DataDir, sim.Paths.SettingsDir, sim.Paths.StartMenuDir,
                     sim.Paths.DesktopDir, sim.Paths.TempDir, sim.Paths.EnvBackup, sim.Paths.EnvFile, sim.Paths.SetupLog
                 })
        {
            path.Should().StartWith(simRoot.Path);
        }
        sim.Paths.EnvBackup.Should().NotBe(real);

        Directory.CreateDirectory(sim.Paths.DataDir);
        File.WriteAllText(sim.Paths.EnvBackup, BackupText());

        // simulated paths, payload, shortcuts and registry, but instant docker and delays so the test stays fast
        var services = new SetupServices
        {
            Paths = sim.Paths, Docker = rig.Docker, DockerInstaller = sim.DockerInstaller, Health = rig.Health, Ports = sim.Ports,
            Shortcuts = sim.Shortcuts, Registry = sim.Registry, Shell = sim.Shell, Payload = sim.Payload, Delay = rig.Delay,
            Version = "1.20.0", IsSimulation = true
        };
        var context = new InstallContext(Rig.Answers(), services, new SetupLog(null));
        var outcome = await new InstallEngine(context).RunAsync(CancellationToken.None);

        outcome.Should().Be(InstallOutcome.Success);
        EnvFile.Get(File.ReadAllText(sim.Paths.EnvFile), "POSTGRES_PASSWORD").Should().Be(OldPostgres);
        File.Exists(sim.Paths.EnvBackup).Should().BeFalse("the simulated backup is consumed in the temp tree");
        Snapshot(real).Should().Be(before, "the real backup file must not be read-modified, created or deleted by a simulation");
    }

    private static string Snapshot(string path) =>
        File.Exists(path) ? $"exists|{new FileInfo(path).Length}|{File.GetLastWriteTimeUtc(path).Ticks}" : "missing";
}
