using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HQStudio.Services.BugReport
{
    public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);

    public interface IProcessRunner
    {
        Task<ProcessResult> RunAsync(string fileName, string arguments, string workingDirectory,
            TimeSpan timeout, CancellationToken ct);
    }

    public interface IFileReader
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string ReadAllText(string path);
        IReadOnlyList<string> ReadLastLines(string path, int count);
    }

    public sealed class SystemProcessRunner : IProcessRunner
    {
        public async Task<ProcessResult> RunAsync(string fileName, string arguments, string workingDirectory,
            TimeSpan timeout, CancellationToken ct)
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                return new ProcessResult(-1, "", ex.Message, false);
            }

            var stdOut = process.StandardOutput.ReadToEndAsync();
            var stdErr = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                if (ct.IsCancellationRequested) throw;
                return new ProcessResult(-1, "", "timed out", true);
            }

            return new ProcessResult(process.ExitCode, await stdOut, await stdErr, false);
        }
    }

    public sealed class DiskFileReader : IFileReader
    {
        // Хвост читаем с запасом: строки лога бывают длинными (стек-трейсы).
        private const int TailBytes = 512 * 1024;

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string ReadAllText(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public IReadOnlyList<string> ReadLastLines(string path, int count)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var truncatedHead = stream.Length > TailBytes;
            if (truncatedHead)
                stream.Seek(-TailBytes, SeekOrigin.End);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var text = reader.ReadToEnd();
            var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            // Первая строка после перемотки обрезана посередине.
            if (truncatedHead && lines.Count > 0)
                lines.RemoveAt(0);

            // Хвост файла обычно заканчивается переводом строки: пустой последний элемент не считаем.
            if (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines.Count <= count ? lines : lines.GetRange(lines.Count - count, count);
        }
    }
}
