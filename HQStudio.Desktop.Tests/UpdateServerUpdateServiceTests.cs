using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateServerUpdateServiceTests
{
    private const string Version = "docker version";
    private const string Pull = "docker compose pull";
    private const string Up = "docker compose up -d --remove-orphans";
    private const string UpPlain = "docker compose up -d";

    private sealed class Fixture : IDisposable
    {
        public UpdateTempDir Temp { get; } = new();
        public string ServerDir { get; }
        public InstallInfo Install { get; }
        public UpdateFakeProcess Runner { get; } = new();
        public UpdateFakeHttp Downloads { get; } = new();
        public UpdateFakeHttp Health { get; } = new();
        public byte[] ServerZip { get; }
        public ReleaseInfo Release { get; }
        public UpdateProgressRecorder Progress { get; } = new();
        public List<string> Log { get; } = new();
        public string EnvPath => Path.Combine(ServerDir, ".env");

        public Fixture(bool withDigest = true, string? badDigest = null, string envContent = UpdateTestData.OldEnv)
        {
            ServerDir = UpdateTestData.CreateInstall(Temp, envContent);
            Install = UpdateTestData.Install(ServerDir);
            ServerZip = UpdateTestData.ServerZip();
            Release = UpdateTestData.Release("1.20.0", ServerZip, withDigest, badDigest: badDigest);
            Downloads.Respond = _ => UpdateFakeHttp.Bytes(ServerZip);
            Health.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK);
        }

        public ServerUpdateService Service(ServerUpdateOptions? options = null) =>
            new(Runner, new ReleaseDownloader(Downloads), Health, options ?? UpdateTestData.FastOptions(Temp));

        public Task<UpdateOperationResult> RunAsync(ServerUpdateOptions? options = null) =>
            Service(options).UpdateAsync(Release, Install, Progress, Log.Add, CancellationToken.None);

        public string Read(params string[] relative) => File.ReadAllText(Path.Combine(new[] { ServerDir }.Concat(relative).ToArray()));

        public void Dispose() => Temp.Dispose();
    }

    [Fact]
    public async Task HappyPath_RunsDockerStepsInOrder_AndChecksHealth()
    {
        using var f = new Fixture();
        f.Runner.Scripts["compose pull"] = new UpdateFakeScript(Lines: new[]
        {
            " Image postgres:16-alpine Pulling ",
            " 3c6d4a1b9e2f Pulling fs layer ",
            " 3c6d4a1b9e2f Downloading [=>   ]  2.1MB/29MB",
            " 3c6d4a1b9e2f Pull complete ",
            " Image postgres:16-alpine Pulled "
        });

        var result = await f.RunAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Runner.Calls.Should().Equal(Version, Pull, Up);
        f.Runner.WorkingDirectories.Skip(1).Should().OnlyContain(d => d == f.ServerDir);
        f.Health.Requests.Should().ContainSingle().Which.Should().Be("http://localhost:8080/api/health");
        f.Log.Should().Contain(l => l.Contains("Pull complete"), "docker output is streamed to the log");
    }

    [Fact]
    public async Task ComposeIsInvokedLikeTheInstaller_WithProjectDirectoryAndComposeFile()
    {
        using var f = new Fixture();

        await f.RunAsync();

        var compose = Path.Combine(f.ServerDir, "docker-compose.yml");
        f.Runner.RawCalls.Should().Equal(
            "docker version",
            $"docker compose --project-directory {f.ServerDir} -f {compose} pull",
            $"docker compose --project-directory {f.ServerDir} -f {compose} up -d --remove-orphans");
    }

    [Fact]
    public async Task ComposeUsesTunnelProfile_WhenTunaTokenIsConfigured()
    {
        using var f = new Fixture(envContent: "TUNA_TOKEN=abc123\nHQSTUDIO_VERSION=1.19.6\n");

        await f.RunAsync();

        f.Runner.RawCalls.Skip(1).Should().OnlyContain(c => c.Contains("--profile tunnel "));
        f.Runner.Calls.Should().Equal(Version, Pull, Up);
    }

    [Fact]
    public async Task ComposeHasNoTunnelProfile_WhenTokenIsEmpty()
    {
        using var f = new Fixture(envContent: "TUNA_TOKEN=\nHQSTUDIO_VERSION=1.19.6\n");

        await f.RunAsync();

        f.Runner.RawCalls.Should().NotContain(c => c.Contains("--profile"));
    }

    [Fact]
    public async Task HappyPath_UpdatesEnvVersion_KeepsEveryOtherLine_AndNeverTouchesEnvFromZip()
    {
        using var f = new Fixture();

        await f.RunAsync();

        f.Read(".env").Should().Be("# site settings\r\nPOSTGRES_PASSWORD=secret\r\nHQSTUDIO_VERSION=1.20.0\r\nWEB_PORT=8080\r\n");
        f.Read(".env").Should().NotContain("OVERWRITTEN");
        Directory.GetFiles(f.ServerDir, ".*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task HappyPath_ReplacesDeployFiles_AndBacksUpPreviousOnes()
    {
        using var f = new Fixture();

        await f.RunAsync();

        f.Read("docker-compose.yml").Should().Be("services: new\n");
        f.Read("nginx", "default.conf").Should().Be("# new nginx\n");
        f.Read("scripts", "deploy.ps1").Should().Be("Write-Host new\n");

        var backup = Path.Combine(f.ServerDir, ServerUpdateService.BackupFolderName);
        File.ReadAllText(Path.Combine(backup, "files", "docker-compose.yml")).Should().Be(UpdateTestData.OldCompose);
        File.ReadAllText(Path.Combine(backup, "files", "nginx", "default.conf")).Should().Be(UpdateTestData.OldNginx);
        File.ReadAllText(Path.Combine(backup, "env.backup")).Should().Be(UpdateTestData.OldEnv);
    }

    [Fact]
    public async Task HappyPath_ProgressIsMonotonicAndEndsAt100()
    {
        using var f = new Fixture();
        f.Runner.Scripts["compose pull"] = new UpdateFakeScript(Lines: new[]
        {
            " Image a:1 Pulling ", " Image b:1 Pulling ", " Image a:1 Pulled ", " Image b:1 Pulled "
        });

        await f.RunAsync();

        var percents = f.Progress.Items.Select(p => p.Percent).ToList();
        percents.Should().BeInAscendingOrder();
        percents.Last().Should().Be(100);
        f.Progress.Items.Select(p => p.Stage).Should().Contain(new[] { "Проверка Docker", "Загрузка образов", "Проверка работы сайта" });
        f.Progress.Items.Where(p => p.Stage == "Загрузка образов").Select(p => p.Percent).Should().Contain(p => p > 26 && p < 76);
    }

    [Fact]
    public async Task HappyPath_WaitsForHealthUntilItTurnsGreen()
    {
        using var f = new Fixture();
        var attempts = 0;
        f.Health.Respond = _ => ++attempts < 4
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK);

        var result = await f.RunAsync(new ServerUpdateOptions
        {
            TempRoot = f.Temp.Combine("tmp"),
            HealthTimeout = TimeSpan.FromSeconds(5),
            HealthPollInterval = TimeSpan.FromMilliseconds(10)
        });

        result.Success.Should().BeTrue(result.Message);
        f.Health.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task DockerNotRunning_FailsBeforeAnythingIsDownloadedOrChanged()
    {
        using var f = new Fixture();
        f.Runner.Scripts["version"] = new UpdateFakeScript(ExitCode: 1, StdErr: "error during connect: open //./pipe/dockerDesktopLinuxEngine");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeFalse();
        result.Message.Should().Contain("Docker Desktop").And.Contain("не запущен").And.Contain(UpdateLinks.DockerInstallUrl);
        result.HelpUrl.Should().Be(UpdateLinks.DockerInstallUrl);
        f.Runner.Calls.Should().Equal(Version);
        f.Downloads.Requests.Should().BeEmpty();
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
        f.Read("docker-compose.yml").Should().Be(UpdateTestData.OldCompose);
    }

    [Fact]
    public async Task DockerNotInstalled_ExplainsAndLinks()
    {
        using var f = new Fixture();
        f.Runner.Scripts["version"] = new UpdateFakeScript(Throw: UpdateTestData.DockerMissing());

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Docker не найден").And.Contain(UpdateLinks.DockerInstallUrl);
        result.HelpUrl.Should().Be(UpdateLinks.DockerInstallUrl);
        f.Downloads.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task PullFails_RollsBackFilesAndEnv_AndRestartsPreviousVersion()
    {
        using var f = new Fixture();
        f.Runner.Scripts["compose pull"] = new UpdateFakeScript(ExitCode: 1, Lines: new[] { "Error response from daemon: manifest unknown" },
            StdErr: "Error response from daemon: manifest for ghcr.io/ibuildrun/hqstudio/api:1.20.0 not found: manifest unknown");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        result.Message.Should().Contain("ещё не опубликованы").And.Contain("Прежняя версия сайта восстановлена");
        f.Runner.Calls.Should().Equal(Version, Pull, UpPlain);
        f.Read(".env").Should().Be(UpdateTestData.OldEnv, "the original .env is restored byte for byte");
        f.Read("docker-compose.yml").Should().Be(UpdateTestData.OldCompose);
        f.Read("nginx", "default.conf").Should().Be(UpdateTestData.OldNginx);
        File.Exists(Path.Combine(f.ServerDir, "scripts", "deploy.ps1")).Should().BeFalse("files created by the update are removed");
        f.Log.Should().Contain(l => l.Contains("Возвращаю прежнюю версию"));
    }

    [Fact]
    public async Task UpFails_RollsBack()
    {
        using var f = new Fixture();
        f.Runner.Scripts["compose up -d --remove-orphans"] = new UpdateFakeScript(ExitCode: 1,
            StdErr: "Bind for 0.0.0.0:8080 failed: port is already allocated");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        result.Message.Should().Contain("порт");
        f.Runner.Calls.Should().Equal(Version, Pull, Up, UpPlain);
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
    }

    [Fact]
    public async Task HealthNeverTurnsGreen_RollsBack_AndReportsRestoredSite()
    {
        using var f = new Fixture();
        // Unhealthy while the new version runs, healthy again after the rollback "up -d".
        f.Health.Respond = _ => f.Runner.Calls.Last() == UpPlain
            ? new HttpResponseMessage(HttpStatusCode.OK)
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        result.Message.Should().Contain("не ответил").And.Contain("Прежняя версия сайта восстановлена и работает");
        f.Runner.Calls.Should().Equal(Version, Pull, Up, UpPlain);
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
        f.Read("docker-compose.yml").Should().Be(UpdateTestData.OldCompose);
    }

    [Fact]
    public async Task HealthNeverTurnsGreen_AndRollbackAlsoDead_TellsUserPlainly()
    {
        using var f = new Fixture();
        f.Health.Respond = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        result.Message.Should().Contain("Не удалось автоматически запустить прежнюю версию").And.Contain("Docker Desktop");
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
    }

    [Fact]
    public async Task HealthConnectionRefused_CountsAsUnhealthy()
    {
        using var f = new Fixture();
        f.Health.Respond = _ => throw new HttpRequestException("refused");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        f.Health.Requests.Count.Should().BeGreaterThan(1, "it keeps polling until the timeout");
    }

    [Fact]
    public async Task DigestMismatch_FailsBeforeTouchingTheInstall()
    {
        using var f = new Fixture(badDigest: new string('0', 64));

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeFalse();
        result.Message.Should().Contain("повреждён");
        f.Runner.Calls.Should().Equal(Version);
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
        f.Read("docker-compose.yml").Should().Be(UpdateTestData.OldCompose);
        Directory.Exists(Path.Combine(f.ServerDir, ServerUpdateService.BackupFolderName)).Should().BeFalse();
    }

    [Fact]
    public async Task MissingDigest_SkipsVerification()
    {
        using var f = new Fixture(withDigest: false);

        var result = await f.RunAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Log.Should().Contain(l => l.Contains("проверка пропущена"));
    }

    [Fact]
    public async Task CorruptArchive_FailsWithoutTouchingInstall()
    {
        using var f = new Fixture(withDigest: false);
        f.Downloads.Respond = _ => UpdateFakeHttp.Bytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("повреждён");
        f.Runner.Calls.Should().Equal(Version);
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
    }

    [Fact]
    public async Task DownloadFailure_FailsWithoutTouchingInstall()
    {
        using var f = new Fixture();
        f.Downloads.Respond = _ => throw new HttpRequestException("offline");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeFalse();
        result.Message.Should().Contain("интернету");
        f.Read(".env").Should().Be(UpdateTestData.OldEnv);
    }

    [Fact]
    public async Task ZipWithWrapperFolder_IsFlattened()
    {
        using var f = new Fixture(withDigest: false);
        var wrapped = UpdateTestData.Zip(
            ("HQStudio-Server-v1.20.0/docker-compose.yml", "services: wrapped\n"),
            ("HQStudio-Server-v1.20.0/nginx/default.conf", "# wrapped\n"));
        f.Downloads.Respond = _ => UpdateFakeHttp.Bytes(wrapped);

        var result = await f.RunAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Read("docker-compose.yml").Should().Be("services: wrapped\n");
        Directory.Exists(Path.Combine(f.ServerDir, "HQStudio-Server-v1.20.0")).Should().BeFalse();
    }

    [Fact]
    public async Task MissingInstallFolder_ReportsClearly()
    {
        using var f = new Fixture();
        var install = f.Install with { ServerDir = f.Temp.Combine("nowhere") };

        var result = await f.Service().UpdateAsync(f.Release, install, null, null, default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Не найдена папка сайта");
        f.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingEnvFile_ReportsClearly_WithoutRunningDocker()
    {
        using var f = new Fixture();
        File.Delete(f.EnvPath);

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain(".env");
        f.Runner.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReleaseWithoutServerAsset_ReportsClearly()
    {
        using var f = new Fixture();
        var release = UpdateTestData.Release("1.20.0");

        var result = await f.Service().UpdateAsync(release, f.Install, null, null, default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("нет файлов сайта");
    }

    [Fact]
    public async Task PreviousVersionLatest_IsReplacedWithConcreteTag()
    {
        using var f = new Fixture(envContent: "HQSTUDIO_VERSION=latest\nA=1\n");

        var result = await f.RunAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Read(".env").Should().Be("HQSTUDIO_VERSION=1.20.0\nA=1\n");
    }

    [Fact]
    public async Task KeyMissingInEnv_IsAppended()
    {
        using var f = new Fixture(envContent: "A=1\n");

        var result = await f.RunAsync();

        result.Success.Should().BeTrue(result.Message);
        f.Read(".env").Should().Be("A=1\nHQSTUDIO_VERSION=1.20.0\n");
    }

    [Fact]
    public async Task KeyMissingInEnv_RollbackRestoresOriginalContent()
    {
        using var f = new Fixture(envContent: "A=1\n");
        f.Runner.Scripts["compose pull"] = new UpdateFakeScript(ExitCode: 1, StdErr: "some failure");

        var result = await f.RunAsync();

        result.Success.Should().BeFalse();
        f.Read(".env").Should().Be("A=1\n");
    }

    [Theory]
    [InlineData("Cannot connect to the Docker daemon at unix:///var/run/docker.sock", "Docker остановился")]
    [InlineData("write /var/lib/docker/tmp: no space left on device", "не хватает места")]
    [InlineData("Get \"https://ghcr.io/v2/\": dial tcp: lookup ghcr.io: no such host", "нет связи")]
    [InlineData("Head \"https://ghcr.io/v2/x\": denied", "доступ запрещён")]
    [InlineData("toomanyrequests: You have reached your pull rate limit", "ограничило скачивание")]
    [InlineData("something unexpected happened", "something unexpected happened")]
    public void ExplainDockerFailure_MapsKnownCauses(string stderr, string expectedFragment)
    {
        var message = ServerUpdateService.ExplainDockerFailure(new ProcessResult(1, "", stderr), "загрузке образов");

        message.Should().Contain(expectedFragment);
    }

    [Fact]
    public void ReadInstalledVersion_ReadsEnv()
    {
        using var f = new Fixture();
        ServerUpdateService.ReadInstalledVersion(f.Install).Should().Be("1.19.6");
        ServerUpdateService.ReadInstalledVersion(f.Install with { ServerDir = f.Temp.Combine("none") }).Should().BeNull();
    }
}

public class UpdateArchiveExtractorTests
{
    [Fact]
    public void Extract_SkipsEnv_AndWritesTheRest()
    {
        using var temp = new UpdateTempDir();
        var zip = temp.Combine("a.zip");
        File.WriteAllBytes(zip, UpdateTestData.ServerZip());

        var written = ArchiveExtractor.ExtractToDirectory(zip, temp.Combine("out"), p => p == ".env");

        written.Should().BeEquivalentTo("docker-compose.yml", "nginx/default.conf", "scripts/deploy.ps1");
        File.Exists(temp.Combine("out", ".env")).Should().BeFalse();
    }

    [Fact]
    public void Extract_RejectsPathTraversal()
    {
        using var temp = new UpdateTempDir();
        var zip = temp.Combine("evil.zip");
        File.WriteAllBytes(zip, UpdateTestData.Zip(("docker-compose.yml", "ok"), ("../../evil.txt", "bad")));

        var act = () => ArchiveExtractor.ExtractToDirectory(zip, temp.Combine("out"));

        act.Should().Throw<UpdateException>().WithMessage("*недопустимые пути*");
        File.Exists(temp.Combine("evil.txt")).Should().BeFalse();
    }

    [Fact]
    public void Extract_RejectsCorruptArchive()
    {
        using var temp = new UpdateTempDir();
        var zip = temp.Combine("bad.zip");
        File.WriteAllBytes(zip, new byte[] { 1, 2, 3, 4 });

        var act = () => ArchiveExtractor.ExtractToDirectory(zip, temp.Combine("out"));

        act.Should().Throw<UpdateException>().WithMessage("*повреждён*");
    }
}
