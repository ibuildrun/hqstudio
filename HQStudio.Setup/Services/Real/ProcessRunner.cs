using System.Diagnostics;
using System.Text;

namespace HQStudio.Setup.Services.Real;

public sealed class ProcessRunner : IProcessRunner
{
    private const int MaxOutputChars = 96 * 1024;

    public async Task<ProcessResult> RunAsync(ProcessRequest request, Action<string>? onLine, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(request.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = request.WorkingDirectory ?? ""
        };
        foreach (var arg in request.Arguments)
            psi.ArgumentList.Add(arg);
        if (request.Environment != null)
        {
            foreach (var (key, value) in request.Environment)
                psi.Environment[key] = value;
        }

        var output = new StringBuilder();
        var gate = new object();
        void Append(string? line)
        {
            if (line == null)
                return;
            lock (gate)
            {
                output.AppendLine(line);
                if (output.Length > MaxOutputChars)
                    output.Remove(0, output.Length - MaxOutputChars);
            }
            onLine?.Invoke(line);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => Append(e.Data);
        process.ErrorDataReceived += (_, e) => Append(e.Data);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (request.Timeout is { } timeout)
            timeoutCts.CancelAfter(timeout);

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new ProcessResult(-1, ex.Message);
        }

        try { process.StandardInput.Close(); } catch (IOException) { }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (ct.IsCancellationRequested)
                throw;
            string text;
            lock (gate) text = output.ToString();
            return new ProcessResult(-1, text, TimedOut: true);
        }

        string finalOutput;
        lock (gate) finalOutput = output.ToString();
        return new ProcessResult(process.ExitCode, finalOutput);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }
}
