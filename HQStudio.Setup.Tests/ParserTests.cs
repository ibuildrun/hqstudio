using FluentAssertions;
using HQStudio.Setup.Core;
using Xunit;

namespace HQStudio.Setup.Tests;

public class PullProgressParserTests
{
    // `docker pull` / compose v1 style: "<layer>: <state>".
    private static readonly string[] ClassicOutput =
    {
        "16-alpine: Pulling from library/postgres",
        "c6a83fedfae6: Already exists",
        "a0d8b6a3e7c1: Pulling fs layer",
        "5f70bf18a086: Pulling fs layer",
        "e2b7c1d4a9f3: Pulling fs layer",
        "a0d8b6a3e7c1: Waiting",
        "5f70bf18a086: Downloading [==>                                                ]  1.2MB/29.1MB",
        "e2b7c1d4a9f3: Downloading [=======>                                           ]  850B/1.2kB",
        "5f70bf18a086: Downloading [=========================>                         ]  14.6MB/29.1MB",
        "5f70bf18a086: Verifying Checksum",
        "5f70bf18a086: Download complete",
        "a0d8b6a3e7c1: Downloading [==================================================>]  3.4MB/3.4MB",
        "a0d8b6a3e7c1: Download complete",
        "5f70bf18a086: Extracting [=====>                                             ]  3.1MB/29.1MB",
        "5f70bf18a086: Extracting [==================================================>]  29.1MB/29.1MB",
        "5f70bf18a086: Pull complete",
        "a0d8b6a3e7c1: Pull complete",
        "e2b7c1d4a9f3: Pull complete",
        "Digest: sha256:5b2ad8d3f1f2d8d3d0b4a1f6a8c1b9e7f4d2a6c8e0b3d5f7a9c1e3b5d7f9a1c3",
        "Status: Downloaded newer image for postgres:16-alpine"
    };

    // `docker compose pull` with non-TTY output: "Image <name> Pulling" and "<layer> <state>".
    private static readonly string[] ComposePlainOutput =
    {
        " Image postgres:16-alpine Pulling ",
        " Image ghcr.io/ibuildrun/hqstudio/api:1.20.0 Pulling ",
        " Image ghcr.io/ibuildrun/hqstudio/web:1.20.0 Pulling ",
        " 3c6d4a1b9e2f Pulling fs layer ",
        " 8d2f1a7c5b3e Pulling fs layer ",
        " 3c6d4a1b9e2f Downloading [=>      ]  2.1MB/29MB",
        " 8d2f1a7c5b3e Downloading [=======>  ]  40MB/80MB",
        " 3c6d4a1b9e2f Pull complete ",
        " Image postgres:16-alpine Pulled ",
        " 8d2f1a7c5b3e Download complete ",
        " 8d2f1a7c5b3e Extracting [======>  ]  20MB/80MB",
        " 8d2f1a7c5b3e Pull complete ",
        " Image ghcr.io/ibuildrun/hqstudio/api:1.20.0 Pulled ",
        " Image ghcr.io/ibuildrun/hqstudio/web:1.20.0 Pulled "
    };

    private static List<double> Run(PullProgressParser parser, IEnumerable<string> lines)
    {
        var values = new List<double>();
        foreach (var line in lines)
        {
            parser.Feed(line);
            values.Add(parser.Percent);
        }
        return values;
    }

    [Fact]
    public void ClassicOutput_IsMonotonicAndReachesTheEnd()
    {
        var parser = new PullProgressParser();

        var values = Run(parser, ClassicOutput);

        values.Should().BeInAscendingOrder();
        values[0].Should().Be(0);
        parser.LayerCount.Should().Be(3, "the layer that already exists is not downloaded");
        parser.CompletedLayers.Should().Be(3);
        parser.Percent.Should().BeGreaterThan(95).And.BeLessThanOrEqualTo(100);
        values.Distinct().Count().Should().BeGreaterThan(6);
    }

    [Fact]
    public void ComposePlainOutput_IsMonotonicAndFinishesAt100()
    {
        var parser = new PullProgressParser();

        var values = Run(parser, ComposePlainOutput);

        values.Should().BeInAscendingOrder();
        parser.Percent.Should().Be(100);
        parser.LayerCount.Should().Be(2);
        parser.CompletedLayers.Should().Be(2);
    }

