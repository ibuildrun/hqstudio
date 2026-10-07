using System.IO;
using System.ComponentModel;
using FluentAssertions;
using HQStudio.Services.Site;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UninstallServiceTests
{
    private const string AppDir = @"C:\Tools\HQ Studio";
    private const string TempRoot = @"C:\Users\x\AppData\Local\Temp\";
    private const string LocalRoot = @"C:\Users\x\AppData\Local\HQStudio";
    private const string StartMenu = @"C:\Users\x\AppData\Roaming\Microsoft\Windows\Start Menu\Programs";
    private const string Desktop = @"C:\Users\x\Desktop";
    private static readonly string ServerDir = Path.Combine(LocalRoot, "server");

    private sealed class Rig
    {
        public SiteFakeFiles Files { get; } = new();
        public SiteFakeRunner Runner { get; } = new();
        public SiteFakeLocator Locator { get; } = new();
        public SiteFakeRegistry Registry { get; } = new();
        public SiteFakeInstallStore Install { get; } = new();
        public List<string> Timeline { get; } = new();
        public UninstallService Service { get; }
        public UninstallLocations Locations { get; }

        public Rig(string currentExeDir = AppDir, bool withEnv = true)
        {
            Install.Result = new SiteInstallReadResult(
                new SiteInstallInfo(ServerDir, "http://localhost:8080", "http://localhost:8080", "r"), null);
            Files.Add(Path.Combine(AppDir, "HQStudio.exe"));
            Files.Add(Path.Combine(LocalRoot, "install.json"), "{}");
            Files.Add(Path.Combine(LocalRoot, "settings.json"), "{}");
            Files.Add(Path.Combine(LocalRoot, "crash.log"), "log");
            Files.Add(Path.Combine(StartMenu, "HQ Studio.lnk"));
            Files.Add(Path.Combine(Desktop, "HQ Studio.lnk"));
            if (withEnv)
                Files.Add(Path.Combine(ServerDir, ".env"), SiteTestEnv.BaseEnv);
            Files.Directories.Add(ServerDir);

            Files.Timeline = Timeline;
            var composePrefix = $"compose --project-directory {ServerDir} -f {ServerDir}\\docker-compose.yml ";
            Runner.OnCall = call => Timeline.Add(call.StartsWith("detached:") ? call : "run:" + call.Replace(composePrefix, ""));
            Registry.OnDelete = key => Timeline.Add("registry:" + key);

            Locations = new UninstallLocations(AppDir, currentExeDir, TempRoot, StartMenu, Desktop, new SitePaths(LocalRoot),
                new[] { @"C:\Windows", @"C:\Program Files", @"C:\Users\x", Desktop });
            Service = new UninstallService(Files, Runner, Locator, Install, Registry, Locations);
        }

        public Task<UninstallResult> Run(bool deleteData, List<UninstallStageUpdate>? updates = null, string appDir = AppDir) =>
            Service.RunAsync(new UninstallOptions(deleteData, appDir), updates == null ? null : updates.Add);
    }

    // ---------------------------------------------------------------- порядок этапов

    [Fact]
    public async Task Run_ReportsStagesInOrder_EachRunningThenFinished()
    {
        var rig = new Rig();
        var updates = new List<UninstallStageUpdate>();

        var result = await rig.Run(false, updates);

        result.HasFailures.Should().BeFalse();
        updates.Select(u => (u.Stage, u.State)).Should().Equal(
            (UninstallStage.StopSite, UninstallStageState.Running),
            (UninstallStage.StopSite, UninstallStageState.Done),
            (UninstallStage.Shortcuts, UninstallStageState.Running),
            (UninstallStage.Shortcuts, UninstallStageState.Done),
            (UninstallStage.Registry, UninstallStageState.Running),
            (UninstallStage.Registry, UninstallStageState.Done),
            (UninstallStage.SiteFiles, UninstallStageState.Running),
            (UninstallStage.SiteFiles, UninstallStageState.Done),
            (UninstallStage.Program, UninstallStageState.Running),
            (UninstallStage.Program, UninstallStageState.Done));
    }

    [Fact]
    public async Task Run_ActionsHappenInTheDocumentedOrder_ProgramRemovalLast()
    {
        var rig = new Rig();

        await rig.Run(false);

        rig.Timeline.Should().ContainInOrder(
            "run:down --remove-orphans",
            "delete-file:HQ Studio.lnk",
            "registry:HQStudio",
            "copy:.env",
            "delete-dir:server",
            "delete-file:install.json",
            "detached:cmd.exe");
        rig.Timeline.Last().Should().Be("detached:cmd.exe");
    }

    // ---------------------------------------------------------------- данные: оставить и удалить

    [Fact]
    public async Task KeepData_StopsSiteWithoutVolumes()
    {
        var rig = new Rig();

        await rig.Run(false);

        rig.Runner.ComposeCommands(ServerDir).Should().Equal("down --remove-orphans");
    }

    [Fact]
    public async Task DeleteData_AddsVolumesFlag()
    {
        var rig = new Rig();

        await rig.Run(true);

        rig.Runner.ComposeCommands(ServerDir).Should().Equal("down --remove-orphans --volumes");
    }

    [Fact]
    public async Task Down_UsesTunnelProfileWhenTokenIsSet()
    {
        var rig = new Rig();
        rig.Files.Add(Path.Combine(ServerDir, ".env"), SiteTestEnv.EnvWithToken());

        await rig.Run(false);

        rig.Runner.ComposeCommands(ServerDir).Should().Equal("[tunnel] down --remove-orphans");
    }

    [Fact]
    public async Task KeepData_SavesEnvBackupBeforeDeletingServerFolder()
    {
        var rig = new Rig();

        var result = await rig.Run(false);

        var backup = Path.Combine(LocalRoot, "site-env.backup");
        rig.Files.Files.Should().ContainKey(backup);
        rig.Files.Files[backup].Should().Be(SiteTestEnv.BaseEnv);
        var copyIndex = rig.Files.Log.FindIndex(l => l.StartsWith("copy:"));
        var deleteIndex = rig.Files.Log.FindIndex(l => l == $"delete-dir:{ServerDir}");
        copyIndex.Should().BeGreaterOrEqualTo(0).And.BeLessThan(deleteIndex);
        result.Stages.Single(s => s.Stage == UninstallStage.SiteFiles).Note.Should().Contain("сохранены");
    }

    [Fact]
    public async Task KeepData_WhenBackupFails_ServerFolderIsNotTouched()
    {
        var rig = new Rig();
        rig.Files.FailCopy = _ => true;

        var result = await rig.Run(false);

        result.HasFailures.Should().BeTrue();
        rig.Files.Log.Should().NotContain($"delete-dir:{ServerDir}");
        rig.Files.Files.Should().ContainKey(Path.Combine(LocalRoot, "install.json"));
    }

    [Fact]
    public async Task DeleteData_RemovesAnOldEnvBackup_AndMakesNoNewOne()
    {
        var rig = new Rig();
        var backup = Path.Combine(LocalRoot, "site-env.backup");
        rig.Files.Add(backup, "old");

        await rig.Run(true);

        rig.Files.Files.Should().NotContainKey(backup);
        rig.Files.Log.Should().NotContain(l => l.StartsWith("copy:"));
    }

    [Fact]
    public async Task SiteFiles_ServerFolderAndInstallJsonAreRemoved_SettingsAndCrashLogAreKept()
    {
        var rig = new Rig();

        await rig.Run(false);

        rig.Files.Log.Should().Contain($"delete-dir:{ServerDir}");
        rig.Files.Files.Should().NotContainKey(Path.Combine(LocalRoot, "install.json"));
        rig.Files.Files.Should().ContainKey(Path.Combine(LocalRoot, "settings.json"));
        rig.Files.Files.Should().ContainKey(Path.Combine(LocalRoot, "crash.log"));
        rig.Files.Log.Should().NotContain(l => l.Contains("settings.json") || l.Contains("crash.log"));
    }

    [Fact]
    public async Task SiteFiles_CustomServerFolderInsideLocalRoot_IsRemovedToo()
    {
        var rig = new Rig();
        var custom = Path.Combine(LocalRoot, "site2");
        rig.Install.Result = new SiteInstallReadResult(new SiteInstallInfo(custom, "u", "u", "r"), null);
        rig.Files.Add(Path.Combine(custom, ".env"), SiteTestEnv.BaseEnv);

        await rig.Run(true);

        rig.Files.Log.Should().Contain($"delete-dir:{custom}").And.Contain($"delete-dir:{ServerDir}");
    }

    [Fact]
    public async Task SiteFiles_ServerFolderOutsideLocalRoot_IsNeverDeleted()
    {
        var rig = new Rig();
        rig.Install.Result = new SiteInstallReadResult(new SiteInstallInfo(@"D:\Important\Documents", "u", "u", "r"), null);

        await rig.Run(true);

        rig.Files.Log.Should().NotContain(l => l.Contains(@"D:\Important"));
        rig.Files.Log.Should().Contain($"delete-dir:{ServerDir}");
    }

    // ---------------------------------------------------------------- Docker выключен

    [Fact]
    public async Task DockerEngineOff_StageIsSkippedWithNote_AndTheRestStillRuns()
    {
        var rig = new Rig();
        rig.Runner.When("down", 1, "", "error during connect: open //./pipe/dockerDesktopLinuxEngine");

        var result = await rig.Run(true);

        var stop = result.Stages.Single(s => s.Stage == UninstallStage.StopSite);
        stop.State.Should().Be(UninstallStageState.Skipped);
        stop.Note.Should().Contain("Docker выключен").And.Contain("данные");
        result.HasFailures.Should().BeFalse();
        result.HasWarnings.Should().BeTrue();
        result.Stages.Where(s => s.Stage != UninstallStage.StopSite).Should().OnlyContain(s => s.State == UninstallStageState.Done);
        rig.Registry.Deleted.Should().Equal("HQStudio");
        rig.Runner.Detached.Should().ContainSingle();
    }

    [Fact]
    public async Task DockerNotInstalled_StageSkipped()
    {
        var rig = new Rig();
        rig.Locator.Docker = null;

        var result = await rig.Run(false);

        result.Stages.Single(s => s.Stage == UninstallStage.StopSite).State.Should().Be(UninstallStageState.Skipped);
        rig.Runner.Calls.Should().BeEmpty();
        result.Stages.Last().State.Should().Be(UninstallStageState.Done);
    }

    [Fact]
    public async Task DockerExecutableCannotStart_StageSkipped()
    {
        var rig = new Rig();
        rig.Runner.ThrowOnRun = new Win32Exception(2, "not found");

        var result = await rig.Run(false);

        result.Stages.Single(s => s.Stage == UninstallStage.StopSite).State.Should().Be(UninstallStageState.Skipped);
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public async Task ComposeFailsForAnotherReason_IsAWarning_NotAnAbort()
    {
        var rig = new Rig();
        rig.Runner.When("down", 1, "", "no configuration file provided");

        var result = await rig.Run(false);

        result.HasWarnings.Should().BeTrue();
        result.Stages.Single(s => s.Stage == UninstallStage.StopSite).Note.Should().Contain("Docker");
        rig.Runner.Detached.Should().ContainSingle();
    }

    [Fact]
    public async Task NotInstalledSite_SkipsStopQuietly_ButStillRemovesProgram()
    {
        var rig = new Rig();
        rig.Install.Result = new SiteInstallReadResult(null, null);

        var result = await rig.Run(false);

        var stop = result.Stages.Single(s => s.Stage == UninstallStage.StopSite);
        stop.State.Should().Be(UninstallStageState.Skipped);
        stop.Note.Should().BeEmpty();
        result.HasWarnings.Should().BeFalse();
        rig.Runner.Calls.Should().BeEmpty();
        rig.Runner.Detached.Should().ContainSingle();
    }

    // ---------------------------------------------------------------- ярлыки и реестр

    [Fact]
    public async Task Shortcuts_BothExistingShortcutsAreDeleted()
    {
        var rig = new Rig();

        var result = await rig.Run(false);

        rig.Files.Log.Should().Contain(Path.Combine(StartMenu, "HQ Studio.lnk").Insert(0, "delete-file:"))
            .And.Contain(Path.Combine(Desktop, "HQ Studio.lnk").Insert(0, "delete-file:"));
        result.Stages.Single(s => s.Stage == UninstallStage.Shortcuts).Note.Should().Contain("2");
    }

    [Fact]
    public async Task Shortcuts_MissingOnesAreFine()
    {
        var rig = new Rig();
        rig.Files.Files.Remove(Path.Combine(Desktop, "HQ Studio.lnk"));

        var result = await rig.Run(false);

        rig.Files.Log.Where(l => l.EndsWith("HQ Studio.lnk")).Should().ContainSingle();
        result.Stages.Single(s => s.Stage == UninstallStage.Shortcuts).State.Should().Be(UninstallStageState.Done);
    }

    [Fact]
    public async Task Shortcuts_NothingToDelete_SaysSo()
    {
        var rig = new Rig();
        rig.Files.Files.Remove(Path.Combine(Desktop, "HQ Studio.lnk"));
        rig.Files.Files.Remove(Path.Combine(StartMenu, "HQ Studio.lnk"));

        var result = await rig.Run(false);

        result.Stages.Single(s => s.Stage == UninstallStage.Shortcuts).Note.Should().Be("Ярлыков не нашлось.");
    }

    [Fact]
    public async Task Registry_UninstallKeyIsRemoved()
    {
        var rig = new Rig();

        await rig.Run(false);

        rig.Registry.Deleted.Should().Equal("HQStudio");
    }

    [Fact]
    public async Task Registry_Failure_IsRecordedButDoesNotStopTheRest()
    {
        var rig = new Rig();
        rig.Registry.OnDelete = _ => throw new UnauthorizedAccessException("denied");

        var result = await rig.Run(false);

        result.HasFailures.Should().BeTrue();
        result.Stages.Single(s => s.Stage == UninstallStage.Registry).State.Should().Be(UninstallStageState.Failed);
        result.Stages.Single(s => s.Stage == UninstallStage.Program).State.Should().Be(UninstallStageState.Done);
    }

    // ---------------------------------------------------------------- удаление папки программы

    [Fact]
    public async Task Program_SchedulesHiddenCmdThatWaitsAndRemovesTheFolder()
    {
        var rig = new Rig();

        await rig.Run(false);

        var (file, args, workingDirectory) = rig.Runner.Detached.Should().ContainSingle().Subject;
        file.Should().Be("cmd.exe");
        args.Should().StartWith("/c ").And.Contain("ping -n").And.Contain($"rmdir /s /q \"{AppDir}\"");
        workingDirectory.Should().Be(TempRoot);
    }

    [Fact]
    public async Task Program_RunningFromTheTempCopy_AlsoRemovesTheCopyFolder()
    {
        var copyDir = Path.Combine(TempRoot, "HQStudio-Uninstall");
        var rig = new Rig(currentExeDir: copyDir);

        await rig.Run(false);

        var args = rig.Runner.Detached.Single().Args;
        args.Should().Contain($"rmdir /s /q \"{AppDir}\"").And.Contain($"rmdir /s /q \"{copyDir}\"");
    }

    [Fact]
    public async Task Program_NormalRun_DoesNotTouchTempFolder()
    {
        var rig = new Rig();

        await rig.Run(false);

        rig.Runner.Detached.Single().Args.Should().NotContain("HQStudio-Uninstall");
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"C:\Program Files")]
    [InlineData(@"C:\Users\x")]
    [InlineData(@"C:\Users\x\Desktop")]
    [InlineData(@"C:\Nowhere\HQ Studio")]
    [InlineData("")]
    [InlineData("relative\\path")]
    [InlineData(@"C:\Tools\HQ ""Studio")]
    [InlineData(@"C:\Tools\100%\HQ")]
    public async Task Program_UnsafeFolders_AreRefused_AndNothingIsScheduled(string appDir)
    {
        var rig = new Rig();

        var result = await rig.Run(false, appDir: appDir);

        result.Stages.Single(s => s.Stage == UninstallStage.Program).State.Should().Be(UninstallStageState.Failed);
        rig.Runner.Detached.Should().BeEmpty();
    }

    [Fact]
    public void IsSafeAppDir_RequiresMainExeInside()
    {
        var rig = new Rig();

        rig.Service.IsSafeAppDir(AppDir).Should().BeTrue();
        rig.Service.IsSafeAppDir(AppDir + "\\").Should().BeTrue();
        rig.Service.IsSafeAppDir(@"C:\Tools\Other").Should().BeFalse();
    }

    // ---------------------------------------------------------------- итог

    [Fact]
    public async Task Result_CleanRun_HasNoFailuresOrWarnings()
    {
        var rig = new Rig();

        var result = await rig.Run(false);

        result.CleanSuccess.Should().BeTrue();
        result.Stages.Should().HaveCount(5);
    }

    [Fact]
    public async Task EndToEnd_NothingOutsideTheFakesIsTouched()
    {
        var rig = new Rig();

        await rig.Run(true);

        // Все этапы работали только с подставными файлами, процессами и реестром.
        rig.Files.Log.Should().OnlyContain(l => l.StartsWith("delete-") || l.StartsWith("copy:") || l.StartsWith("write:"));
        rig.Runner.Calls.Should().OnlyContain(c => c.Contains("compose"));
    }
}

