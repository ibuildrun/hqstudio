using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateSemVerTests
{
    [Theory]
    [InlineData("1.20.0", "1.19.6", true)]
    [InlineData("v1.20.0", "1.19.6", true)]
    [InlineData("1.2.10", "1.2.9", true)]
    [InlineData("2.0.0", "1.99.99", true)]
    [InlineData("1.19.6", "1.19.6", false)]
    [InlineData("1.19.5", "1.19.6", false)]
    [InlineData("1.19.6.0", "1.19.6", false)]
    [InlineData("1.19.6.1", "1.19.6", true)]
    [InlineData("1.2.3+build5", "1.2.3", false)]
    [InlineData("1.20", "1.19.9", true)]
    public void IsNewer_ComparesNumerically(string candidate, string current, bool expected)
    {
        SemVer.IsNewer(candidate, current).Should().Be(expected);
    }

    [Fact]
    public void Release_IsGreaterThanItsPreRelease()
    {
        SemVer.Parse("1.2.0").Should().BeGreaterThan(SemVer.Parse("1.2.0-rc.1"));
        SemVer.Parse("1.2.0-rc.2").Should().BeGreaterThan(SemVer.Parse("1.2.0-rc.1"));
        SemVer.Parse("1.2.0-rc.10").Should().BeGreaterThan(SemVer.Parse("1.2.0-rc.9"));
        SemVer.Parse("1.2.0-beta").Should().BeGreaterThan(SemVer.Parse("1.2.0-alpha"));
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1")]
    [InlineData("1.x.0")]
    public void TryParse_RejectsGarbage(string? text)
    {
        SemVer.TryParse(text, out _).Should().BeFalse();
    }

    [Fact]
    public void ToString_IsCanonical()
    {
        SemVer.Parse("v1.20").ToString().Should().Be("1.20.0");
        SemVer.Parse("1.19.6.0").ToString().Should().Be("1.19.6");
        SemVer.Parse("1.2.3-rc.1+abc").ToString().Should().Be("1.2.3-rc.1");
    }

    [Theory]
    [InlineData("1.19.6", "1.20.0", true)]
    [InlineData("1.20.0", "1.20.0", false)]
    [InlineData("1.21.0", "1.20.0", false)]
    [InlineData("latest", "1.20.0", true)]
    [InlineData("", "1.20.0", true)]
    [InlineData(null, "1.20.0", true)]
    [InlineData("1.19.6", "garbage", false)]
    [InlineData("1.19.6", null, false)]
    public void IsOutdated_TreatsUnparsableInstalledAsOutdated(string? installed, string? latest, bool expected)
    {
        SemVer.IsOutdated(installed, latest).Should().Be(expected);
    }
}
