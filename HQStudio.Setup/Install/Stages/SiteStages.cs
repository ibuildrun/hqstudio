using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.Install.Stages;

/// <summary>Waits for /api/health, then drops the plain admin password from .env and records the install.</summary>
public sealed class HealthStage : StageBase
{
    public const int PollSeconds = 3;
    public const int MaxPolls = 80;

    public HealthStage() : base(StageId.Health, 12) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var healthy = false;
        for (var i = 0; i <= MaxPolls; i++)
        {
            if (await ctx.Services.Health.IsHealthyAsync(ctx.Port, ct))
            {
                healthy = true;
                break;
            }
            if (i == MaxPolls)
                break;

            reporter.Report(Math.Min(0.95, (double)i / MaxPolls), $"Жду, пока сайт ответит ({i * PollSeconds} с)...");
            await ctx.Services.Delay.DelayAsync(TimeSpan.FromSeconds(PollSeconds), ct);
        }

        if (!healthy)
        {
            throw new InstallException(
                FailureKind.HealthTimeout,
                $"http://127.0.0.1:{ctx.Port}/api/health did not answer",
                await CollectDiagnosticsAsync(ctx, ct));
        }

        ctx.Log.Write($"Сайт отвечает: {AppConfigWriter.LocalUrl(ctx.Port)}");

        // The admin account exists now; the plain password has no business staying in the file.
        var envPath = ctx.Paths.EnvFile;
        var text = EnvFile.ReadAllTextOrNull(envPath);
        if (text != null)
            await Task.Run(() => EnvFile.WriteAtomic(envPath, EnvPlanner.BlankAdminPassword(text)), ct);

        await Task.Run(() => AppConfigWriter.WriteInstallJson(ctx.Paths, ctx.Port), ct);
        ctx.SiteInstalled = true;

        reporter.Report(1, "Сайт работает");
        return StageOutcome.Done;
    }

    // Container state and the API log tail make a bug report useful.
    private static async Task<string> CollectDiagnosticsAsync(InstallContext ctx, CancellationToken ct)
    {
        try
        {
            var ps = await ctx.Services.Docker.ComposeAsync(ctx.Paths.ServerDir, ctx.TunnelEnabled, new[] { "ps", "-a" }, null, ct);
            var api = await ctx.Services.Docker.ComposeAsync(
                ctx.Paths.ServerDir, ctx.TunnelEnabled, new[] { "logs", "--no-color", "--tail", "30", "api" }, null, ct);
            return ps.Output + Environment.NewLine + api.Output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return "";
        }
    }
}

/// <summary>Reads the Tuna address from the tunnel container log. A missing address is a warning, not a failure.</summary>
public sealed class PublicUrlStage : StageBase
{
    public const int PollSeconds = 3;
    public const int MaxPolls = 20;

    public PublicUrlStage() : base(StageId.PublicUrl, 6) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested && context.TunnelEnabled;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        for (var i = 0; i < MaxPolls; i++)
        {
            reporter.Report((double)i / MaxPolls, "Подключаю публичный адрес...");

            var result = await ctx.Services.Docker.ComposeAsync(
                ctx.Paths.ServerDir, true, new[] { "logs", "--no-color", "tuna" }, null, ct);
            var url = TunnelUrlParser.Parse(result.Output);
            if (url != null)
            {
                ctx.PublicUrl = url;
                var envPath = ctx.Paths.EnvFile;
                var text = EnvFile.ReadAllTextOrNull(envPath);
                await Task.Run(() =>
                {
                    if (text != null)
                        EnvFile.WriteAtomic(envPath, EnvPlanner.SetPublicUrl(text, url));
                    File.WriteAllText(ctx.Paths.PublicUrlFile, url);
                }, ct);

                ctx.Log.Write($"Публичный адрес: {url}");
                reporter.Report(1, "Публичный адрес получен");
                return StageOutcome.Done;
            }

            await ctx.Services.Delay.DelayAsync(TimeSpan.FromSeconds(PollSeconds), ct);
        }

        if (ctx.ExpectedPublicUrl is { } expected)
        {
            // Own domain: Tuna may still be waiting for the domain to be added and verified, so the expected address is used.
            ctx.PublicUrl = expected;
            var envPath = ctx.Paths.EnvFile;
            var text = EnvFile.ReadAllTextOrNull(envPath);
            await Task.Run(() =>
            {
                if (text != null)
                    EnvFile.WriteAtomic(envPath, EnvPlanner.SetPublicUrl(text, expected));
                File.WriteAllText(ctx.Paths.PublicUrlFile, expected);
            }, ct);

            ctx.PublicUrlNote = "Свой домен заработает, когда вы добавите его на my.tuna.am/domains и подтвердите DNS.";
            return StageOutcome.Warning(ctx.PublicUrlNote);
        }

        ctx.PublicUrlNote = "Публичный адрес пока не получен. Проверьте токен Tuna.";
        return StageOutcome.Warning(ctx.PublicUrlNote);
    }
}
