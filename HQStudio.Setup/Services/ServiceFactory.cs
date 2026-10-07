using HQStudio.Setup.Core;
using HQStudio.Setup.Install;
using HQStudio.Setup.Services.Real;
using HQStudio.Setup.Services.Sim;

namespace HQStudio.Setup.Services;

public static class ServiceFactory
{
    public static SetupServices CreateReal(SetupOptions options)
    {
        var paths = InstallPaths.ForCurrentUser();
        var runner = new ProcessRunner();
        return new SetupServices
        {
            Paths = paths,
            Docker = new DockerClient(runner),
            DockerInstaller = new DockerDesktopInstaller(paths.TempDir),
            Health = new HttpHealthProbe(),
            Ports = new TcpPortProbe(),
            Shortcuts = new WScriptShortcutCreator(),
            Registry = new UninstallRegistry(),
            Shell = new ShellActions(),
            Payload = new EmbeddedPayloadSource(options.PayloadPath),
            Delay = new RealDelay(),
            Version = VersionInfo.Current
        };
    }

    /// <summary>Everything fake, every write under a fresh temp folder.</summary>
    public static SetupServices CreateSimulated(SetupOptions options, Action<string> log, string? root = null)
    {
        root ??= Path.Combine(Path.GetTempPath(), "HQStudio-Setup-Sim", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        var paths = InstallPaths.Under(root);
        var world = new SimWorld(options.SimDocker);

        return new SetupServices
        {
            Paths = paths,
            Docker = new SimDockerClient(world, paths),
            DockerInstaller = new SimDockerInstaller(world),
            Health = new SimHealthProbe(),
            Ports = new SimPortProbe(),
            Shortcuts = new SimShortcutCreator(),
            Registry = new SimRegistry(),
            Shell = new SimShellActions(new ShellActions(), log),
            Payload = new SimPayloadSource(),
            Delay = new SimDelay(),
            FailureInjector = options.SimulateFail is { } stage ? new FailOnceInjector(stage) : null,
            Version = VersionInfo.Current,
            IsSimulation = true
        };
    }
}
