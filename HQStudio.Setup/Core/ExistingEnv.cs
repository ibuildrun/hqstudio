namespace HQStudio.Setup.Core;

/// <summary>The .env an install starts from: the real one, or the backup of a removed installation.</summary>
public sealed record ExistingEnv(string Text, bool FromBackup);

public static class ExistingEnvReader
{
    /// <summary>
    /// A real .env always wins. The backup (kept so the old database password is not lost) is used only
    /// when the server folder has no .env at all.
    /// </summary>
    public static ExistingEnv? Read(InstallPaths paths)
    {
        var real = EnvFile.ReadAllTextOrNull(paths.EnvFile);
        if (real != null)
            return new ExistingEnv(real, FromBackup: false);

        try
        {
            var backup = EnvFile.ReadAllTextOrNull(paths.EnvBackup);
            return backup == null ? null : new ExistingEnv(backup, FromBackup: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