public class UninstallCommandsTests
{
    [Fact]
    public void BuildDeleteFoldersArguments_SingleFolder()
    {
        var args = UninstallCommands.BuildDeleteFoldersArguments(new[] { @"C:\Tools\HQ Studio" });

        args.Should().Be("/c for /l %i in (1,1,40) do (ping -n 3 127.0.0.1 >nul & rmdir /s /q \"C:\\Tools\\HQ Studio\" 2>nul & if not exist \"C:\\Tools\\HQ Studio\" exit)");
    }

    [Fact]
    public void BuildDeleteFoldersArguments_TwoFolders_WaitsForBoth()
    {
        var args = UninstallCommands.BuildDeleteFoldersArguments(new[] { @"C:\A", @"C:\B" });

        args.Should().Be("/c for /l %i in (1,1,40) do (ping -n 3 127.0.0.1 >nul & rmdir /s /q \"C:\\A\" 2>nul & rmdir /s /q \"C:\\B\" 2>nul & if not exist \"C:\\A\" if not exist \"C:\\B\" exit)");
    }

    [Fact]
    public void BuildUninstallLaunchArguments_QuotesTheAppFolder()
    {
        UninstallCommands.BuildUninstallLaunchArguments(@"C:\Program Files\HQ Studio")
            .Should().Be("--uninstall --app-dir \"C:\\Program Files\\HQ Studio\"");
    }
}

