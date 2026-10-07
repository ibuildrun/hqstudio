using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Updates
{
    public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    public interface IProcessRunner
    {
        /// <summary>
        /// Runs a process to completion, calling <paramref name="onLine"/> for every output line of
        /// both streams. Throws <see cref="Win32Exception"/> when the executable cannot be started.
        /// </summary>
        Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string? workingDirectory,
            Action<string>? onLine, CancellationToken ct);
    }

    public sealed class ProcessRunner : IProcessRunner
    {
        private static readonly Regex AnsiCodes = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);

        public async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments,
            string? workingDirectory, Action<string>? onLine, CancellationToken ct)
        {
            var psi = new ProcessStartInfo(fileName)
            {
                WorkingDirectory = workingDirectory ?? "",
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

            return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        }

        // Progress output uses carriage returns to redraw a line, so both \r and \n end a line.
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
                    {
                        Flush(line, sink, onLine);
                    }
                    else
                    {
                        line.Append(c);
                    }
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

    public static class DockerLocator
    {
        /// <summary>Plain "docker" when it is on PATH, otherwise the default Docker Desktop location.</summary>
        public static string Resolve()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (File.Exists(Path.Combine(dir.Trim().Trim('"'), "docker.exe")))
                        return "docker";
                }
                catch (ArgumentException) { }
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var candidate = Path.Combine(programFiles, "Docker", "Docker", "resources", "bin", "docker.exe");
            return File.Exists(candidate) ? candidate : "docker";
        }
    }
}
