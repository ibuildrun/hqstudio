using FluentAssertions;
using HQStudio.Services.Site;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class SiteComposePsParserTests
{
    [Fact]
    public void Parse_OneObjectPerLine_ReturnsAllServices()
    {
        var output =
            "{\"Service\":\"db\",\"State\":\"running\",\"Health\":\"healthy\",\"Status\":\"Up 2 hours (healthy)\"}\n" +
            "{\"Service\":\"api\",\"State\":\"running\",\"Health\":\"starting\",\"Status\":\"Up 5 seconds (health: starting)\"}\r\n" +
            "{\"Service\":\"proxy\",\"State\":\"exited\",\"Health\":\"\",\"Status\":\"Exited (1) 3 minutes ago\",\"ExitCode\":1}\n";

        var result = SiteComposePsParser.Parse(output);

        result.Should().HaveCount(3);
        result[0].Should().Be(new ComposeServiceEntry("db", "running", "healthy", "Up 2 hours (healthy)", null));
        result[1].Health.Should().Be("starting");
        result[2].State.Should().Be("exited");
        result[2].ExitCode.Should().Be(1);
    }

    [Fact]
    public void Parse_JsonArray_ReturnsAllServices()
    {
        var output = "[{\"Service\":\"db\",\"State\":\"running\",\"Health\":\"healthy\",\"Status\":\"Up\"}," +
                     "{\"Service\":\"web\",\"State\":\"created\",\"Health\":\"\",\"Status\":\"Created\"}]";

        var result = SiteComposePsParser.Parse(output);

        result.Select(r => r.Service).Should().Equal("db", "web");
        result[1].State.Should().Be("created");
    }

    [Fact]
    public void Parse_PrettyPrintedArray_IsSupported()
    {
        var output = "[\n  {\n    \"Service\": \"db\",\n    \"State\": \"running\",\n    \"Health\": \"healthy\"\n  }\n]";

        SiteComposePsParser.Parse(output).Should().ContainSingle().Which.Service.Should().Be("db");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    [InlineData(null)]
    public void Parse_EmptyOutput_ReturnsNothing(string? output)
    {
        SiteComposePsParser.Parse(output).Should().BeEmpty();
    }

    [Fact]
    public void Parse_SkipsWarningsAndBrokenLines()
    {
        var output =
            "time=\"2026-10-07T10:00:00+05:00\" level=warning msg=\"the attribute `version` is obsolete\"\n" +
            "{\"Service\":\"db\",\"State\":\"running\",\"Health\":\"\",\"Status\":\"Up\"}\n" +
            "{broken json\n" +
            "{\"Service\":\"api\",\"State\":\"running\"}\n";

        SiteComposePsParser.Parse(output).Select(r => r.Service).Should().Equal("db", "api");
    }

    [Fact]
    public void Parse_PropertyNamesAreCaseInsensitiveAndStatesLowercased()
    {
        var result = SiteComposePsParser.Parse("{\"service\":\"web\",\"STATE\":\"Running\",\"health\":\"Healthy\",\"status\":\"Up\"}");

        result.Should().ContainSingle();
        result[0].State.Should().Be("running");
        result[0].Health.Should().Be("healthy");
    }

    [Fact]
    public void Parse_ObjectWithoutServiceName_IsIgnored()
    {
        SiteComposePsParser.Parse("{\"State\":\"running\"}").Should().BeEmpty();
    }
}

public class SiteComposeCommandTests
{
    private const string Dir = @"C:\hq\server";

    [Fact]
    public void Build_WithoutTunnel_HasNoProfile()
    {
        var args = SiteComposeCommand.Build(Dir, false, "up", "-d", "--remove-orphans");

        args.Should().Equal("compose", "--project-directory", Dir, "-f", @"C:\hq\server\docker-compose.yml",
            "up", "-d", "--remove-orphans");
    }

    [Fact]
    public void Build_WithTunnel_AddsProfileBeforeCommand()
    {
        var args = SiteComposeCommand.Build(Dir, true, "ps", "--all", "--format", "json");

        args.Should().Equal("compose", "--project-directory", Dir, "-f", @"C:\hq\server\docker-compose.yml",
            "--profile", "tunnel", "ps", "--all", "--format", "json");
    }

    [Fact]
    public void Build_PathWithSpaces_StaysSingleArgument()
    {
        var args = SiteComposeCommand.Build(@"C:\Users\Иван Петров\HQ", false, "stop");

        args[2].Should().Be(@"C:\Users\Иван Петров\HQ");
        args[4].Should().Be(@"C:\Users\Иван Петров\HQ\docker-compose.yml");
    }
}
