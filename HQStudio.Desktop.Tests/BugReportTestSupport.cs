using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using HQStudio.Services.BugReport;

namespace HQStudio.Desktop.Tests;

internal sealed record RecordedRequest(
    HttpMethod Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string? Body)
{
    public string Header(string name) =>
        Headers.TryGetValue(name, out var value) ? value : "";

    public Dictionary<string, string> Form()
    {
        var result = new Dictionary<string, string>();
        foreach (var pair in (Body ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            result[Uri.UnescapeDataString(parts[0].Replace('+', ' '))] =
                parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "";
        }
        return result;
    }
}

/// <summary>HttpMessageHandler, отвечающий по очереди заготовленными ответами. В сеть не ходит.</summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();

    public List<RecordedRequest> Requests { get; } = new();

    public FakeHttpHandler Enqueue(HttpStatusCode status, string json, Action<HttpResponseMessage>? configure = null)
    {
        _responders.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            configure?.Invoke(response);
            return response;
        });
        return this;
    }

    public FakeHttpHandler EnqueueOk(string json) => Enqueue(HttpStatusCode.OK, json);

    public FakeHttpHandler EnqueueThrow(Exception exception)
    {
        _responders.Enqueue(_ => throw exception);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
            headers[header.Key] = string.Join(",", header.Value);
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
                headers[header.Key] = string.Join(",", header.Value);
        }

        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.ToString(), headers, body));

        if (_responders.Count == 0)
            throw new InvalidOperationException("No fake response queued for " + request.RequestUri);

        return _responders.Dequeue()(request);
    }
}

internal static class BugReportJson
{
    public const string DeviceCode =
        "{\"device_code\":\"dev-123\",\"user_code\":\"ABCD-1234\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":900,\"interval\":5}";

    public static string Token(string token) =>
        "{\"access_token\":\"" + token + "\",\"token_type\":\"bearer\",\"scope\":\"public_repo\"}";

    public static string Error(string error, int? interval = null) =>
        interval == null
            ? "{\"error\":\"" + error + "\"}"
            : "{\"error\":\"" + error + "\",\"interval\":" + interval + "}";

    public const string IssueCreated =
        "{\"number\":42,\"html_url\":\"https://github.com/ibuildrun/hqstudio/issues/42\"}";
}

/// <summary>Записывает прогресс синхронно (Progress&lt;T&gt; доставляет события асинхронно).</summary>
internal sealed class RecordingProgress : IProgress<BugReportProgress>
{
    public List<BugReportProgress> Items { get; } = new();

    public IEnumerable<BugReportState> States => Items.Select(i => i.State);

    public void Report(BugReportProgress value) => Items.Add(value);
}

internal sealed class FakeFileReader : IFileReader
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => Files.ContainsKey(path);

    public bool DirectoryExists(string path) => Directories.Contains(path);

    public string ReadAllText(string path) => Files[path];

    public IReadOnlyList<string> ReadLastLines(string path, int count)
    {
        var lines = Files[path].Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines.Count <= count ? lines : lines.GetRange(lines.Count - count, count);
    }
}

internal sealed class FakeProcessRunner : IProcessRunner
{
    public List<(string FileName, string Arguments, string WorkingDirectory)> Calls { get; } = new();

    public Func<ProcessResult> Result { get; set; } = () => new ProcessResult(0, "", "", false);

    public Task<ProcessResult> RunAsync(string fileName, string arguments, string workingDirectory,
        TimeSpan timeout, CancellationToken ct)
    {
        Calls.Add((fileName, arguments, workingDirectory));
        return Task.FromResult(Result());
    }
}

internal sealed class FakeShell : IShellActions
{
    public List<string> OpenedUrls { get; } = new();
    public List<string> Clipboard { get; } = new();
    public bool ClipboardWorks { get; set; } = true;
    public Exception? OpenFailure { get; set; }

    public void OpenUrl(string url)
    {
        if (OpenFailure != null) throw OpenFailure;
        OpenedUrls.Add(url);
    }

    public bool CopyToClipboard(string text)
    {
        if (!ClipboardWorks) return false;
        Clipboard.Add(text);
        return true;
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "hqstudio-bugreport-tests-" + Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { }
    }
}
