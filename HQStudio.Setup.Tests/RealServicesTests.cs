using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using HQStudio.Setup.Services;
using HQStudio.Setup.Services.Real;
using Xunit;

namespace HQStudio.Setup.Tests;

/// <summary>Real implementations that only touch loopback or a temp folder.</summary>
public class RealServicesTests
{
    [Fact]
    public void TcpPortProbe_ReportsAListeningPortAsBusyAndAFreedPortAsFree()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var probe = new TcpPortProbe();

        probe.IsFree(port).Should().BeFalse();

        listener.Stop();
        probe.IsFree(port).Should().BeTrue();
    }

    private static (TcpListener Listener, int Port, Task Serve) StartFakeSite(string statusLine)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serve = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[2048];
                _ = await stream.ReadAsync(buffer);
                var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {statusLine}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(response);
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or IOException) { }
        });
        return (listener, port, serve);
    }

    [Fact]
    public async Task HttpHealthProbe_Http200IsHealthy()
    {
        var (listener, port, serve) = StartFakeSite("200 OK");

        var healthy = await new HttpHealthProbe().IsHealthyAsync(port, CancellationToken.None);

        healthy.Should().BeTrue();
        listener.Stop();
        await serve;
    }

    [Fact]
    public async Task HttpHealthProbe_Http503IsNotHealthy()
    {
        var (listener, port, serve) = StartFakeSite("503 Service Unavailable");

        var healthy = await new HttpHealthProbe().IsHealthyAsync(port, CancellationToken.None);

        healthy.Should().BeFalse();
        listener.Stop();
        await serve;
    }

    [Fact]
    public async Task HttpHealthProbe_NothingListeningIsNotHealthy()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        (await new HttpHealthProbe().IsHealthyAsync(port, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public void WScriptShortcutCreator_WritesAWorkingLnkFile()
    {
        using var temp = new TempDir();
        var target = temp.Combine("app", "HQStudio.exe");
        Directory.CreateDirectory(temp.Combine("app"));
        File.WriteAllText(target, "x");
        var lnk = temp.Combine("menu", "HQ Studio.lnk");

        new WScriptShortcutCreator().Create(new ShortcutSpec(lnk, target, "--flag", temp.Combine("app"), target, "HQ Studio"));

        File.Exists(lnk).Should().BeTrue();

        // read it back through the same COM object to prove the fields were stored
        var type = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic link = shell.CreateShortcut(lnk);
        ((string)link.TargetPath).Should().Be(target);
        ((string)link.Arguments).Should().Be("--flag");
        ((string)link.WorkingDirectory).Should().Be(temp.Combine("app"));
        ((string)link.Description).Should().Be("HQ Studio");
    }

    [Fact]
    public void EmbeddedPayloadSource_ReadsAnExternalFileWhenAPathIsGiven()
    {
        using var temp = new TempDir();
        Directory.CreateDirectory(temp.Path);
        var zip = temp.Combine("payload.zip");
        File.WriteAllBytes(zip, FakePayload.StandardZip());

        using var stream = new EmbeddedPayloadSource(zip).Open();

        stream.Should().NotBeNull();
        stream!.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void EmbeddedPayloadSource_ReturnsNullForAMissingFile()
    {
        new EmbeddedPayloadSource(@"C:\definitely\not\here\payload.zip").Open().Should().BeNull();
    }

    [Fact]
    public void ShellActions_RefusesNonHttpUrls()
    {
        // must not throw and must not start anything for schemes other than http and https
        var shell = new ShellActions();

        shell.OpenUrl("file:///C:/Windows/System32/cmd.exe");
        shell.OpenUrl("not a url");
        shell.OpenUrl("ms-settings:");
    }
}
