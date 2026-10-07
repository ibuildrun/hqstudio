using System.Reflection;
using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.Install;

public static class VersionInfo
{
    public static string Current
    {
        get
        {
            var attr = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = attr?.InformationalVersion ?? "0.0.0-dev";
            var plus = version.IndexOf('+');
            return plus > 0 ? version[..plus] : version;
        }
    }

    /// <summary>Tag of the site images: the installer version, or "latest" for development builds.</summary>
    public static string ImageTag(string version) =>
        string.IsNullOrWhiteSpace(version) || version.StartsWith("0.0.0", StringComparison.Ordinal) || version.Contains("dev", StringComparison.OrdinalIgnoreCase)
            ? "latest"
            : version;
}

public sealed class InstallContext
{
    public InstallContext(InstallAnswers answers, SetupServices services, SetupLog log)
    {
        Answers = answers;
        Services = services;
        Log = log;

        Log.Masker.AddRange(answers.Secrets());

        // A token typed earlier stays in force when the field is left blank on a re-install.
        ExistingEnv = ExistingEnvReader.Read(Paths);
        var existing = ExistingEnv?.Text;
        TunnelEnabled = !answers.SkipSite
            && !string.IsNullOrWhiteSpace(EnvPlanner.EffectiveTunaToken(existing, answers));
        var domain = EnvPlanner.EffectiveTunaDomain(existing, answers);
        TunaDomain = TunnelEnabled && domain.Length > 0 ? domain : null;
        if (existing != null)
        {
            foreach (var key in new[] { "TUNA_TOKEN", "GEMINI_API_KEY", "POSTGRES_PASSWORD", "JWT_KEY" })
                Log.Masker.Add(EnvFile.Get(existing, key));
        }
    }

    /// <summary>Settings of an earlier install (real .env or the backup), read once when the install starts.</summary>
    public ExistingEnv? ExistingEnv { get; }

    public bool UsesEnvBackup => ExistingEnv?.FromBackup == true;

    /// <summary>Called after the whole install succeeded: the backup has done its job once the site runs from the new .env.</summary>
    public void CompleteSuccessfully()
    {
        if (!UsesEnvBackup || !SiteInstalled)
            return;

        try
        {
            if (File.Exists(Paths.EnvBackup))
                File.Delete(Paths.EnvBackup);
            Log.Write("Копия настроек прежней установки больше не нужна и удалена.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write("Не удалось удалить копию настроек прежней установки: " + ex.Message);
        }
    }

    public InstallAnswers Answers { get; }
    public SetupServices Services { get; }
    public SetupLog Log { get; }
    public InstallPaths Paths => Services.Paths;

    public bool SiteRequested => !Answers.SkipSite;
    public bool TunnelEnabled { get; }

    /// <summary>Own domain of the tunnel (only when the tunnel is on), or null.</summary>
    public string? TunaDomain { get; }

    /// <summary>The address the site is expected at with an own domain; the tunnel log still has the last word.</summary>
    public string? ExpectedPublicUrl => TunaDomain == null ? null : EnvPlanner.PublicUrlFor(TunaDomain);

    public string ImageTag => VersionInfo.ImageTag(Services.Version);

    public int Port { get; set; } = PortSelector.First;
    public string? PublicUrl { get; set; }
    public string? PublicUrlNote { get; set; }
    public bool SiteInstalled { get; set; }

    /// <summary>The database from an earlier install was kept, so the admin password did not change.</summary>
    public bool KeptExistingDatabase { get; set; }
}

public interface IStageReporter
{
    /// <summary>Progress inside the current stage (0..1) and what is happening right now.</summary>
    void Report(double fraction, string? text = null);
}

public sealed record StageOutcome(StageState State, string? Note = null)
{
    public static StageOutcome Done { get; } = new(StageState.Done);
    public static StageOutcome Warning(string note) => new(StageState.Warning, note);
}

public interface IInstallStage
{
    StageId Id { get; }
    string Title { get; }

    /// <summary>Relative share of the overall progress bar.</summary>
    double Weight { get; }

    bool IsApplicable(InstallContext context);

    Task<StageOutcome> RunAsync(InstallContext context, IStageReporter reporter, CancellationToken ct);
}

public abstract class StageBase : IInstallStage
{
    protected StageBase(StageId id, double weight)
    {
        Id = id;
        Weight = weight;
    }

    public StageId Id { get; }
    public string Title => StageNames.Title(Id);
    public double Weight { get; }

    public virtual bool IsApplicable(InstallContext context) => true;

    public abstract Task<StageOutcome> RunAsync(InstallContext context, IStageReporter reporter, CancellationToken ct);
}
