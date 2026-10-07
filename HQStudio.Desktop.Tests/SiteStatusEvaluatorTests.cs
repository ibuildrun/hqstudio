using FluentAssertions;
using HQStudio.Services.Site;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class SiteStatusEvaluatorTests
{
    private static ComposeServiceEntry E(string service, string state = "running", string health = "healthy",
        string status = "Up", int? exit = null) => new(service, state, health, status, exit);

    private static ComposeServiceEntry[] Core(string state = "running", string health = "healthy") =>
        new[] { E("db", state, health), E("api", state, health), E("web", state, health), E("proxy", state, health) };

    private static SiteStatusInput Input(IReadOnlyList<ComposeServiceEntry>? entries, bool tunnel = false,
        SiteDockerState docker = SiteDockerState.Running, bool? health = true, int failures = 0,
        SiteOperation op = SiteOperation.None, bool installed = true, string? installProblem = null,
        string? composeProblem = null) =>
        new(installed, installProblem, docker, entries, composeProblem, tunnel, health, failures, op);

    private static SiteOverview Evaluate(SiteStatusInput input) =>
        SiteStatusEvaluator.Evaluate(input, SiteStatusEvaluator.BuildServices(input));

    // ---------------------------------------------------------------- одна служба

    [Theory]
    [InlineData("running", "healthy", ServiceLevel.Ok, "Работает")]
    [InlineData("running", "", ServiceLevel.Ok, "Работает")]
    [InlineData("running", "starting", ServiceLevel.Starting, "Запускается")]
    [InlineData("running", "unhealthy", ServiceLevel.Error, "Не отвечает")]
    [InlineData("restarting", "", ServiceLevel.Error, "Перезапускается")]
    [InlineData("created", "", ServiceLevel.Starting, "Запускается")]
    [InlineData("paused", "", ServiceLevel.Stopped, "На паузе")]
    [InlineData("dead", "", ServiceLevel.Error, "Сбой")]
    [InlineData("exited", "", ServiceLevel.Stopped, "Остановлен")]
    [InlineData("weird", "", ServiceLevel.Unknown, "Неизвестно")]
    public void ClassifyEntry_MapsStateAndHealth(string state, string health, ServiceLevel level, string badge)
    {
        var status = SiteStatusEvaluator.ClassifyEntry("api", E("api", state, health, "raw text"));

        status.Level.Should().Be(level);
        status.BadgeText.Should().Be(badge);
        status.RawText.Should().Be("raw text");
        status.Title.Should().Be("Сервер (API)");
    }

    [Fact]
    public void ClassifyEntry_ExitedWithNonZeroCode_IsError()
    {
        SiteStatusEvaluator.ClassifyEntry("web", E("web", "exited", "", "Exited (1) 2 minutes ago", 1))
            .Level.Should().Be(ServiceLevel.Error);
    }

    [Fact]
    public void ClassifyEntry_ExitCodeReadFromStatusTextWhenFieldMissing()
    {
        SiteStatusEvaluator.ClassifyEntry("web", E("web", "exited", "", "Exited (137) 5 seconds ago"))
            .Level.Should().Be(ServiceLevel.Error);
        SiteStatusEvaluator.ClassifyEntry("web", E("web", "exited", "", "Exited (0) 5 seconds ago"))
            .Level.Should().Be(ServiceLevel.Stopped);
    }

    [Fact]
    public void ClassifyEntry_MissingContainer_IsAbsent()
    {
        var status = SiteStatusEvaluator.ClassifyEntry("db", null);

        status.Level.Should().Be(ServiceLevel.Absent);
        status.BadgeText.Should().Be("Не запущен");
    }

    // ---------------------------------------------------------------- карточки

    [Fact]
    public void BuildServices_ReturnsFiveCardsInOrder()
    {
        var services = SiteStatusEvaluator.BuildServices(Input(Core()));

        services.Select(s => s.Id).Should().Equal("db", "api", "web", "proxy", "tuna");
        services.Select(s => s.Title).Should().Equal("База данных", "Сервер (API)", "Сайт", "Прокси", "Адрес в интернете");
    }

    [Fact]
    public void BuildServices_NoToken_TunnelCardIsNotConfigured_EvenIfContainerExists()
    {
        var entries = Core().Append(E("tuna", health: "")).ToArray();

        var tuna = SiteStatusEvaluator.BuildServices(Input(entries, tunnel: false)).Last();

        tuna.Level.Should().Be(ServiceLevel.NotConfigured);
        tuna.BadgeText.Should().Be("Не настроен");
    }

    [Fact]
    public void BuildServices_WithToken_TunnelCardReflectsContainer()
    {
        var running = SiteStatusEvaluator.BuildServices(Input(Core().Append(E("tuna", health: "")).ToArray(), tunnel: true)).Last();
        var absent = SiteStatusEvaluator.BuildServices(Input(Core(), tunnel: true)).Last();

        running.Level.Should().Be(ServiceLevel.Ok);
        absent.Level.Should().Be(ServiceLevel.Absent);
    }

    [Theory]
    [InlineData(SiteDockerState.Missing, "Docker не найден")]
    [InlineData(SiteDockerState.NotRunning, "Docker не запущен")]
    public void BuildServices_DockerDown_CoreCardsHaveNoData(SiteDockerState docker, string reason)
    {
        var services = SiteStatusEvaluator.BuildServices(Input(null, docker: docker));

        services.Take(4).Should().OnlyContain(s => s.Level == ServiceLevel.Unknown && s.RawText == reason);
    }

    // ---------------------------------------------------------------- общая плашка

    [Fact]
    public void Evaluate_NotInstalled()
    {
        var overview = Evaluate(Input(null, installed: false));

        overview.Pill.Should().Be(SitePill.NotInstalled);
        overview.PillText.Should().Be("Не установлен");
    }

    [Fact]
    public void Evaluate_BrokenInstallFile_IsErrorWithProblemText()
    {
        var overview = Evaluate(Input(null, installed: false, installProblem: "Не удалось прочитать файл установки сайта."));

        overview.Pill.Should().Be(SitePill.Error);
        overview.Explanation.Should().Contain("Не удалось прочитать файл установки сайта.");
    }

    [Fact]
    public void Evaluate_DockerMissing_ShowsDockerDownWithOwnText()
    {
        var overview = Evaluate(Input(null, docker: SiteDockerState.Missing));

        overview.Pill.Should().Be(SitePill.DockerDown);
        overview.PillText.Should().Be("Docker не установлен");
    }

    [Fact]
    public void Evaluate_DockerNotRunning()
    {
        var overview = Evaluate(Input(null, docker: SiteDockerState.NotRunning));

        overview.Pill.Should().Be(SitePill.DockerDown);
        overview.PillText.Should().Be("Docker не запущен");
        overview.Explanation.Should().Contain("Запустить Docker");
    }

    [Fact]
    public void Evaluate_AllHealthy_IsRunning()
    {
        var overview = Evaluate(Input(Core()));

        overview.Pill.Should().Be(SitePill.Running);
        overview.PillText.Should().Be("Работает");
    }

    [Fact]
    public void Evaluate_AllContainersUpButSiteNotAnswering_IsStartingAtFirst()
    {
        Evaluate(Input(Core(), health: false, failures: 1)).Pill.Should().Be(SitePill.Starting);
        Evaluate(Input(Core(), health: null)).Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public void Evaluate_SiteNotAnsweringForTooLong_IsError()
    {
        var overview = Evaluate(Input(Core(), health: false, failures: SiteStatusEvaluator.HealthFailureLimit));

        overview.Pill.Should().Be(SitePill.Error);
        overview.Explanation.Should().Contain("не отвечает");
    }

    [Fact]
    public void Evaluate_SiteNotAnsweringButWeAreStartingIt_StaysStarting()
    {
        Evaluate(Input(Core(), health: false, failures: 99, op: SiteOperation.Starting)).Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public void Evaluate_TunnelFailureDoesNotBreakTheSite_ButIsMentioned()
    {
        var entries = Core().Append(E("tuna", "exited", "", "Exited (1) 1 minute ago", 1)).ToArray();

        var overview = Evaluate(Input(entries, tunnel: true));

        overview.Pill.Should().Be(SitePill.Running);
        overview.Explanation.Should().Contain("адрес в интернете");
    }

    [Fact]
    public void Evaluate_OneServiceStopped_IsPartialErrorNamingIt()
    {
        var entries = new[] { E("db"), E("api", "exited", "", "Exited (0) 1 minute ago"), E("web"), E("proxy") };

        var overview = Evaluate(Input(entries));

        overview.Pill.Should().Be(SitePill.Error);
        overview.Explanation.Should().Contain("«Сервер (API)»");
    }

    [Fact]
    public void Evaluate_OneServiceMissing_IsPartialError()
    {
        var overview = Evaluate(Input(new[] { E("db"), E("api"), E("web") }));

        overview.Pill.Should().Be(SitePill.Error);
        overview.Explanation.Should().Contain("«Прокси»");
    }

    [Fact]
    public void Evaluate_UnhealthyService_IsError()
    {
        var entries = new[] { E("db"), E("api", health: "unhealthy"), E("web"), E("proxy") };

        Evaluate(Input(entries)).Pill.Should().Be(SitePill.Error);
    }

    [Fact]
    public void Evaluate_ServiceStarting_IsStarting()
    {
        var entries = new[] { E("db"), E("api", health: "starting"), E("web", "created", ""), E("proxy", "created", "") };

        Evaluate(Input(entries, health: null)).Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public void Evaluate_ErrorWinsOverStarting()
    {
        var entries = new[] { E("db"), E("api", health: "starting"), E("web", "exited", "", "Exited (1) now", 1), E("proxy") };

        Evaluate(Input(entries)).Pill.Should().Be(SitePill.Error);
    }

    [Theory]
    [InlineData("exited")]
    [InlineData("paused")]
    public void Evaluate_AllStopped_IsStopped(string state)
    {
        var overview = Evaluate(Input(Core(state, ""), health: null));

        overview.Pill.Should().Be(SitePill.Stopped);
        overview.PillText.Should().Be("Остановлен");
    }

    [Fact]
    public void Evaluate_NoContainersAtAll_IsStopped()
    {
        Evaluate(Input(Array.Empty<ComposeServiceEntry>(), health: null)).Pill.Should().Be(SitePill.Stopped);
    }

    [Fact]
    public void Evaluate_NoContainersWhileStarting_IsStarting()
    {
        Evaluate(Input(Array.Empty<ComposeServiceEntry>(), health: null, op: SiteOperation.Starting))
            .Pill.Should().Be(SitePill.Starting);
        Evaluate(Input(new[] { E("db") }, health: null, op: SiteOperation.Restarting)).Pill.Should().Be(SitePill.Starting);
    }

    [Fact]
    public void Evaluate_StoppingOperation_ShowsStopping()
    {
        Evaluate(Input(Core(), op: SiteOperation.Stopping)).Pill.Should().Be(SitePill.Stopping);
    }

    [Fact]
    public void Evaluate_ComposeFailedWhileDockerRuns_IsErrorWithProblemText()
    {
        var overview = Evaluate(Input(null, composeProblem: "Не получилось прочитать настройки."));

        overview.Pill.Should().Be(SitePill.Error);
        overview.Explanation.Should().Be("Не получилось прочитать настройки.");
    }

    [Fact]
    public void Evaluate_NoDataYet_IsChecking()
    {
        Evaluate(Input(null, docker: SiteDockerState.Unknown)).Pill.Should().Be(SitePill.Checking);
    }

    [Fact]
    public void Evaluate_DockerDownBeatsEverythingExceptInstall()
    {
        Evaluate(Input(Core(), docker: SiteDockerState.NotRunning)).Pill.Should().Be(SitePill.DockerDown);
        Evaluate(Input(Core(), docker: SiteDockerState.NotRunning, installed: false)).Pill.Should().Be(SitePill.NotInstalled);
    }
}
