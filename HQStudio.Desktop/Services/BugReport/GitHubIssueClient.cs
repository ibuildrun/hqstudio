using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HQStudio.Services.BugReport
{
    public sealed record CreatedIssue(int Number, string HtmlUrl);

    public sealed class GitHubIssueClient
    {
        private readonly HttpClient _http;

        public GitHubIssueClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<CreatedIssue> CreateIssueAsync(string token, string title, string body, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(new { title, body });
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportConfig.IssuesApiUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.UserAgent.ParseAdd(BugReportConfig.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", BugReportConfig.ApiVersion);

            int status;
            string text;
            bool rateLimitExhausted;
            try
            {
                using var response = await _http.SendAsync(request, ct);
                status = (int)response.StatusCode;
                text = await response.Content.ReadAsStringAsync(ct);
                rateLimitExhausted = response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
                                     remaining.FirstOrDefault() == "0";
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                                       (ex is TaskCanceledException && !ct.IsCancellationRequested))
            {
                throw new BugReportException(BugReportErrorKind.Network, "Could not reach api.github.com", inner: ex);
            }

            if (status is 200 or 201)
                return ParseCreated(text, status);

            var message = ExtractMessage(text);
            throw status switch
            {
                401 => new BugReportException(BugReportErrorKind.Unauthorized, $"GitHub rejected the token: {message}", status),
                429 => new BugReportException(BugReportErrorKind.RateLimited, $"GitHub rate limit reached: {message}", status),
                403 when rateLimitExhausted =>
                    new BugReportException(BugReportErrorKind.RateLimited, $"GitHub rate limit reached: {message}", status),
                403 => new BugReportException(BugReportErrorKind.Forbidden, $"GitHub denied access: {message}", status),
                404 or 410 or 422 => new BugReportException(BugReportErrorKind.Rejected, $"GitHub rejected the issue: {message}", status),
                >= 500 => new BugReportException(BugReportErrorKind.ServerError, $"GitHub server error (HTTP {status}): {message}", status),
                _ => new BugReportException(BugReportErrorKind.Unexpected, $"Unexpected GitHub response (HTTP {status}): {message}", status)
            };
        }

        private static CreatedIssue ParseCreated(string text, int status)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var url = root.GetProperty("html_url").GetString();
                var number = root.TryGetProperty("number", out var n) && n.TryGetInt32(out var value) ? value : 0;
                if (!string.IsNullOrEmpty(url))
                    return new CreatedIssue(number, url);
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw new BugReportException(BugReportErrorKind.Unexpected,
                    "GitHub created the issue but the response could not be read", status, ex);
            }

            throw new BugReportException(BugReportErrorKind.Unexpected,
                "GitHub created the issue but did not return its URL", status);
        }

        private static string ExtractMessage(string text)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? "";
                }
            }
            catch (JsonException) { }

            return text.Length <= 200 ? text : text[..200];
        }
    }
}
