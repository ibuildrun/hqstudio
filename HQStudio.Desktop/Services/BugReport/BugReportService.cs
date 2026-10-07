using System.Net.Http;

namespace HQStudio.Services.BugReport
{
    public enum BugReportState
    {
        RequestingCode,
        WaitingForUser,
        CreatingIssue,
        Completed
    }

    public sealed record BugReportProgress(BugReportState State, DeviceCodeInfo? DeviceCode = null);

    /// <summary>
    /// Оркестратор автоматической отправки: сохранённый токен или вход через GitHub, затем создание issue.
    /// </summary>
    public sealed class BugReportService
    {
        private static readonly Lazy<HttpClient> SharedHttp = new(() => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        private readonly GitHubDeviceFlowClient _flow;
        private readonly GitHubIssueClient _issues;
        private readonly GitHubTokenStore _tokens;
        private readonly string _clientId;

        public BugReportService(GitHubDeviceFlowClient flow, GitHubIssueClient issues,
            GitHubTokenStore tokens, string clientId)
        {
            _flow = flow;
            _issues = issues;
            _tokens = tokens;
            _clientId = clientId?.Trim() ?? "";
        }

        public static BugReportService CreateDefault() => new(
            new GitHubDeviceFlowClient(SharedHttp.Value),
            new GitHubIssueClient(SharedHttp.Value),
            new GitHubTokenStore(),
            BugReportConfig.ResolveClientId());

        public bool IsAutomaticModeAvailable => _clientId.Length > 0;

        public async Task<CreatedIssue> SubmitAsync(BugReportDraft draft,
            IProgress<BugReportProgress>? progress = null, CancellationToken ct = default)
        {
            if (!IsAutomaticModeAvailable)
                throw new BugReportException(BugReportErrorKind.NotConfigured,
                    "GitHub client id is not configured");

            var stored = _tokens.Load();
            if (stored != null)
            {
                progress?.Report(new BugReportProgress(BugReportState.CreatingIssue));
                try
                {
                    var issue = await _issues.CreateIssueAsync(stored, draft.Title, draft.Body, ct);
                    progress?.Report(new BugReportProgress(BugReportState.Completed));
                    return issue;
                }
                catch (BugReportException ex) when (ex.Kind == BugReportErrorKind.Unauthorized)
                {
                    // Токен отозван или истёк: забываем его и просим войти заново.
                    _tokens.Delete();
                }
            }

            var token = await SignInAsync(progress, ct);
            _tokens.Save(token);

            progress?.Report(new BugReportProgress(BugReportState.CreatingIssue));
            try
            {
                var issue = await _issues.CreateIssueAsync(token, draft.Title, draft.Body, ct);
                progress?.Report(new BugReportProgress(BugReportState.Completed));
                return issue;
            }
            catch (BugReportException ex) when (ex.Kind == BugReportErrorKind.Unauthorized)
            {
                _tokens.Delete();
                throw;
            }
        }

        private async Task<string> SignInAsync(IProgress<BugReportProgress>? progress, CancellationToken ct)
        {
            progress?.Report(new BugReportProgress(BugReportState.RequestingCode));
            var code = await _flow.RequestDeviceCodeAsync(_clientId, ct);

            progress?.Report(new BugReportProgress(BugReportState.WaitingForUser, code));
            return await _flow.PollForTokenAsync(_clientId, code, ct);
        }
    }
}
