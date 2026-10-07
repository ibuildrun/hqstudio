using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using HQStudio.Setup.Core;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Real;
using Xunit;

namespace HQStudio.Setup.Tests;

public class ValidatorsTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Иван", true)]
    public void FirstName_IsRequired(string value, bool ok) => (Validators.FirstName(value) == null).Should().Be(ok);

    [Fact]
    public void LastName_IsRequired() => Validators.LastName("").Should().NotBeNull();

    [Fact]
    public void Password_ShorterThanEightIsRejected()
    {
        Validators.Password("1234567", "Иван", "Петров").Should().Contain("8");
        Validators.Password("12345678", "Иван", "Петров").Should().BeNull();
    }

    [Theory]
    [InlineData("иванпетров")]
    [InlineData("Иван Петров")]
    [InlineData("петров иван")]
    [InlineData("ПЕТРОВИВАН")]
    public void Password_EqualToTheNamesIsRejected(string password)
    {
        Validators.Password(password, "Иван", "Петров").Should().Contain("имен");
    }

    [Fact]
    public void Password_EqualToLastNameIsRejectedWhenLongEnough()
    {
        Validators.Password("Константинопольский", "Иван", "Константинопольский").Should().Contain("имен");
    }

    [Fact]
    public void Password_OnlySpacesIsRejected()
    {
        Validators.Password("         ", "Иван", "Петров").Should().NotBeNull();
    }

    [Fact]
    public void PasswordRepeat_MustMatchExactly()
    {
        Validators.PasswordRepeat("Abcdefg1", "Abcdefg1").Should().BeNull();
        Validators.PasswordRepeat("Abcdefg1", "abcdefg1").Should().NotBeNull();
        Validators.PasswordRepeat("Abcdefg1", "").Should().NotBeNull();
    }

    [Theory]
    [InlineData("", PasswordStrength.None)]
    [InlineData("abc", PasswordStrength.Weak)]
    [InlineData("12345678", PasswordStrength.Weak)]
    [InlineData("abcdefgh", PasswordStrength.Weak)]
    [InlineData("abcdefg1", PasswordStrength.Medium)]
    [InlineData("Sunny-Day-2026", PasswordStrength.Strong)]
    public void Strength_ClassifiesPasswords(string password, PasswordStrength expected)
    {
        Validators.Strength(password).Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("mysite", true)]
    [InlineData("my-site-2", true)]
    [InlineData("MySite", true)]
    [InlineData("-mysite", false)]
    [InlineData("mysite-", false)]
    [InlineData("my_site", false)]
    [InlineData("my site", false)]
    [InlineData("мой-сайт", false)]
    [InlineData("a--b", false)]
    public void Subdomain_AcceptsLatinLettersDigitsAndInnerDashes(string value, bool ok)
    {
        (Validators.Subdomain(value) == null).Should().Be(ok);
    }

    [Fact]
    public void Subdomain_TooLongIsRejected()
    {
        Validators.Subdomain(new string('a', Validators.MaxSubdomainLength + 1)).Should().NotBeNull();
    }

    [Fact]
    public void Key_WithSpacesIsRejected()
    {
        Validators.Key("abc def", "ключе").Should().NotBeNull();
        Validators.Key("  abc  ", "ключе").Should().BeNull("surrounding spaces are trimmed");
        Validators.Key("", "ключе").Should().BeNull();
    }
}

