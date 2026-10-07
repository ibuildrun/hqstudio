using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace HQStudio.Services.Updates
{
    public sealed class GitHubReleaseClient
    {
        public const string DefaultRepo = "ibuildrun/hqstudio";

        private readonly HttpClient _http;
        private readonly string _latestUrl;

        public GitHubReleaseClient(HttpMessageHandler handler, string repo = DefaultRepo,
            string apiBase = "https://api.github.com")
        {
            _http = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HQStudio-Updater", "1.0"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _latestUrl = $"{apiBase.TrimEnd('/')}/repos/{repo.Trim('/')}/releases/latest";
        }

        public async Task<ReleaseInfo> GetLatestAsync(CancellationToken ct = default)
        {
            try
            {
                using var response = await _http.GetAsync(_latestUrl, ct).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new UpdateException("На GitHub пока нет опубликованных версий.");

                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                    throw new UpdateException("GitHub временно ограничил число проверок. Повторите через несколько минут.");

                if (!response.IsSuccessStatusCode)
                    throw new UpdateException($"GitHub вернул ошибку {(int)response.StatusCode}. Попробуйте позже.");

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return ReleaseInfo.Parse(json);
            }
            catch (UpdateException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new UpdateException("GitHub не отвечает. Проверьте интернет и повторите попытку.");
            }
            catch (HttpRequestException ex)
            {
                throw new UpdateException("Не удалось связаться с GitHub. Проверьте подключение к интернету.", ex);
            }
        }
    }
}