    [Fact]
    public void Percent_NeverDecreasesWhenNewLayersAppearLate()
    {
        var parser = new PullProgressParser();
        parser.Feed(" aaaaaaaaaaaa Pulling fs layer ");
        parser.Feed(" aaaaaaaaaaaa Pull complete ");
        var before = parser.Percent;

        parser.Feed(" bbbbbbbbbbbb Pulling fs layer ");

        parser.Percent.Should().BeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Bytes_AreParsedFromDownloadingLinesAndStayMonotonic()
    {
        var parser = new PullProgressParser();
        parser.Feed(" 3c6d4a1b9e2f Pulling fs layer ");
        parser.Feed(" 3c6d4a1b9e2f Downloading [=>      ]  10.0MB/40.0MB");
        var firstDownloaded = parser.DownloadedBytes;
        parser.Feed(" 3c6d4a1b9e2f Downloading [====>    ]  25.0MB/40.0MB");

        parser.HasByteInfo.Should().BeTrue();
        parser.TotalBytes.Should().Be(40_000_000);
        firstDownloaded.Should().Be(10_000_000);
        parser.DownloadedBytes.Should().Be(25_000_000);

        parser.Feed(" 3c6d4a1b9e2f Pull complete ");
        parser.DownloadedBytes.Should().Be(40_000_000);
    }

    [Fact]
    public void BinaryUnits_AreUsedWhenDockerPrintsThem()
    {
        var parser = new PullProgressParser();
        parser.Feed(" aaaaaaaaaaaa Downloading [=>]  1.0MiB/2.0MiB");

        parser.TotalBytes.Should().Be(2 * 1024 * 1024);
        parser.DownloadedBytes.Should().Be(1024 * 1024);
    }

    [Fact]
    public void AnsiColoursAndNoiseAreIgnored()
    {
        var parser = new PullProgressParser();

        parser.Feed("\u001b[1m\u001b[36mSome unrelated line\u001b[0m").Should().BeFalse();
        parser.Feed("").Should().BeFalse();
        parser.Feed(null).Should().BeFalse();
        parser.Feed("\u001b[32m 3c6d4a1b9e2f Pull complete \u001b[0m").Should().BeTrue();
        parser.CompletedLayers.Should().Be(1);
    }

    [Fact]
    public void SummaryLine_DrivesProgressWhenNoLayersAreShown()
    {
        var parser = new PullProgressParser();

        parser.Feed("[+] Pulling 2/4");

        parser.Percent.Should().Be(50);
    }

    [Fact]
    public void LayersThatAlreadyExistDoNotCountAsProgress()
    {
        var parser = new PullProgressParser();

        parser.Feed("c6a83fedfae6: Already exists");

        parser.Percent.Should().Be(0);
        parser.LayerCount.Should().Be(0);
    }
}

public class TunnelUrlParserTests
{
    [Fact]
    public void Parse_FindsForwardingAddress()
    {
        const string log = "tuna-1  | Tuna client v0.19\ntuna-1  | Forwarding https://crm.example.ru -> proxy:80\ntuna-1  | Ready";

        TunnelUrlParser.Parse(log).Should().Be("https://crm.example.ru");
    }

    [Fact]
    public void Parse_ReturnsTheNewestAddressAfterARestart()
    {
        const string log = "Forwarding https://old.example.ru -> proxy:80\nrestarting\nForwarding https://new.example.ru -> proxy:80";

        TunnelUrlParser.Parse(log).Should().Be("https://new.example.ru");
    }

    [Fact]
    public void Parse_StripsColourCodes()
    {
        var log = "\u001b[32mForwarding\u001b[0m https://x.example.ru -> proxy:80";

        TunnelUrlParser.Parse(log).Should().Be("https://x.example.ru");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("tuna-1 | connecting...")]
    [InlineData("Forwarding no-scheme.example -> proxy:80")]
    public void Parse_ReturnsNullWhenThereIsNoAddress(string? log)
    {
        TunnelUrlParser.Parse(log).Should().BeNull();
    }
}

public class PortSelectorTests
{
    [Fact]
    public void Select_PicksTheFirstFreePort()
    {
        PortSelector.Select(PortSelector.Candidates(), _ => true).Should().Be(8080);
    }

    [Fact]
    public void Select_SkipsBusyPorts_8080BusyGives8081()
    {
        var busy = new HashSet<int> { 8080 };

        PortSelector.Select(PortSelector.Candidates(), p => !busy.Contains(p)).Should().Be(8081);
    }

    [Fact]
    public void Select_ReturnsNullWhenTheWholeRangeIsBusy()
    {
        PortSelector.Select(PortSelector.Candidates(), _ => false).Should().BeNull();
    }

    [Fact]
    public void Candidates_StartWithThePreviouslyUsedPortWithoutDuplicates()
    {
        PortSelector.Candidates(8085).Should().Equal(8085, 8080, 8081, 8082, 8083, 8084, 8086, 8087, 8088, 8089, 8090);
        PortSelector.Candidates(8080).Should().Equal(Enumerable.Range(8080, 11));
    }

    [Fact]
    public void Next_SkipsTheBusyPortAndAlreadyTriedOnes()
    {
        PortSelector.Next(8080, _ => true, new HashSet<int> { 8080, 8081 }).Should().Be(8082);
        PortSelector.Next(8090, _ => true).Should().Be(8080);
        PortSelector.Next(8080, _ => false).Should().BeNull();
    }
}

public class ByteSizeTests
{
    [Theory]
    [InlineData(0, "0 Б")]
    [InlineData(512, "512 Б")]
    [InlineData(34_500, "35 КБ")]
    [InlineData(34_500_000, "34,5 МБ")]
    [InlineData(1_200_000_000, "1,2 ГБ")]
    public void Format_UsesRussianUnits(long bytes, string expected)
    {
        ByteSize.Format(bytes).Should().Be(expected);
    }
}