public class PayloadExtractorTests
{
    [Fact]
    public void Extract_PlacesAppAndServerFilesInTheirFolders()
    {
        using var temp = new TempDir();
        using var stream = new MemoryStream(FakePayload.StandardZip());

        var result = PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        File.ReadAllText(temp.Combine("app", "HQStudio.exe")).Should().Be("exe");
        File.Exists(temp.Combine("app", "ИНСТРУКЦИЯ.html")).Should().BeTrue();
        File.Exists(temp.Combine("server", "docker-compose.yml")).Should().BeTrue();
        File.Exists(temp.Combine("server", ".env.example")).Should().BeTrue();
        File.Exists(temp.Combine("server", "nginx", "default.conf")).Should().BeTrue();
        result.AppFiles.Should().BeEquivalentTo("HQStudio.exe", "ИНСТРУКЦИЯ.html");
        result.ServerFiles.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("app/../../evil.txt")]
    [InlineData("app/..\\..\\evil.txt")]
    [InlineData("server/../../evil.txt")]
    [InlineData("/abs/evil.txt")]
    [InlineData("app/C:/evil.txt")]
    [InlineData("app/sub/../../../evil.txt")]
    public void Extract_RejectsZipSlipAndWritesNothing(string evilName)
    {
        using var temp = new TempDir();
        var zip = FakePayload.Zip(new[] { ("app/HQStudio.exe", "exe"), (evilName, "pwned") });
        using var stream = new MemoryStream(zip);

        var act = () => PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        act.Should().Throw<InstallException>().Which.Kind.Should().Be(FailureKind.PayloadCorrupt);
        Directory.Exists(temp.Combine("app")).Should().BeFalse("nothing is written before every path is validated");
        File.Exists(System.IO.Path.Combine(temp.Path, "evil.txt")).Should().BeFalse();
        File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(temp.Path)!, "evil.txt")).Should().BeFalse();
    }

    [Fact]
    public void Extract_AcceptsBackslashSeparatedEntryNames()
    {
        using var temp = new TempDir();
        var zip = FakePayload.Zip(new[] { ("app\\HQStudio.exe", "exe"), ("server\\nginx\\default.conf", "conf") });
        using var stream = new MemoryStream(zip);

        PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        File.Exists(temp.Combine("app", "HQStudio.exe")).Should().BeTrue();
        File.Exists(temp.Combine("server", "nginx", "default.conf")).Should().BeTrue();
    }

    [Fact]
    public void Extract_NeverOverwritesTheUsersEnvFile()
    {
        using var temp = new TempDir();
        Directory.CreateDirectory(temp.Combine("server"));
        File.WriteAllText(temp.Combine("server", ".env"), "POSTGRES_PASSWORD=mine");
        var zip = FakePayload.Zip(new[] { ("app/HQStudio.exe", "exe"), ("server/.env", "POSTGRES_PASSWORD=from-archive") });
        using var stream = new MemoryStream(zip);

        PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        File.ReadAllText(temp.Combine("server", ".env")).Should().Be("POSTGRES_PASSWORD=mine");
    }

    [Fact]
    public void Extract_OverwritesOlderProgramFiles()
    {
        using var temp = new TempDir();
        Directory.CreateDirectory(temp.Combine("app"));
        File.WriteAllText(temp.Combine("app", "HQStudio.exe"), "old");
        using var stream = new MemoryStream(FakePayload.StandardZip());

        PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        File.ReadAllText(temp.Combine("app", "HQStudio.exe")).Should().Be("exe");
    }

    [Fact]
    public void Extract_ArchiveWithoutTheProgramIsRejected()
    {
        using var temp = new TempDir();
        using var stream = new MemoryStream(FakePayload.Zip(new[] { ("server/docker-compose.yml", "x") }));

        var act = () => PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        act.Should().Throw<InstallException>().Which.Kind.Should().Be(FailureKind.PayloadCorrupt);
    }

    [Fact]
    public void Extract_NotAZipIsReportedAsCorrupt()
    {
        using var temp = new TempDir();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var act = () => PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        act.Should().Throw<InstallException>().Which.Kind.Should().Be(FailureKind.PayloadCorrupt);
    }

    [Fact]
    public void Extract_IgnoresEntriesOutsideAppAndServer()
    {
        using var temp = new TempDir();
        var zip = FakePayload.Zip(new[] { ("app/HQStudio.exe", "exe"), ("readme.txt", "hello"), ("other/file.txt", "x") });
        using var stream = new MemoryStream(zip);

        PayloadExtractor.Extract(stream, temp.Combine("app"), temp.Combine("server"));

        Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Should().ContainSingle();
    }
}

public class AppConfigWriterTests
{
    [Fact]
    public void InstallJson_HasTheContractedFields()
    {
        var text = AppConfigWriter.InstallJsonText(@"C:\Users\Иван\AppData\Local\HQStudio\server", 8081);

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        root.GetProperty("serverDir").GetString().Should().Be(@"C:\Users\Иван\AppData\Local\HQStudio\server");
        root.GetProperty("webUrl").GetString().Should().Be("http://localhost:8081");
        root.GetProperty("apiUrl").GetString().Should().Be("http://localhost:8081");
        root.GetProperty("repo").GetString().Should().Be("ibuildrun/hqstudio");
        root.EnumerateObject().Select(p => p.Name).Should().Equal("serverDir", "webUrl", "apiUrl", "repo");
    }

