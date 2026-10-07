using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.Install.Stages;

/// <summary>Unpacks the program and the server files from payload.zip.</summary>
public sealed class PrepareStage : StageBase
{
    public PrepareStage() : base(StageId.Prepare, 8) { }

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        reporter.Report(0, "Копирую файлы программы...");

        var stream = ctx.Services.Payload.Open()
            ?? throw new InstallException(FailureKind.PayloadMissing, "payload.zip is not embedded in this build");

        using (stream)
        {
            var result = await Task.Run(() => PayloadExtractor.Extract(
                stream, ctx.Paths.AppDir, ctx.Paths.ServerDir,
                (fraction, name) => reporter.Report(fraction, string.IsNullOrEmpty(name) ? "Копирую файлы программы..." : $"Копирую: {name}")), ct);

            ctx.Log.Write($"Файлов программы: {result.AppFiles.Count}, файлов сайта: {result.ServerFiles.Count}");
        }

        reporter.Report(1, "Файлы программы скопированы");
        return StageOutcome.Done;
    }
}

/// <summary>Chooses the site port and writes the server .env (secrets, admin account, optional keys).</summary>
public sealed class ConfigureStage : StageBase
{
    public ConfigureStage() : base(StageId.Configure, 4) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var paths = ctx.Paths;
        reporter.Report(0.1, "Читаю настройки...");

        var existing = ctx.ExistingEnv?.Text;
        var example = EnvFile.ReadAllTextOrNull(paths.EnvExample);
        if (string.IsNullOrWhiteSpace(existing) && example == null)
            throw new InstallException(FailureKind.PayloadCorrupt, "В архиве нет файла .env.example.");

        int? preferred = existing != null && EnvFile.Get(existing, "HQ_PORT") != null
            ? EnvPlanner.GetPort(existing, PortSelector.First)
            : null;

        // The port of an earlier, still running install is ours even though it looks busy.
        var preferredIsOurs = preferred != null && await ctx.Services.Health.IsHealthyAsync(preferred.Value, ct);

        reporter.Report(0.4, "Ищу свободный адрес для сайта...");
        var port = PortSelector.Select(
            PortSelector.Candidates(preferred),
            p => (preferredIsOurs && p == preferred) || ctx.Services.Ports.IsFree(p));
        if (port == null)
            throw new InstallException(FailureKind.PortBusy, $"Ports {PortSelector.First}-{PortSelector.Last} are busy");

        ctx.Port = port.Value;
        ctx.Log.Write($"Порт сайта: {port}");

        var plan = EnvPlanner.Build(existing, example ?? "", ctx.Answers, port.Value, ctx.ImageTag);
        ctx.Log.Masker.Add(plan.PostgresPassword);
        ctx.Log.Masker.Add(plan.JwtKey);
        ctx.KeptExistingDatabase = plan.KeptExistingSecrets;

        reporter.Report(0.8, "Сохраняю настройки...");
        await Task.Run(() => EnvFile.WriteAtomic(paths.EnvFile, plan.Text), ct);
        if (ctx.UsesEnvBackup)
            ctx.Log.Write("Найдена копия настроек удалённой установки, использую её.");
        ctx.Log.Write(plan.KeptExistingSecrets
            ? "Найдена прежняя установка: секреты базы данных сохранены."
            : "Созданы новые секреты базы данных.");

        reporter.Report(1, "Настройки сохранены");
        return StageOutcome.Done;
    }
}

/// <summary>Start Menu and Desktop shortcuts, the "Apps and features" entry and the app's first-run settings.</summary>
public sealed class ShortcutsStage : StageBase
{
    public ShortcutsStage() : base(StageId.Shortcuts, 4) { }

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var paths = ctx.Paths;
        var services = ctx.Services;

        reporter.Report(0.1, "Создаю ярлык в меню «Пуск»...");
        await Task.Run(() => services.Shortcuts.Create(Spec(paths.StartMenuShortcut, paths)), ct);

        if (ctx.Answers.DesktopShortcut)
        {
            reporter.Report(0.4, "Создаю ярлык на рабочем столе...");
            await Task.Run(() => services.Shortcuts.Create(Spec(paths.DesktopShortcut, paths)), ct);
        }

        reporter.Report(0.7, "Записываю программу в список установленных...");
        services.Registry.WriteUninstallEntry(new UninstallEntry(
            DisplayName: "HQ Studio",
            DisplayVersion: services.Version,
            Publisher: "HQ Studio",
            InstallLocation: paths.AppDir,
            DisplayIcon: paths.AppExe,
            UninstallString: $"\"{paths.AppExe}\" --uninstall"));

        var created = await Task.Run(() => AppConfigWriter.WriteSettingsIfAbsent(paths, ctx.Port), ct);
        ctx.Log.Write(created ? "Созданы настройки программы." : "Настройки программы уже есть, не меняю.");

        reporter.Report(1, "Ярлыки созданы");
        return StageOutcome.Done;
    }

    private static ShortcutSpec Spec(string path, InstallPaths paths) => new(
        Path: path,
        Target: paths.AppExe,
        Arguments: null,
        WorkingDirectory: paths.AppDir,
        IconPath: paths.AppExe,
        Description: "HQ Studio");
}
