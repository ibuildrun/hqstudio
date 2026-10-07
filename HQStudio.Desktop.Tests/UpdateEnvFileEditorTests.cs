using System.IO;
using System.Text;
using FluentAssertions;
using HQStudio.Services.Updates;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class UpdateEnvFileEditorTests
{
    private const string Key = EnvFileEditor.VersionKey;

    [Theory]
    [InlineData("HQSTUDIO_VERSION=1.19.6", "1.19.6")]
    [InlineData("  HQSTUDIO_VERSION = 1.19.6  ", "1.19.6")]
    [InlineData("HQSTUDIO_VERSION=\"1.19.6\"", "1.19.6")]
    [InlineData("HQSTUDIO_VERSION='1.19.6'", "1.19.6")]
    [InlineData("export HQSTUDIO_VERSION=1.19.6", "1.19.6")]
    [InlineData("A=1\r\nHQSTUDIO_VERSION=latest\r\nB=2", "latest")]
    public void GetValue_ReadsAssignment(string text, string expected)
    {
        EnvFileEditor.GetValue(text, Key).Should().Be(expected);
    }

    [Fact]
    public void GetValue_IgnoresCommentsAndOtherKeys()
    {
        var text = "# HQSTUDIO_VERSION=old\nHQSTUDIO_VERSION_EXTRA=5\nOTHER=1\n";
        EnvFileEditor.GetValue(text, Key).Should().BeNull();
    }

    [Fact]
    public void SetValue_ReplacesOnlyTheKeyLine_AndKeepsEverythingElse()
    {
        var text = "# settings\r\nA=1\r\n\r\nHQSTUDIO_VERSION=1.19.6\r\n# tail comment\r\nB=two words\r\n";

        var result = EnvFileEditor.SetValue(text, Key, "1.20.0");

        result.Should().Be("# settings\r\nA=1\r\n\r\nHQSTUDIO_VERSION=1.20.0\r\n# tail comment\r\nB=two words\r\n");
    }

    [Fact]
    public void SetValue_KeepsLfLineEndings()
    {
        var result = EnvFileEditor.SetValue("A=1\nHQSTUDIO_VERSION=1\nB=2\n", Key, "2");
        result.Should().Be("A=1\nHQSTUDIO_VERSION=2\nB=2\n");
    }

    [Fact]
    public void SetValue_ReplacesOnlyFirstOccurrence()
    {
        var result = EnvFileEditor.SetValue("HQSTUDIO_VERSION=1\nHQSTUDIO_VERSION=2\n", Key, "3");
        result.Should().Be("HQSTUDIO_VERSION=3\nHQSTUDIO_VERSION=2\n");
    }

    [Fact]
    public void SetValue_IgnoresCommentedKey_AndAppends()
    {
        var result = EnvFileEditor.SetValue("# HQSTUDIO_VERSION=old\nA=1\n", Key, "1.20.0");
        result.Should().Be("# HQSTUDIO_VERSION=old\nA=1\nHQSTUDIO_VERSION=1.20.0\n");
    }

    [Fact]
    public void SetValue_AddsMissingKey_WithoutTrailingNewline()
    {
        EnvFileEditor.SetValue("A=1", Key, "1.20.0").Should().Be("A=1" + Environment.NewLine + "HQSTUDIO_VERSION=1.20.0" + Environment.NewLine);
        EnvFileEditor.SetValue("A=1\r\nB=2", Key, "1.20.0").Should().Be("A=1\r\nB=2\r\nHQSTUDIO_VERSION=1.20.0\r\n");
    }

    [Fact]
    public void SetValue_OnEmptyText_CreatesTheLine()
    {
        EnvFileEditor.SetValue("", Key, "1.20.0").Should().StartWith("HQSTUDIO_VERSION=1.20.0");
    }

    [Fact]
    public void WriteValue_UpdatesFile_PreservesBom_AndLeavesNoTempFiles()
    {
        using var temp = new UpdateTempDir();
        var path = temp.Combine(".env");
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("# Комментарий\r\nHQSTUDIO_VERSION=1.19.6\r\nA=1\r\n")).ToArray();
        File.WriteAllBytes(path, bytes);

        EnvFileEditor.WriteValue(path, Key, "1.20.0");

        var after = File.ReadAllBytes(path);
        after.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Encoding.UTF8.GetString(after, 3, after.Length - 3)
            .Should().Be("# Комментарий\r\nHQSTUDIO_VERSION=1.20.0\r\nA=1\r\n");
        Directory.GetFiles(temp.Path).Select(Path.GetFileName).Should().Equal(".env");
    }

    [Fact]
    public void WriteValue_FailureLeavesOriginalIntact_AndNoTempFiles()
    {
        using var temp = new UpdateTempDir();
        var path = temp.Combine(".env");
        File.WriteAllText(path, "HQSTUDIO_VERSION=1.19.6\n");

        // Exclusive lock makes the replace step fail after the temp file was written.
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var act = () => EnvFileEditor.WriteAtomic(path, "HQSTUDIO_VERSION=1.20.0\n");
            act.Should().Throw<IOException>();
        }

        File.ReadAllText(path).Should().Be("HQSTUDIO_VERSION=1.19.6\n");
        Directory.GetFiles(temp.Path).Select(Path.GetFileName).Should().Equal(".env");
    }

    [Fact]
    public void ReadValue_ReturnsNullWhenFileMissing()
    {
        using var temp = new UpdateTempDir();
        EnvFileEditor.ReadValue(temp.Combine(".env"), Key).Should().BeNull();
    }
}