    [Fact]
    public void SettingsJson_HasTheContractedFields()
    {
        using var doc = JsonDocument.Parse(AppConfigWriter.SettingsJsonText(8080));
        var root = doc.RootElement;

        root.GetProperty("Theme").GetString().Should().Be("Dark");
        root.GetProperty("ShowSplash").GetBoolean().Should().BeTrue();
        root.GetProperty("Language").GetString().Should().Be("ru");
        root.GetProperty("ApiUrl").GetString().Should().Be("http://localhost:8080");
        root.GetProperty("UseApi").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void WriteSettingsIfAbsent_DoesNotOverwriteAnExistingFile()
    {
        using var temp = new TempDir();
        var paths = InstallPaths.Under(temp.Path);
        Directory.CreateDirectory(paths.SettingsDir);
        File.WriteAllText(paths.SettingsJson, "{\"Theme\":\"Light\"}");

        AppConfigWriter.WriteSettingsIfAbsent(paths, 8080).Should().BeFalse();

        File.ReadAllText(paths.SettingsJson).Should().Be("{\"Theme\":\"Light\"}");
    }

    [Fact]
    public void WriteSettingsIfAbsent_CreatesTheFileWhenMissing()
    {
        using var temp = new TempDir();
        var paths = InstallPaths.Under(temp.Path);

        AppConfigWriter.WriteSettingsIfAbsent(paths, 8082).Should().BeTrue();

        File.ReadAllText(paths.SettingsJson).Should().Contain("http://localhost:8082");
    }

    [Fact]
    public void WriteInstallJson_WritesUtf8WithoutBom()
    {
        using var temp = new TempDir();
        var paths = InstallPaths.Under(temp.Path);

        AppConfigWriter.WriteInstallJson(paths, 8080);

        File.ReadAllBytes(paths.InstallJson).Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        using var doc = JsonDocument.Parse(File.ReadAllText(paths.InstallJson));
        doc.RootElement.GetProperty("serverDir").GetString().Should().Be(paths.ServerDir);
    }
}

public class ComposeArgsAndDockerClientTests
{
    private sealed class RecordingRunner : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = new();
        public Func<ProcessRequest, ProcessResult> Responder { get; set; } = _ => new ProcessResult(0, "");

        public Task<ProcessResult> RunAsync(ProcessRequest request, Action<string>? onLine, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(Responder(request));
        }
    }

    [Fact]
    public void ComposeArgs_BuildsTheContractedCommandLine()
    {
        var args = ComposeArgs.Build(@"C:\srv", tunnel: false, "up", "-d");

        args.Should().Equal("compose", "--project-directory", @"C:\srv", "-f", @"C:\srv\docker-compose.yml", "up", "-d");
    }

    [Fact]
    public void ComposeArgs_AddsTheTunnelProfileOnlyWhenAsked()
    {
        ComposeArgs.Build(@"C:\srv", tunnel: true, "pull").Should().Contain(new[] { "--profile", "tunnel" });
        ComposeArgs.Build(@"C:\srv", tunnel: false, "pull").Should().NotContain("--profile");
    }

