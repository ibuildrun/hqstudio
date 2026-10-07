using FluentAssertions;
using HQStudio.Services.BugReport;
using Xunit;

namespace HQStudio.Desktop.Tests;

public class BugReportIssueUrlBuilderTests
{
    private static Dictionary<string, string> Query(string url)
    {
        var query = url[(url.IndexOf('?') + 1)..];
        return query.Split('&').ToDictionary(
            p => p.Split('=', 2)[0],
            p => Uri.UnescapeDataString(p.Split('=', 2)[1]));
    }

    [Fact]
    public void ShortReportIsNotTruncated()
    {
        var result = IssueUrlBuilder.Build("Title", "Body text");

        result.Truncated.Should().BeFalse();
        result.Url.Should().StartWith("https://github.com/ibuildrun/hqstudio/issues/new?title=Title&body=Body%20text");
        result.Url.Should().EndWith("&labels=from-app");
    }

    [Fact]
    public void EncodesSpecialCharactersAndCyrillic()
    {
        const string title = "Ошибка: Orders & Clients #1";
        const string body = "Строка 1\nСтрока 2 & <b>html</b> ?x=1#frag\r\n`code`";

        var result = IssueUrlBuilder.Build(title, body);

        result.Url.Should().NotContain(" ").And.NotContain("\n").And.NotContain("#");
        var query = Query(result.Url);
        query["title"].Should().Be(title);
        query["body"].Should().Be(body);
        query["labels"].Should().Be("from-app");
    }

    [Fact]
    public void LongAsciiBodyIsCappedBelowLimit()
    {
        var body = string.Join("\n", Enumerable.Range(0, 5000).Select(i => "line " + i));

        var result = IssueUrlBuilder.Build("Title", body);

        result.Truncated.Should().BeTrue();
        result.Url.Length.Should().BeLessThan(7000);
        Query(result.Url)["body"].Should().EndWith(IssueUrlBuilder.TruncationNote);
    }

    [Fact]
    public void LongCyrillicBodyIsCappedBelowLimit()
    {
        // Кириллица кодируется 6 символами на букву: худший случай для лимита.
        var body = new string('Ж', 20000);

        var result = IssueUrlBuilder.Build("Заголовок", body);

        result.Truncated.Should().BeTrue();
        result.Url.Length.Should().BeLessThan(7000);
        Query(result.Url)["body"].Should().Contain(IssueUrlBuilder.TruncationNote);
    }

    [Fact]
    public void TruncationKeepsAsMuchAsPossible()
    {
        var body = new string('a', 50000);

        var result = IssueUrlBuilder.Build("T", body);

        result.Url.Length.Should().BeGreaterThan(6900, because: "the budget should be used almost completely");
    }

    [Fact]
    public void TruncationDoesNotSplitSurrogatePairs()
    {
        // Эмодзи занимают две UTF-16 единицы; обрезка посередине роняет Uri.EscapeDataString.
        var body = string.Concat(Enumerable.Repeat("\U0001F600", 5000));

        var act = () => IssueUrlBuilder.Build("T", body);

        var result = act.Should().NotThrow().Which;
        result.Url.Length.Should().BeLessThan(7000);
        Query(result.Url)["body"].Should().NotContain("�");
    }

    [Fact]
    public void TruncationClosesOpenCodeFence()
    {
        var body = "## Diagnostics\n\n```\n" + string.Join("\n", Enumerable.Range(0, 3000).Select(i => "log line " + i)) + "\n```\n";

        var result = IssueUrlBuilder.Build("T", body);

        var decoded = Query(result.Url)["body"];
        var beforeNote = decoded[..decoded.IndexOf(IssueUrlBuilder.TruncationNote, StringComparison.Ordinal)];
        beforeNote.Split('\n').Count(l => l.StartsWith("```")).Should().Be(2, because: "the cut must close the block it opened");
    }

    [Fact]
    public void ExactlyFittingBodyIsNotTruncated()
    {
        var head = "https://github.com/ibuildrun/hqstudio/issues/new?title=T&body=";
        var tail = "&labels=from-app";
        var room = IssueUrlBuilder.MaxUrlLength - 1 - head.Length - tail.Length;

        var fits = IssueUrlBuilder.Build("T", new string('a', room));
        var over = IssueUrlBuilder.Build("T", new string('a', room + 1));

        fits.Truncated.Should().BeFalse();
        fits.Url.Length.Should().Be(IssueUrlBuilder.MaxUrlLength - 1);
        over.Truncated.Should().BeTrue();
        over.Url.Length.Should().BeLessThan(IssueUrlBuilder.MaxUrlLength);
    }

    [Fact]
    public void VeryLongTitleIsClipped()
    {
        var result = IssueUrlBuilder.Build(new string('T', 5000), "body");

        Query(result.Url)["title"].Length.Should().Be(IssueUrlBuilder.MaxTitleLength);
        result.Url.Length.Should().BeLessThan(7000);
    }

    [Fact]
    public void EmptyInputsProduceValidUrl()
    {
        var result = IssueUrlBuilder.Build("", "");

        result.Url.Should().Be("https://github.com/ibuildrun/hqstudio/issues/new?title=&body=&labels=from-app");
        result.Truncated.Should().BeFalse();
    }
}
