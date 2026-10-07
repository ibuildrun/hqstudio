using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateDockerPullProgressParserTests
{
    // Output of `docker pull` / compose v1 style: "<layer>: <state>".
    private static readonly string[] ClassicOutput =
    {
        "Pulling db (postgres:16-alpine)...",
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

    private static List<double> Run(DockerPullProgressParser parser, IEnumerable<string> lines)
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
    public void ClassicOutput_IsMonotonicAndAdvances()
    {
        var parser = new DockerPullProgressParser();

        var values = Run(parser, ClassicOutput);

        values.Should().BeInAscendingOrder();
        parser.LayerCount.Should().Be(3, "the layer that already exists is not downloaded");
        parser.CompletedLayers.Should().Be(3);
        parser.Percent.Should().BeGreaterThan(95).And.BeLessThanOrEqualTo(100);
        values[0].Should().Be(0);
        values.Distinct().Count().Should().BeGreaterThan(6);
    }

    [Fact]
    public void ClassicOutput_PartialDownload_ReportsPartialProgress()
    {
        var parser = new DockerPullProgressParser();
        Run(parser, ClassicOutput.Take(10));

        parser.Percent.Should().BeGreaterThan(5).And.BeLessThan(60);
    }

    [Fact]
    public void ComposePlainOutput_IsMonotonicAndReaches100()
    {
        var parser = new DockerPullProgressParser();

        var values = Run(parser, ComposePlainOutput);

        values.Should().BeInAscendingOrder();
        values.Last().Should().Be(100);
        values[2].Should().Be(0);
    }

    [Fact]
    public void ComposePlainOutput_ImageLevelProgressCountsPulledImages()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed(" Image a:1 Pulling ");
        parser.Feed(" Image b:1 Pulling ");
        parser.Feed(" Image c:1 Pulling ");
        parser.Feed(" Image a:1 Pulled ");

        parser.Percent.Should().BeApproximately(33.3, 0.5);

        parser.Feed(" Image b:1 Pulled ");
        parser.Feed(" Image c:1 Pulled ");
        parser.Percent.Should().Be(100);
    }

    [Fact]
    public void ComposeSummaryLine_IsUsed()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed("[+] Pulling 2/4").Should().BeTrue();
        parser.Percent.Should().Be(50);
    }

    [Fact]
    public void DiscoveringMoreLayers_NeverLowersProgress()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed("aaaaaaaaaaaa: Pulling fs layer");
        parser.Feed("aaaaaaaaaaaa: Pull complete");
        var before = parser.Percent;

        parser.Feed("bbbbbbbbbbbb: Pulling fs layer");
        parser.Feed("cccccccccccc: Pulling fs layer");

        parser.Percent.Should().BeGreaterThanOrEqualTo(before);
        parser.LayerCount.Should().Be(3);
    }

    [Fact]
    public void ThroughputNoise_AndBlankLines_AreIgnored()
    {
        var parser = new DockerPullProgressParser();

        parser.Feed("").Should().BeFalse();
        parser.Feed("   ").Should().BeFalse();
        parser.Feed(null).Should().BeFalse();
        parser.Feed("Status: Image is up to date for postgres:16-alpine").Should().BeFalse();
        parser.Feed("Digest: sha256:abcdef").Should().BeFalse();
        parser.Percent.Should().Be(0);
    }

    [Fact]
    public void AnsiEscapeCodes_AreStripped()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed("\u001b[2K\u001b[1Aabcdef123456: Pull complete");

        parser.LayerCount.Should().Be(1);
        parser.CompletedLayers.Should().Be(1);
    }

    [Fact]
    public void AlreadyExistsLayers_DoNotCountAsProgress()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed("abcdef123456: Already exists").Should().BeFalse();
        parser.Feed("abcdef123457: Already exists").Should().BeFalse();

        parser.LayerCount.Should().Be(0);
        parser.Percent.Should().Be(0);

        parser.Feed("111111111111: Pulling fs layer");
        parser.Percent.Should().Be(0);
        parser.Feed("111111111111: Pull complete");
        parser.Percent.Should().BeGreaterThan(90);
    }

    [Fact]
    public void LayerStateNeverGoesBackwards()
    {
        var parser = new DockerPullProgressParser();
        parser.Feed("abcdef123456: Pull complete");
        parser.Feed("abcdef123456: Downloading [=>   ] 1MB/10MB");

        parser.CompletedLayers.Should().Be(1);
    }
}
