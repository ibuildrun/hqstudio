using System.Net.Http;
using System.IO;
using System.Security.Cryptography;

namespace HQStudio.Services.Updates
{
    /// <summary>Streams a release asset to disk with progress and optional SHA-256 verification.</summary>
    public sealed class ReleaseDownloader
    {
        private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(45);

        private readonly HttpClient _http;

        public ReleaseDownloader(HttpMessageHandler handler)
        {
            _http = new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("HQStudio-Updater/1.0");
        }

        /// <summary>
        /// Downloads to <paramref name="destination"/>. The file appears at its final path only after
        /// the digest (when known) has matched; a failed download leaves nothing behind.
        /// </summary>
        public async Task DownloadAsync(ReleaseAsset asset, string destination,
            IProgress<DownloadProgress>? progress, CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var partial = destination + ".part";
            try
            {
                using (var stall = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    stall.CancelAfter(StallTimeout);

                    using var response = await _http
                        .GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, stall.Token)
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                        throw new UpdateException(response.StatusCode == System.Net.HttpStatusCode.NotFound
                            ? $"Файл «{asset.Name}» не найден на GitHub. Возможно, его ещё не загрузили, повторите позже."
                            : $"Не удалось скачать «{asset.Name}» (ошибка {(int)response.StatusCode}).");

                    var total = response.Content.Headers.ContentLength ?? (asset.Size > 0 ? asset.Size : null);
                    await using var source = await response.Content.ReadAsStreamAsync(stall.Token).ConfigureAwait(false);
                    await using var target = new FileStream(partial, FileMode.Create, FileAccess.Write,
                        FileShare.None, 81920, useAsync: true);
                    using var hash = SHA256.Create();

                    var buffer = new byte[81920];
                    long received = 0;
                    int read;
                    while (true)
                    {
                        stall.CancelAfter(StallTimeout);
                        read = await source.ReadAsync(buffer.AsMemory(), stall.Token).ConfigureAwait(false);
                        if (read == 0) break;

                        await target.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                        hash.TransformBlock(buffer, 0, read, null, 0);
                        received += read;
                        progress?.Report(new DownloadProgress(received, total));
                    }

                    hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    await target.FlushAsync(ct).ConfigureAwait(false);

                    if (total.HasValue && received != total.Value)
                        throw new UpdateException("Файл скачался не полностью. Проверьте интернет и повторите попытку.");

                    if (asset.Sha256 != null)
                    {
                        var actual = Convert.ToHexString(hash.Hash!).ToLowerInvariant();
                        if (!string.Equals(actual, asset.Sha256, StringComparison.OrdinalIgnoreCase))
                            throw new UpdateException(
                                "Скачанный файл повреждён (контрольная сумма не совпала). Установленная версия не затронута, повторите попытку.");
                    }
                }

                File.Move(partial, destination, overwrite: true);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new UpdateException("Скачивание остановилось: сервер перестал отвечать. Повторите попытку.");
            }
            catch (Exception ex) when (ex is HttpRequestException or HttpIOException)
            {
                throw new UpdateException("Не удалось скачать файл. Проверьте подключение к интернету.", ex);
            }
            catch (IOException ex)
            {
                throw new UpdateException("Не удалось сохранить файл на диск. Проверьте свободное место.", ex);
            }
            finally
            {
                TryDelete(partial);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