public class UninstallArgumentsTests
{
    [Fact]
    public void TryParse_NoFlag_IsNotUninstall()
    {
        UninstallArguments.TryParse(new[] { "--other" }, out var result).Should().BeFalse();
        result.Should().BeNull();
        UninstallArguments.TryParse(Array.Empty<string>(), out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_PlainFlag_HasNoAppDir()
    {
        UninstallArguments.TryParse(new[] { "--uninstall" }, out var result).Should().BeTrue();
        result!.AppDir.Should().BeNull();
    }

    [Fact]
    public void TryParse_AppDirAsSeparateArgument()
    {
        UninstallArguments.TryParse(new[] { "--uninstall", "--app-dir", @"C:\Program Files\HQ Studio" }, out var result).Should().BeTrue();
        result!.AppDir.Should().Be(@"C:\Program Files\HQ Studio");
    }

    [Fact]
    public void TryParse_AppDirWithEqualsAndQuotes_AndCaseInsensitiveFlags()
    {
        UninstallArguments.TryParse(new[] { "--UNINSTALL", "--app-dir=\"C:\\HQ\"" }, out var result).Should().BeTrue();
        result!.AppDir.Should().Be(@"C:\HQ");
    }

    [Fact]
    public void TryParse_AppDirAloneDoesNotTriggerUninstall()
    {
        UninstallArguments.TryParse(new[] { "--app-dir", @"C:\HQ" }, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_AppDirWithoutValue_IsIgnored()
    {
        UninstallArguments.TryParse(new[] { "--uninstall", "--app-dir" }, out var result).Should().BeTrue();
        result!.AppDir.Should().BeNull();
    }
}

public class UninstallLauncherTests
{
    private const string Temp = @"C:\Users\x\AppData\Local\Temp\";
    private const string Exe = @"C:\Program Files\HQ Studio\HQStudio.exe";

    [Fact]
    public void Launch_CopiesExeToTempFolder_AndStartsItWithUninstallArguments()
    {
        var files = new SiteFakeFiles().Add(Exe, "exe-bytes");
        var runner = new SiteFakeRunner();

        var result = new UninstallLauncher(files, runner, Temp).Launch(Exe);

        result.Success.Should().BeTrue();
        var target = Path.Combine(Temp, "HQStudio-Uninstall", "HQStudio.exe");
        files.Files.Should().ContainKey(target);
        var started = runner.Detached.Should().ContainSingle().Subject;
        started.File.Should().Be(target);
        started.Args.Should().Be("--uninstall --app-dir \"C:\\Program Files\\HQ Studio\"");
        started.WorkingDirectory.Should().Be(Path.Combine(Temp, "HQStudio-Uninstall"));
    }

    [Fact]
    public void Launch_RemovesLeftoversOfAPreviousAttemptFirst()
    {
        var files = new SiteFakeFiles().Add(Exe);
        var runner = new SiteFakeRunner();

        new UninstallLauncher(files, runner, Temp).Launch(Exe);

        files.Log.Should().StartWith($"delete-dir:{Path.Combine(Temp, "HQStudio-Uninstall")}");
    }

    [Fact]
    public void Launch_MultiFileBuild_CopiesTheWholeAppFolder()
    {
        var dll = @"C:\Program Files\HQ Studio\HQStudio.dll";
        var files = new SiteFakeFiles().Add(Exe).Add(dll);
        files.Listings[@"C:\Program Files\HQ Studio"] = new[] { Exe, dll, @"C:\Program Files\HQ Studio\HQStudio.runtimeconfig.json" };

        new UninstallLauncher(files, new SiteFakeRunner(), Temp).Launch(Exe);

        files.Log.Where(l => l.StartsWith("copy:")).Should().HaveCount(3);
        files.Files.Keys.Should().Contain(Path.Combine(Temp, "HQStudio-Uninstall", "HQStudio.dll"));
    }

    [Fact]
    public void Launch_MissingExe_FailsWithoutStartingAnything()
    {
        var files = new SiteFakeFiles();
        var runner = new SiteFakeRunner();

        var result = new UninstallLauncher(files, runner, Temp).Launch(Exe);

        result.Success.Should().BeFalse();
        runner.Detached.Should().BeEmpty();
    }

    [Fact]
    public void Launch_CopyFails_ReportsAndDoesNotStart()
    {
        var files = new SiteFakeFiles().Add(Exe);
        files.FailCopy = _ => true;
        var runner = new SiteFakeRunner();

        var result = new UninstallLauncher(files, runner, Temp).Launch(Exe);

        result.Success.Should().BeFalse();
        result.Failure!.Title.Should().Be("Не удалось начать удаление");
        runner.Detached.Should().BeEmpty();
    }
}
