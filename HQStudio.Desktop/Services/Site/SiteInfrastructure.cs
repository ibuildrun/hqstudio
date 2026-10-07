using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    /// <summary>Файловые операции за интерфейсом: тесты не трогают настоящие файлы пользователя.</summary>
    public interface ISiteFiles
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string ReadAllText(string path);
        /// <summary>Пишет через временный файл рядом и подменой: при сбое старый файл остаётся целым.</summary>
        void WriteAllTextAtomic(string path, string content);
        void DeleteFile(string path);
        void DeleteDirectory(string path);
        void CopyFile(string source, string destination, bool overwrite);
        void CreateDirectory(string path);
        IReadOnlyList<string> ListFiles(string directory);
    }

    public sealed class SiteFiles : ISiteFiles
    {
        private static readonly UTF8Encoding Utf8NoBom = new(false);

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllTextAtomic(string path, string content)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
            Directory.CreateDirectory(dir);
            var temp = Path.Combine(dir, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp");
            try
            {
                File.WriteAllText(temp, content, Utf8NoBom);
                File.Move(temp, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    try { File.Delete(temp); } catch (IOException) { }
                }
            }
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                // Файлы только для чтения (например, из архива) тоже должны удаляться.
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        public void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            var dir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(source, destination, overwrite);
        }

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public IReadOnlyList<string> ListFiles(string directory) =>
            Directory.Exists(directory) ? Directory.GetFiles(directory) : Array.Empty<string>();
    }

    public sealed record SiteProcessResult(int ExitCode, string Output, string Error)
    {
        /// <summary>Оба потока подряд: для docker важны и stdout, и stderr.</summary>
        public string Combined =>
            string.IsNullOrEmpty(Error) ? Output : string.IsNullOrEmpty(Output) ? Error : Output + Environment.NewLine + Error;
    }

    public interface ISiteProcessRunner
    {
        /// <summary>
        /// Запускает процесс без окна и ждёт завершения. <paramref name="onLine"/> получает каждую строку вывода.
        /// Бросает <see cref="Win32Exception"/>, если программу не удалось запустить.
        /// </summary>
        Task<SiteProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, Action<string>? onLine,
            CancellationToken ct);

        /// <summary>Запускает процесс скрыто и не ждёт его (Docker Desktop, отложенное удаление папок).</summary>
        void StartDetached(string fileName, string arguments, string? workingDirectory);
    }

    public sealed class SiteProcessRunner : ISiteProcessRunner
    {
        private static readonly Regex AnsiCodes = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);

        public async Task<SiteProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments,
            Action<string>? onLine, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var psi = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);
            psi.Environment["COMPOSE_ANSI"] = "never";
            psi.Environment["DOCKER_CLI_HINTS"] = "false";

            using var process = new Process { StartInfo = psi };
            process.Start();
            process.StandardInput.Close();

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var outTask = PumpAsync(process.StandardOutput, stdout, onLine);
            var errTask = PumpAsync(process.StandardError, stderr, onLine);

            using var registration = ct.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
            });

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await Task.WhenAll(outTask, errTask).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            return new SiteProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        }

        public void StartDetached(string fileName, string arguments, string? workingDirectory)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory ?? ""
            };
            using var process = Process.Start(psi);
        }

        // Прогресс docker перерисовывает строку через \r, поэтому концом строки считаются и \r, и \n.
        private static async Task PumpAsync(StreamReader reader, StringBuilder sink, Action<string>? onLine)
        {
            var line = new StringBuilder();
            var buffer = new char[1024];
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    var c = buffer[i];
                    if (c is '\r' or '\n')
                        Flush(line, sink, onLine);
                    else
                        line.Append(c);
                }
            }
            Flush(line, sink, onLine);
        }

        private static void Flush(StringBuilder line, StringBuilder sink, Action<string>? onLine)
        {
            if (line.Length == 0)
                return;
            var text = AnsiCodes.Replace(line.ToString(), "").TrimEnd();
            line.Clear();
            if (text.Length == 0)
                return;
            lock (sink)
            {
                sink.AppendLine(text);
            }
            onLine?.Invoke(text);
        }
    }

    public interface ISiteDockerLocator
    {
        /// <summary>Полный путь к docker.exe или <c>null</c>, если Docker не найден.</summary>
        string? FindDocker();

        /// <summary>Полный путь к «Docker Desktop.exe» или <c>null</c>.</summary>
        string? FindDockerDesktop();
    }

    public sealed class SiteDockerLocator : ISiteDockerLocator
    {
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string?> _pathVariable;
        private readonly string _programFiles;
        private readonly string _localAppData;

        public SiteDockerLocator()
            : this(File.Exists, () => Environment.GetEnvironmentVariable("PATH"),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
        {
        }

        public SiteDockerLocator(Func<string, bool> fileExists, Func<string?> pathVariable, string programFiles,
            string localAppData)
        {
            _fileExists = fileExists;
            _pathVariable = pathVariable;
            _programFiles = programFiles;
            _localAppData = localAppData;
        }

        public string? FindDocker()
        {
            foreach (var dir in (_pathVariable() ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim().Trim('"'), "docker.exe");
                    if (_fileExists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // Некорректный элемент PATH пропускаем.
                }
            }

            return FirstExisting(
                Path.Combine(_programFiles, "Docker", "Docker", "resources", "bin", "docker.exe"),
                Path.Combine(_localAppData, "Programs", "Docker", "Docker", "resources", "bin", "docker.exe"));
        }

        public string? FindDockerDesktop() => FirstExisting(
            Path.Combine(_programFiles, "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(_localAppData, "Programs", "Docker", "Docker", "Docker Desktop.exe"));

        private string? FirstExisting(params string[] candidates) => candidates.FirstOrDefault(_fileExists);
    }

    public interface ISiteHealthProbe
    {
        /// <summary>GET http://127.0.0.1:{port}/api/health отвечает 200.</summary>
        Task<bool> IsHealthyAsync(int port, CancellationToken ct);
    }

    public sealed class HttpSiteHealthProbe : ISiteHealthProbe
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(3) };

        public async Task<bool> IsHealthyAsync(int port, CancellationToken ct)
        {
            try
            {
                using var response = await Client.GetAsync($"http://127.0.0.1:{port}/api/health", ct).ConfigureAwait(false);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                ct.ThrowIfCancellationRequested();
                return false;
            }
        }
    }

    /// <summary>Действия оболочки Windows: открыть адрес в браузере и положить текст в буфер обмена.</summary>
    public interface ISiteShell
    {
        bool OpenUrl(string url);
        bool CopyText(string text);
    }

    public sealed class WindowsSiteShell : ISiteShell
    {
        public bool OpenUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            try
            {
                using var process = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                return false;
            }
        }

        public bool CopyText(string text)
        {
            // Буфер обмена бывает занят другой программой: пробуем несколько раз.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(40);
                }
            }
            return false;
        }
    }
}
