using System.IO;
using System.Text;
using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportTokenStoreTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new GitHubTokenStore(_dir.File("github.token"));

        store.Save("gho_roundtrip_token_123");

        store.Load().Should().Be("gho_roundtrip_token_123");
    }

    [Fact]
    public void Save_DoesNotWritePlaintext()
    {
        var path = _dir.File("github.token");
        new GitHubTokenStore(path).Save("gho_plaintext_check_456");

        var raw = File.ReadAllBytes(path);

        Encoding.UTF8.GetString(raw).Should().NotContain("gho_plaintext_check_456");
        Encoding.ASCII.GetString(raw).Should().NotContain("gho_plaintext_check_456");
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        var path = Path.Combine(_dir.Path, "nested", "deeper", "github.token");

        new GitHubTokenStore(path).Save("gho_nested");

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void Save_OverwritesPreviousToken()
    {
        var store = new GitHubTokenStore(_dir.File("github.token"));
        store.Save("gho_first");

        store.Save("gho_second");

        store.Load().Should().Be("gho_second");
    }

    [Fact]
    public void Load_MissingFile_ReturnsNull()
    {
        new GitHubTokenStore(_dir.File("absent.token")).Load().Should().BeNull();
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsNullAndRemovesIt()
    {
        var path = _dir.File("github.token");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        new GitHubTokenStore(path).Load().Should().BeNull();

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Delete_RemovesFileAndIsSafeToRepeat()
    {
        var path = _dir.File("github.token");
        var store = new GitHubTokenStore(path);
        store.Save("gho_to_delete");

        store.Delete();
        store.Delete();

        File.Exists(path).Should().BeFalse();
        store.Load().Should().BeNull();
    }

    [Fact]
    public void DefaultPath_IsInsideLocalAppData()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio", "github.token");

        new GitHubTokenStore().FilePath.Should().Be(expected);
    }
}
