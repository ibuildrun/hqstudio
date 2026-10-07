namespace HQStudio.Setup.Core;

/// <summary>Every location the installer reads or writes; tests and --simulate point it at a temp folder.</summary>
public sealed record InstallPaths(
    string AppDir,
    string ServerDir,
    string DataDir,
    string SettingsDir,
    string StartMenuDir,
    string DesktopDir,
    string TempDir)
{
    public const string AppExeName = "HQStudio.exe";

    public string AppExe => Path.Combine(AppDir, AppExeName);
    public string EnvFile => Path.Combine(ServerDir, ".env");
    public string EnvExample => Path.Combine(ServerDir, ".env.example");
    public string ComposeFile => Path.Combine(ServerDir, "docker-compose.yml");
    public string PublicUrlFile => Path.Combine(ServerDir, "public-url.txt");

    /// <summary>Copy of the server .env left by the app's uninstall when the user keeps the site data.</summary>
    public string EnvBackup => Path.Combine(DataDir, "site-env.backup");
    public string InstallJson => Path.Combine(DataDir, "install.json");
    public string SettingsJson => Path.Combine(SettingsDir, "settings.json");
    public string SetupLog => Path.Combine(DataDir, "setup.log");
    public string StartMenuShortcut => Path.Combine(StartMenuDir, "HQ Studio.lnk");
    public string DesktopShortcut => Path.Combine(DesktopDir, "HQ Studio.lnk");

    public static InstallPaths ForCurrentUser()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new InstallPaths(
            AppDir: Path.Combine(local, "Programs", "HQ Studio"),
            ServerDir: Path.Combine(local, "HQStudio", "server"),
            DataDir: Path.Combine(local, "HQStudio"),
            SettingsDir: Path.Combine(roaming, "HQStudio"),
            StartMenuDir: Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            DesktopDir: Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            TempDir: Path.Combine(Path.GetTempPath(), "HQStudio-Setup"));
    }

    public static InstallPaths Under(string root) => new(
        AppDir: Path.Combine(root, "Programs", "HQ Studio"),
        ServerDir: Path.Combine(root, "HQStudio", "server"),
        DataDir: Path.Combine(root, "HQStudio"),
        SettingsDir: Path.Combine(root, "Roaming", "HQStudio"),
        StartMenuDir: Path.Combine(root, "StartMenu"),
        DesktopDir: Path.Combine(root, "Desktop"),
        TempDir: Path.Combine(root, "Temp"));
}
