using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using HQStudio.Setup.Core;
using Microsoft.Win32;

namespace HQStudio.Setup.Services.Real;

public sealed class HttpHealthProbe : IHealthProbe
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<bool> IsHealthyAsync(int port, CancellationToken ct)
    {
        try
        {
            using var response = await Client.GetAsync($"http://127.0.0.1:{port}/api/health", ct).ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            ct.ThrowIfCancellationRequested();
            return false;
        }
    }
}

public sealed class TcpPortProbe : IPortProbe
{
    public bool IsFree(int port)
    {
        try
        {
            if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port == port))
                return false;

            // Also catches ports reserved by Windows (Hyper-V and WSL exclusion ranges).
            var listener = new TcpListener(IPAddress.Loopback, port) { ExclusiveAddressUse = true };
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}

public sealed class RealDelay : IDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
}

public sealed class ShellActions : IShellActions
{
    public void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
    }

    public void Launch(string path, string? arguments = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                Arguments = arguments ?? "",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
    }

    public void RebootNow()
    {
        try
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 5")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
    }
}

public sealed class EmbeddedPayloadSource : IPayloadSource
{
    public const string ResourceName = "payload.zip";

    private readonly string? _filePath;

    public EmbeddedPayloadSource(string? overridePath = null) => _filePath = overridePath;

    public string Description => _filePath ?? "embedded " + ResourceName;

    public Stream? Open()
    {
        if (_filePath != null)
            return File.Exists(_filePath) ? File.OpenRead(_filePath) : null;
        return Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
    }
}

public sealed class UninstallRegistry : IRegistry
{
    public const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\HQStudio";

    public void WriteUninstallEntry(UninstallEntry entry)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
            ?? throw new InvalidOperationException("Не удалось создать запись об удалении программы.");
        key.SetValue("DisplayName", entry.DisplayName);
        key.SetValue("DisplayVersion", entry.DisplayVersion);
        key.SetValue("Publisher", entry.Publisher);
        key.SetValue("InstallLocation", entry.InstallLocation);
        key.SetValue("DisplayIcon", entry.DisplayIcon);
        key.SetValue("UninstallString", entry.UninstallString);
        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        key.SetValue("URLInfoAbout", "https://github.com/" + IssueReport.Repository);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}

/// <summary>Creates .lnk files through WScript.Shell (late bound, no interop assembly needed).</summary>
public sealed class WScriptShortcutCreator : IShortcutCreator
{
    public void Create(ShortcutSpec spec)
    {
        var type = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows не позволяет создать ярлык (WScript.Shell недоступен).");

        Directory.CreateDirectory(Path.GetDirectoryName(spec.Path)!);

        dynamic shell = Activator.CreateInstance(type)!;
        dynamic? link = null;
        try
        {
            link = shell.CreateShortcut(spec.Path);
            link.TargetPath = spec.Target;
            link.Arguments = spec.Arguments ?? "";
            link.WorkingDirectory = spec.WorkingDirectory ?? Path.GetDirectoryName(spec.Target) ?? "";
            link.IconLocation = (spec.IconPath ?? spec.Target) + ",0";
            link.Description = spec.Description ?? "";
            link.Save();
        }
        finally
        {
            if (link != null) Marshal.FinalReleaseComObject(link);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}

public sealed class DockerDesktopInstaller : IDockerInstaller
{
    public const string DownloadUrl = "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe";
    public const string InstallArguments = "install --quiet --accept-license --backend=wsl-2";

    private readonly string _tempDir;

    public DockerDesktopInstaller(string tempDir) => _tempDir = tempDir;

    public async Task<string> DownloadAsync(IProgress<DownloadProgress> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(_tempDir);
        var target = Path.Combine(_tempDir, "Docker Desktop Installer.exe");
        var part = target + ".part";

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HQStudio-Setup");

        try
        {
            using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;

            await using (var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long received = 0;
                var lastReport = Environment.TickCount64;
                int read;
                while ((read = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    received += read;
                    if (Environment.TickCount64 - lastReport >= 150)
                    {
                        progress.Report(new DownloadProgress(received, total));
                        lastReport = Environment.TickCount64;
                    }
                }
                progress.Report(new DownloadProgress(received, total ?? received));
            }

            File.Move(part, target, overwrite: true);
            return target;
        }
        finally
        {
            try { if (File.Exists(part)) File.Delete(part); } catch (IOException) { }
        }
    }

    public async Task<DockerInstallResult> RunAsync(string installerPath, CancellationToken ct)
    {
        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(installerPath, InstallArguments)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new DockerInstallResult(DockerInstallOutcome.Declined, 1223, "Разрешение Windows не было дано.");
        }

        if (process == null)
            return new DockerInstallResult(DockerInstallOutcome.Failed, -1, "Установщик Docker не запустился.");

        using (process)
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            var code = process.ExitCode;
            return code switch
            {
                0 => new DockerInstallResult(DockerInstallOutcome.Success, 0, "Docker установлен."),
                3010 or 1641 => new DockerInstallResult(DockerInstallOutcome.RebootRequired, code, "Нужна перезагрузка."),
                _ => new DockerInstallResult(DockerInstallOutcome.Failed, code, $"Установщик Docker завершился с кодом {code}.")
            };
        }
    }
}
