using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateCoordinatorTests
{
    [Fact]
    public async Task Check_DetectsOutdatedAppAndSite()
    {
        using var f = new UpdateRig();
        f.Coordinator.Status.Latest.Should().BeNull();

        var result = await f.Coordinator.CheckAsync();

        result.Success.Should().BeTrue();
        var s = f.Coordinator.Status;
        s.LatestVersion.Should().Be("1.20.0");
        s.InstalledAppVersion.Should().Be("1.19.6");
        s.InstalledServerVersion.Should().Be("1.19.6");
        s.AppUpdateAvailable.Should().BeTrue();
        s.ServerUpdateAvailable.Should().BeTrue();
        result.Message.Should().Contain("Доступно обновление").And.Contain("приложение").And.Contain("сайт");
    }

    [Fact]
    public async Task Check_UpToDate_ReportsNothingToUpdate()
    {
        using var f = new UpdateRig(installedAppVersion: "1.20.0", envContent: "HQSTUDIO_VERSION=1.20.0\n");

        var result = await f.Coordinator.CheckAsync();

        f.Coordinator.Status.AnyUpdateAvailable.Should().BeFalse();
        result.Message.Should().Contain("последняя версия");
    }

    [Fact]
    public async Task Check_ServerLatestTag_IsTreatedAsOutdated()
    {
        using var f = new UpdateRig(installedAppVersion: "1.20.0", envContent: "HQSTUDIO_VERSION=latest\n");

        await f.Coordinator.CheckAsync();

        f.Coordinator.Status.ServerUpdateAvailable.Should().BeTrue();
        f.Coordinator.Status.AppUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Check_NetworkFailure_ReturnsRussianError_AndKeepsStateUsable()
    {
        using var f = new UpdateRig();
        f.Github.Respond = _ => throw new HttpRequestException("offline");

        var result = await f.Coordinator.CheckAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("интернету");
        f.Coordinator.IsBusy.Should().BeFalse();
        File.Exists(f.StatePath).Should().BeFalse("a failed check must not start the 6 hour quiet period");
    }

    [Fact]
    public void NoInstallJson_MeansSiteNotInstalled()
    {
        using var f = new UpdateRig(siteInstalled: false);

        f.Coordinator.Status.SiteInstalled.Should().BeFalse();
        f.Coordinator.Status.SiteProblem.Should().BeNull();
        f.Coordinator.Status.ServerUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task NoInstallJson_ServerUpdateIsRefused_AppUpdateWorks()
    {
        using var f = new UpdateRig(siteInstalled: false);

        var server = await f.Coordinator.UpdateServerAsync();
        var app = await f.Coordinator.UpdateAppAsync();

        server.Success.Should().BeFalse();
        server.Message.Should().Contain("Сайт не установлен на этом компьютере");
        f.Runner.Calls.Should().BeEmpty();
        app.Success.Should().BeTrue(app.Message);
        app.RestartPending.Should().BeTrue();
    }

    [Fact]
    public void BrokenInstallJson_IsReportedAsProblem()
    {
        using var f = new UpdateRig(siteInstalled: false);
        File.WriteAllText(f.Temp.Combine("install.json"), "{ not json");

        f.Coordinator.RefreshLocalState();

        f.Coordinator.Status.SiteInstalled.Should().BeFalse();
        f.Coordinator.Status.SiteProblem.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateAll_UpdatesSiteFirst_ThenAppLast()
    {
        using var f = new UpdateRig();

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeTrue(result.Message);
        result.RestartPending.Should().BeTrue();
        f.Events.Should().Equal(
            "docker version", "docker compose pull", "docker compose up -d --remove-orphans", "launch-helper", "shutdown");
        f.Coordinator.Status.ServerUpdateAvailable.Should().BeFalse("the site is now on the latest version");
        File.ReadAllText(Path.Combine(f.ServerDir!, ".env")).Should().Contain("HQSTUDIO_VERSION=1.20.0");
    }

    [Fact]
    public async Task UpdateAll_StopsWhenSiteUpdateFails_AndDoesNotRestartApp()
    {
        using var f = new UpdateRig();
        f.Runner.Scripts["compose pull"] = new UpdateFakeScript(ExitCode: 1, StdErr: "boom");

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Обновление приложения не выполнялось");
        f.ShutdownCalls.Should().Be(0);
        f.Events.Should().NotContain("launch-helper");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
    }

    [Fact]
    public async Task UpdateAll_SiteUpdatedButAppFails_SaysSo()
    {
        using var f = new UpdateRig();
        f.Github.Respond = req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.Contains("api.github.com")) return UpdateFakeHttp.Text(UpdateTestData.ReleaseJson("v1.20.0"));
            if (url.EndsWith("server.zip")) return UpdateFakeHttp.Bytes(f.ServerZip);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().StartWith("Сайт обновлён, но приложение обновить не удалось");
        f.Coordinator.Status.ServerUpdateAvailable.Should().BeFalse();
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
    }

    [Fact]
    public async Task UpdateAll_WithoutSite_UpdatesOnlyApp()
    {
        using var f = new UpdateRig(siteInstalled: false);

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Events.Should().Equal("launch-helper", "shutdown");
    }

    [Fact]
    public async Task UpdateAll_WhenSiteAlreadyCurrent_UpdatesOnlyApp()
    {
        using var f = new UpdateRig(envContent: "HQSTUDIO_VERSION=1.20.0\n");

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Events.Should().Equal("launch-helper", "shutdown");
    }

    [Fact]
    public async Task UpdateAll_WhenEverythingCurrent_DoesNothing()
    {
        using var f = new UpdateRig(installedAppVersion: "1.20.0", envContent: "HQSTUDIO_VERSION=1.20.0\n");

        var result = await f.Coordinator.UpdateAllAsync();

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("уже обновлено");
        f.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAll_ProgressIsScaled_ServerFirstThenApp_AndMonotonic()
    {
        using var f = new UpdateRig();
        var reports = new List<UpdateProgress>();
        f.Coordinator.ProgressChanged += (_, p) => reports.Add(p);

        await f.Coordinator.UpdateAllAsync();

        reports.Select(r => r.Percent).Should().BeInAscendingOrder();
        var firstApp = reports.FindIndex(r => r.Stage.StartsWith("Приложение"));
        reports.Take(firstApp).Should().OnlyContain(r => r.Stage.StartsWith("Сайт") || r.Stage == "Подготовка");
        reports.Where(r => r.Stage.StartsWith("Сайт")).Max(r => r.Percent).Should().BeLessOrEqualTo(70);
        reports.Last().Percent.Should().Be(100);
    }

    [Fact]
    public async Task OperationsAreSerialized_SecondOneIsRejectedWhileBusy()
    {
        using var f = new UpdateRig();
        var gate = new ManualResetEventSlim(false);
        var reached = new ManualResetEventSlim(false);
        f.Runner.OnCall = key =>
        {
            if (key != "version") return;
            reached.Set();
            gate.Wait(TimeSpan.FromSeconds(10));
        };

        var first = Task.Run(() => f.Coordinator.UpdateServerAsync());
        UpdateOperationResult second;
        UpdateCheckResult check;
        try
        {
            reached.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("the first operation must reach the docker step");
            f.Coordinator.IsBusy.Should().BeTrue();
            second = await f.Coordinator.UpdateAppAsync();
            check = await f.Coordinator.CheckAsync();
        }
        finally
        {
            gate.Set();
        }
        var firstResult = await first;

        second.Success.Should().BeFalse();
        second.Message.Should().Contain("другое обновление");
        check.Success.Should().BeFalse();
        firstResult.Success.Should().BeTrue(firstResult.Message);
        f.Coordinator.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task StartupCheck_SilentlyChecks_AndShowsOneToast()
    {
        using var f = new UpdateRig();

        await f.Coordinator.RunStartupCheckAsync();

        f.GithubApiCalls.Should().Be(1);
        f.Toasts.Should().ContainSingle().Which.Should().Contain("Доступно обновление").And.Contain("1.20.0");
        f.Coordinator.GetLog().Should().BeEmpty("a silent check does not write to the visible log");
        f.Events.Should().BeEmpty("nothing is installed without a click");
        File.ReadAllBytes(f.ExePath).Should().Equal(f.OldExe);
    }

    [Fact]
    public async Task StartupCheck_RespectsAutoCheckSetting()
    {
        using var f = new UpdateRig();
        f.AutoCheck = false;

        await f.Coordinator.RunStartupCheckAsync();

        f.GithubApiCalls.Should().Be(0);
        f.Toasts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartupCheck_RunsAtMostOncePerSixHours()
    {
        using var f = new UpdateRig();
        await f.Coordinator.RunStartupCheckAsync();

        f.Now = f.Now.AddHours(5).AddMinutes(59);
        await f.Coordinator.RunStartupCheckAsync();
        f.GithubApiCalls.Should().Be(1, "still inside the quiet period");

        f.Now = f.Now.AddMinutes(2);
        await f.Coordinator.RunStartupCheckAsync();
        f.GithubApiCalls.Should().Be(2, "six hours have passed");
    }

    [Fact]
    public async Task StartupCheck_InsideQuietPeriod_StillKnowsAboutRememberedUpdate()
    {
        using var f = new UpdateRig();
        await f.Coordinator.RunStartupCheckAsync();

        // A fresh coordinator (new app start) reads the remembered tag and does not hit the network.
        var second = new UpdateRig();
        try
        {
            File.Copy(f.StatePath, second.StatePath, overwrite: true);
            second.Now = f.Now.AddHours(1);

            await second.Coordinator.RunStartupCheckAsync();

            second.GithubApiCalls.Should().Be(0);
            second.Coordinator.Status.AnyUpdateAvailable.Should().BeTrue();
            second.Toasts.Should().ContainSingle();
        }
        finally
        {
            second.Dispose();
        }
    }

    [Fact]
    public async Task StartupCheck_NoUpdates_NoToast()
    {
        using var f = new UpdateRig(installedAppVersion: "1.20.0", envContent: "HQSTUDIO_VERSION=1.20.0\n");

        await f.Coordinator.RunStartupCheckAsync();

        f.Toasts.Should().BeEmpty();
    }

    [Fact]
    public async Task StartupCheck_NetworkFailure_IsSilent()
    {
        using var f = new UpdateRig();
        f.Github.Respond = _ => throw new HttpRequestException("offline");

        await f.Coordinator.RunStartupCheckAsync();

        f.Toasts.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAfterRememberedTag_RefetchesFullReleaseFirst()
    {
        using var f = new UpdateRig();
        await f.Coordinator.RunStartupCheckAsync();
        var second = new UpdateRig();
        try
        {
            File.Copy(f.StatePath, second.StatePath, overwrite: true);
            second.Now = f.Now.AddHours(1);
            await second.Coordinator.RunStartupCheckAsync();
            second.Coordinator.Status.LatestIsComplete.Should().BeFalse();

            var result = await second.Coordinator.UpdateAppAsync();

            result.Success.Should().BeTrue(result.Message);
            second.GithubApiCalls.Should().Be(1, "assets are needed, so the release is fetched at click time");
        }
        finally
        {
            second.Dispose();
        }
    }

    [Fact]
    public void AutoCheckSetting_ReadsSettingsJson()
    {
        using var temp = new UpdateTempDir();
        var path = temp.Combine("settings.json");

        UpdateSettingsReader.IsAutoCheckEnabled(path).Should().BeTrue("missing file means enabled");

        File.WriteAllText(path, "{\"Theme\":\"Dark\"}");
        UpdateSettingsReader.IsAutoCheckEnabled(path).Should().BeTrue("missing key means enabled");

        File.WriteAllText(path, "{\"AutoCheckUpdates\": false}");
        UpdateSettingsReader.IsAutoCheckEnabled(path).Should().BeFalse();

        File.WriteAllText(path, "{\"AutoCheckUpdates\": true}");
        UpdateSettingsReader.IsAutoCheckEnabled(path).Should().BeTrue();

        File.WriteAllText(path, "not json");
        UpdateSettingsReader.IsAutoCheckEnabled(path).Should().BeTrue();
    }

    [Fact]
    public void InstallInfoReader_ParsesLayout()
    {
        using var temp = new UpdateTempDir();
        var path = temp.Combine("install.json");
        File.WriteAllText(path, "{\"serverDir\":\"C:\\\\Users\\\\X\\\\AppData\\\\Local\\\\HQStudio\\\\server\",\"webUrl\":\"http://localhost:8080\",\"apiUrl\":\"http://localhost:8080\",\"repo\":\"ibuildrun/hqstudio\"}");

        var result = InstallInfoReader.Read(path);

        result.Info.Should().NotBeNull();
        result.Info!.ServerDir.Should().Be(@"C:\Users\X\AppData\Local\HQStudio\server");
        result.Info.ApiUrl.Should().Be("http://localhost:8080");
        result.Info.Repo.Should().Be("ibuildrun/hqstudio");
        result.Problem.Should().BeNull();

        InstallInfoReader.Read(temp.Combine("absent.json")).NotInstalled.Should().BeTrue();

        File.WriteAllText(path, "{\"webUrl\":\"http://x\"}");
        InstallInfoReader.Read(path).Problem.Should().NotBeNull();
    }
}
