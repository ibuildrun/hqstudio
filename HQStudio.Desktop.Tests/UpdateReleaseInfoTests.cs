using System.IO;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateReleaseInfoTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Parse_ReadsTagNotesAssetsAndDigest()
    {
        var json = UpdateTestData.ReleaseJson("v1.20.0", serverDigest: "sha256:" + Hex);

        var release = ReleaseInfo.Parse(json);

        release.Tag.Should().Be("v1.20.0");
        release.Version.Should().Be("1.20.0");
        release.Notes.Should().Contain("one");
        release.PublishedAt.Should().NotBeNull();
        release.HtmlUrl.Should().Contain("releases/tag/v1.20.0");
        release.Assets.Should().HaveCount(3);
        release.ServerAsset!.Sha256.Should().Be(Hex);
        release.ServerAsset.DownloadUrl.Should().Be("https://download.test/server.zip");
        release.ServerAsset.Size.Should().Be(500);
        release.DesktopAsset!.Sha256.Should().BeNull("no digest was published for the desktop asset");
    }

    [Theory]
    [InlineData("sha256:ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789", "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")]
    [InlineData("sha512:" + Hex, null)]
    [InlineData("sha256:xyz", null)]
    [InlineData("", null)]
    public void ParseDigest_AcceptsOnlyWellFormedSha256(string digest, string? expected)
    {
        var json = UpdateTestData.ReleaseJson("v1.0.0", serverDigest: digest.Length == 0 ? null : digest);

        ReleaseInfo.Parse(json).ServerAsset!.Sha256.Should().Be(expected);
    }

    [Fact]
    public void Assets_PreferExactNameForVersion()
    {
        var release = new ReleaseInfo
        {
            Tag = "v1.20.0",
            Version = "1.20.0",
            Assets = new[]
            {
                new ReleaseAsset("HQStudio-Desktop-v1.19.0.zip", "https://x/old", 1, null),
                new ReleaseAsset("HQStudio-Desktop-v1.20.0.zip", "https://x/new", 1, null)
            }
        };

        release.DesktopAsset!.DownloadUrl.Should().Be("https://x/new");
        release.ServerAsset.Should().BeNull();
    }

    [Fact]
    public void Parse_RejectsUnparsableTag()
    {
        var act = () => ReleaseInfo.Parse("{\"tag_name\":\"nightly\",\"assets\":[]}");
        act.Should().Throw<UpdateException>().WithMessage("*версии*");
    }

    [Fact]
    public void Parse_RejectsBrokenJson()
    {
        var act = () => ReleaseInfo.Parse("<html>rate limited</html>");
        act.Should().Throw<UpdateException>();
    }

    [Fact]
    public void Parse_SkipsAssetsWithoutUrl()
    {
        var release = ReleaseInfo.Parse("{\"tag_name\":\"v1.0.0\",\"assets\":[{\"name\":\"a.zip\"},{\"name\":\"b.zip\",\"browser_download_url\":\"https://x/b\"}]}");
        release.Assets.Should().ContainSingle(a => a.Name == "b.zip");
    }

    [Fact]
    public void NotesFormatter_StripsMarkdown()
    {
        var text = ReleaseNotesFormatter.ToPlainText("## Features\n\n* **new** thing ([#12](https://x/12))\n* `code` item\n\n\n\nEnd");

        text.Should().NotContain("##").And.NotContain("**").And.NotContain("`").And.NotContain("https://");
        text.Should().Contain("- new thing (#12)").And.Contain("- code item");
    }

    [Fact]
    public async Task Client_RequestsLatestReleaseOfRepo_WithUserAgent()
    {
        var handler = new UpdateFakeHttp { Respond = _ => UpdateFakeHttp.Text(UpdateTestData.ReleaseJson()) };
        var client = new GitHubReleaseClient(handler, "ibuildrun/hqstudio");

        var release = await client.GetLatestAsync();

        release.Version.Should().Be("1.20.0");
        handler.Requests.Should().ContainSingle().Which.Should()
            .Be("https://api.github.com/repos/ibuildrun/hqstudio/releases/latest");
        handler.Messages[0].Headers.UserAgent.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "*нет опубликованных*")]
    [InlineData(HttpStatusCode.Forbidden, "*ограничил*")]
    [InlineData(HttpStatusCode.TooManyRequests, "*ограничил*")]
    [InlineData(HttpStatusCode.InternalServerError, "*500*")]
    public async Task Client_MapsHttpErrorsToRussianMessages(HttpStatusCode code, string messagePattern)
    {
        var handler = new UpdateFakeHttp { Respond = _ => new HttpResponseMessage(code) };

        var act = async () => await new GitHubReleaseClient(handler).GetLatestAsync();

        await act.Should().ThrowAsync<UpdateException>().WithMessage(messagePattern);
    }

    [Fact]
    public async Task Client_MapsNetworkFailure()
    {
        var handler = new UpdateFakeHttp { Respond = _ => throw new HttpRequestException("dns") };

        var act = async () => await new GitHubReleaseClient(handler).GetLatestAsync();

        await act.Should().ThrowAsync<UpdateException>().WithMessage("*подключение к интернету*");
    }

    [Fact]
    public async Task Downloader_ReportsProgress_AndVerifiesDigest()
    {
        using var temp = new UpdateTempDir();
        var data = new byte[300_000];
        new Random(1).NextBytes(data);
        var handler = new UpdateFakeHttp { Respond = _ => UpdateFakeHttp.Bytes(data) };
        var asset = new ReleaseAsset("a.zip", "https://download.test/a.zip", data.Length, UpdateTestData.Sha256(data));
        var reports = new List<DownloadProgress>();
        var dest = temp.Combine("out", "a.zip");

        await new ReleaseDownloader(handler).DownloadAsync(asset, dest, new SyncProgress<DownloadProgress>(reports.Add), default);

        File.ReadAllBytes(dest).Should().Equal(data);
        reports.Should().NotBeEmpty();
        reports.Select(r => r.BytesReceived).Should().BeInAscendingOrder();
        reports.Last().BytesReceived.Should().Be(data.Length);
        reports.Last().TotalBytes.Should().Be(data.Length);
        File.Exists(dest + ".part").Should().BeFalse();
    }

    [Fact]
    public async Task Downloader_WithoutDigest_SkipsVerification()
    {
        using var temp = new UpdateTempDir();
        var handler = new UpdateFakeHttp { Respond = _ => UpdateFakeHttp.Bytes(new byte[] { 1, 2, 3 }) };
        var asset = new ReleaseAsset("a.zip", "https://download.test/a.zip", 3, null);

        await new ReleaseDownloader(handler).DownloadAsync(asset, temp.Combine("a.zip"), null, default);

        File.ReadAllBytes(temp.Combine("a.zip")).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Downloader_DigestMismatch_LeavesNoFile()
    {
        using var temp = new UpdateTempDir();
        var handler = new UpdateFakeHttp { Respond = _ => UpdateFakeHttp.Bytes(new byte[] { 1, 2, 3 }) };
        var asset = new ReleaseAsset("a.zip", "https://download.test/a.zip", 3, Hex);

        var act = async () => await new ReleaseDownloader(handler).DownloadAsync(asset, temp.Combine("a.zip"), null, default);

        await act.Should().ThrowAsync<UpdateException>().WithMessage("*повреждён*");
        Directory.GetFiles(temp.Path).Should().BeEmpty();
    }

    [Fact]
    public async Task Downloader_NotFound_GivesRussianMessage()
    {
        using var temp = new UpdateTempDir();
        var handler = new UpdateFakeHttp { Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound) };
        var asset = new ReleaseAsset("a.zip", "https://download.test/a.zip", 3, null);

        var act = async () => await new ReleaseDownloader(handler).DownloadAsync(asset, temp.Combine("a.zip"), null, default);

        await act.Should().ThrowAsync<UpdateException>().WithMessage("*не найден*");
    }
}
