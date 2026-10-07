using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HQStudio.Services.BugReport
{
    public sealed record DeviceCodeInfo(
        string DeviceCode,
        string UserCode,
        string VerificationUri,
        int ExpiresInSeconds,
        int IntervalSeconds);

    /// <summary>
    /// GitHub OAuth Device Flow: пользователь вводит код на github.com, приложение получает токен без секрета.
    /// </summary>
    public sealed class GitHubDeviceFlowClient
    {
        private const string DeviceGrantType = "urn:ietf:params:oauth:grant-type:device_code";
        private const int DefaultIntervalSeconds = 5;
        private const int DefaultExpiresSeconds = 900;
        private const int SlowDownStepSeconds = 5;
        private const int MaxConsecutiveNetworkErrors = 3;

        private readonly HttpClient _http;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;

        public GitHubDeviceFlowClient(HttpClient http, Func<TimeSpan, CancellationToken, Task>? delay = null)
        {
            _http = http;
            _delay = delay ?? Task.Delay;
        }

        public async Task<DeviceCodeInfo> RequestDeviceCodeAsync(string clientId, CancellationToken ct = default)
        {
            using var json = await PostFormAsync(BugReportConfig.DeviceCodeUrl, new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scope"] = BugReportConfig.OAuthScope
            }, ct);

            var root = json.RootElement;
            if (TryGetString(root, "error", out var error))
            {
                throw error switch
                {
                    "device_flow_disabled" or "incorrect_client_credentials" or "unauthorized_client" =>
                        new BugReportException(BugReportErrorKind.NotConfigured,
                            $"GitHub OAuth app is not usable for device flow: {error}"),
                    _ => new BugReportException(BugReportErrorKind.Unexpected,
                        $"GitHub refused the device code request: {error}")
                };
            }

            if (!TryGetString(root, "device_code", out var deviceCode) ||
                !TryGetString(root, "user_code", out var userCode) ||
                !TryGetString(root, "verification_uri", out var verificationUri))
            {
                throw new BugReportException(BugReportErrorKind.Unexpected,
                    "GitHub returned an incomplete device code response");
            }

            return new DeviceCodeInfo(
                deviceCode, userCode, verificationUri,
                GetInt(root, "expires_in", DefaultExpiresSeconds),
                GetInt(root, "interval", DefaultIntervalSeconds));
        }

        /// <summary>Опрашивает GitHub, пока пользователь не подтвердит код. Возвращает access token.</summary>
        public async Task<string> PollForTokenAsync(string clientId, DeviceCodeInfo code, CancellationToken ct = default)
        {
            var interval = Math.Max(code.IntervalSeconds, 1);
            var expiresIn = TimeSpan.FromSeconds(code.ExpiresInSeconds > 0 ? code.ExpiresInSeconds : DefaultExpiresSeconds);
            var waited = TimeSpan.Zero;
            var networkErrors = 0;

            while (true)
            {
                var wait = TimeSpan.FromSeconds(interval);
                await _delay(wait, ct);
                waited += wait;
                ct.ThrowIfCancellationRequested();

                JsonDocument json;
                try
                {
                    json = await PostFormAsync(BugReportConfig.AccessTokenUrl, new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["device_code"] = code.DeviceCode,
                        ["grant_type"] = DeviceGrantType
                    }, ct);
                    networkErrors = 0;
                }
                catch (BugReportException ex) when (ex.Kind == BugReportErrorKind.Network &&
                                                    ++networkErrors < MaxConsecutiveNetworkErrors)
                {
                    if (waited >= expiresIn) throw ExpiredException();
                    continue;
                }

                using (json)
                {
                    var root = json.RootElement;

                    if (TryGetString(root, "access_token", out var token))
                        return token;

                    TryGetString(root, "error", out var error);
                    switch (error)
                    {
                        case "authorization_pending":
                            break;
                        case "slow_down":
                            var suggested = GetInt(root, "interval", 0);
                            interval = suggested > interval ? suggested : interval + SlowDownStepSeconds;
                            break;
                        case "expired_token":
                            throw ExpiredException();
                        case "access_denied":
                            throw new BugReportException(BugReportErrorKind.AuthDenied,
                                "The user denied the GitHub authorization request");
                        case "device_flow_disabled":
                        case "incorrect_client_credentials":
                        case "unauthorized_client":
                            throw new BugReportException(BugReportErrorKind.NotConfigured,
                                $"GitHub OAuth app is not usable for device flow: {error}");
                        default:
                            throw new BugReportException(BugReportErrorKind.Unexpected,
                                $"Unexpected GitHub OAuth response: {error ?? "no access_token and no error"}");
                    }
                }

                if (waited >= expiresIn) throw ExpiredException();
            }
        }

        private static BugReportException ExpiredException() =>
            new(BugReportErrorKind.AuthExpired, "The GitHub device code expired before it was confirmed");

        private async Task<JsonDocument> PostFormAsync(string url, Dictionary<string, string> form, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd(BugReportConfig.UserAgent);

            string text;
            int status;
            try
            {
                using var response = await _http.SendAsync(request, ct);
                status = (int)response.StatusCode;
                text = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                                       (ex is TaskCanceledException && !ct.IsCancellationRequested))
            {
                throw new BugReportException(BugReportErrorKind.Network, "Could not reach github.com", inner: ex);
            }

            try
            {
                return JsonDocument.Parse(text);
            }
            catch (JsonException ex)
            {
                var kind = status >= 500 ? BugReportErrorKind.ServerError : BugReportErrorKind.Unexpected;
                throw new BugReportException(kind, $"GitHub returned a non-JSON response (HTTP {status})", status, ex);
            }
        }

        private static bool TryGetString(JsonElement element, string name, out string value)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var prop) &&
                prop.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(prop.GetString()))
            {
                value = prop.GetString()!;
                return true;
            }

            value = "";
            return false;
        }

        private static int GetInt(JsonElement element, string name, int fallback) =>
            element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(name, out var prop) &&
            prop.ValueKind == JsonValueKind.Number &&
            prop.TryGetInt32(out var value)
                ? value
                : fallback;
    }
}