    [Fact]
    public async Task DockerClient_RunsComposeThroughTheFoundDockerExeWithTheTunnelProfile()
    {
        var runner = new RecordingRunner();
        var client = new DockerClient(runner, fileExists: p => p == @"C:\PF\Docker\Docker\resources\bin\docker.exe",
            pathVariable: "", programFiles: @"C:\PF", localAppData: @"C:\L");

        var result = await client.ComposeAsync(@"C:\srv", tunnel: true, new[] { "up", "-d" }, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        var request = runner.Requests.Single();
        request.FileName.Should().Be(@"C:\PF\Docker\Docker\resources\bin\docker.exe");
        request.Arguments.Should().Equal("compose", "--project-directory", @"C:\srv", "-f", @"C:\srv\docker-compose.yml", "--profile", "tunnel", "up", "-d");
        request.Environment!["PATH"].Should().StartWith(@"C:\PF\Docker\Docker\resources\bin;");
    }

    [Fact]
    public async Task DockerClient_ReportsNotInstalledWhenNoDockerExeExists()
    {
        var client = new DockerClient(new RecordingRunner(), fileExists: _ => false, pathVariable: @"C:\Windows", programFiles: @"C:\PF", localAppData: @"C:\L");

        (await client.GetStatusAsync(CancellationToken.None)).Should().Be(DockerStatus.NotInstalled);
    }

    [Theory]
    [InlineData(0, DockerStatus.Running)]
    [InlineData(1, DockerStatus.InstalledNotRunning)]
    public async Task DockerClient_DistinguishesRunningFromStopped(int exitCode, DockerStatus expected)
    {
        var runner = new RecordingRunner { Responder = _ => new ProcessResult(exitCode, "") };
        var client = new DockerClient(runner, fileExists: p => p.EndsWith("docker.exe"), pathVariable: @"C:\tools", programFiles: @"C:\PF", localAppData: @"C:\L");

        (await client.GetStatusAsync(CancellationToken.None)).Should().Be(expected);
        runner.Requests.Single().Arguments.Should().StartWith("info");
    }

    [Fact]
    public async Task DockerClient_TimedOutInfoMeansNotRunning()
    {
        var runner = new RecordingRunner { Responder = _ => new ProcessResult(-1, "", TimedOut: true) };
        var client = new DockerClient(runner, fileExists: p => p.EndsWith("docker.exe"), pathVariable: @"C:\tools", programFiles: @"C:\PF", localAppData: @"C:\L");

        (await client.GetStatusAsync(CancellationToken.None)).Should().Be(DockerStatus.InstalledNotRunning);
    }

    [Fact]
    public void DockerClient_FindsDockerOnThePathFirst()
    {
        var client = new DockerClient(new RecordingRunner(), fileExists: p => p == @"D:\bin\docker.exe", pathVariable: @"C:\x;D:\bin", programFiles: @"C:\PF", localAppData: @"C:\L");

        client.FindDocker().Should().Be(@"D:\bin\docker.exe");
    }

    [Fact]
    public async Task ProcessRunner_RunsAHiddenProcessAndCapturesItsOutput()
    {
        var runner = new ProcessRunner();
        var lines = new List<string>();

        var result = await runner.RunAsync(
            new ProcessRequest("cmd.exe", new[] { "/c", "echo first&& echo second" }), lines.Add, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        lines.Should().Equal("first", "second");
        result.Output.Should().Contain("first").And.Contain("second");
    }

    [Fact]
    public async Task ProcessRunner_KillsTheProcessOnTimeout()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(
            new ProcessRequest("cmd.exe", new[] { "/c", "ping -n 30 127.0.0.1 >nul" }, Timeout: TimeSpan.FromMilliseconds(400)),
            null, CancellationToken.None);

        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRunner_ReportsAMissingProgramInsteadOfThrowing()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(new ProcessRequest(@"C:\definitely\not\here.exe", Array.Empty<string>()), null, CancellationToken.None);

        result.ExitCode.Should().Be(-1);
    }
}

public class SetupOptionsTests
{
    [Fact]
    public void NoArguments_MeansARealInstall()
    {
        var options = SetupOptions.Parse(Array.Empty<string>());

        options.Simulate.Should().BeFalse();
        options.RenderPagesDir.Should().BeNull();
        options.SimulateFail.Should().BeNull();
    }

    [Fact]
    public void Simulate_IsRecognised() => SetupOptions.Parse(new[] { "--simulate" }).Simulate.Should().BeTrue();

    [Theory]
    [InlineData("--simulate-fail=pull", StageId.Pull)]
    [InlineData("--simulate-fail=Health", StageId.Health)]
    [InlineData("--simulate-fail=docker", StageId.DockerReady)]
    [InlineData("--simulate-fail=tunnel", StageId.PublicUrl)]
    [InlineData("--simulate-fail=shortcuts", StageId.Shortcuts)]
    public void SimulateFail_ParsesStageNamesAndImpliesSimulation(string arg, StageId expected)
    {
        var options = SetupOptions.Parse(new[] { arg });

        options.SimulateFail.Should().Be(expected);
        options.Simulate.Should().BeTrue();
    }

    [Fact]
    public void SimulateFail_UnknownStageIsReportedAsWarning()
    {
        var options = SetupOptions.Parse(new[] { "--simulate-fail=nonsense" });

        options.SimulateFail.Should().BeNull();
        options.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void RenderPages_AcceptsSeparateAndInlineValues()
    {
        SetupOptions.Parse(new[] { "--render-pages", @"C:\out" }).RenderPagesDir.Should().Be(@"C:\out");
        SetupOptions.Parse(new[] { "--render-pages=C:\\out2" }).RenderPagesDir.Should().Be(@"C:\out2");
    }

    [Fact]
    public void SimDocker_ParsesModes()
    {
        SetupOptions.Parse(new[] { "--sim-docker=missing" }).SimDocker.Should().Be(SimDockerMode.Missing);
        SetupOptions.Parse(new[] { "--sim-docker", "stopped" }).SimDocker.Should().Be(SimDockerMode.Stopped);
    }
}
