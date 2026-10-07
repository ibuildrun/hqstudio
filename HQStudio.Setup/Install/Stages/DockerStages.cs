using HQStudio.Setup.Core;
using HQStudio.Setup.Services;

namespace HQStudio.Setup.Install.Stages;

/// <summary>Makes sure the Docker engine answers; starts Docker Desktop when it is installed but stopped.</summary>
public sealed class DockerReadyStage : StageBase
{
    public const int PollSeconds = 5;
    public const int MaxPolls = 48;

    public DockerReadyStage() : base(StageId.DockerReady, 6) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var docker = ctx.Services.Docker;
        reporter.Report(0.05, "Проверяю Docker...");

        var status = await docker.GetStatusAsync(ct);
        if (status == DockerStatus.NotInstalled)
            throw new InstallException(FailureKind.DockerNotInstalled, "docker.exe not found");

        if (status != DockerStatus.Running)
        {
            reporter.Report(0.1, "Запускаю Docker Desktop...");
            ctx.Log.Write("Docker не отвечает, запускаю Docker Desktop.");
            docker.TryStartDesktop();

            for (var i = 1; i <= MaxPolls && status != DockerStatus.Running; i++)
            {
                await ctx.Services.Delay.DelayAsync(TimeSpan.FromSeconds(PollSeconds), ct);
                status = await docker.GetStatusAsync(ct);
                reporter.Report(Math.Min(0.95, 0.1 + 0.85 * i / MaxPolls), $"Жду, пока Docker запустится ({i * PollSeconds} с)...");
            }

            if (status != DockerStatus.Running)
                throw new InstallException(FailureKind.DockerNotRunning, "Docker did not start in time");
        }

        reporter.Report(1, "Docker работает");
        return StageOutcome.Done;
    }
}

/// <summary>`docker compose pull` with a layer and megabyte progress taken from the output.</summary>
public sealed class PullStage : StageBase
{
    public const int Attempts = 3;

    public PullStage() : base(StageId.Pull, 50) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var lastOutput = "";
        var kind = FailureKind.Unknown;

        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var parser = new PullProgressParser();
            var throttle = new LogThrottle(ctx.Log);

            reporter.Report(0, attempt == 1 ? "Начинаю скачивание..." : $"Скачиваю ещё раз (попытка {attempt} из {Attempts})...");

            var result = await ctx.Services.Docker.ComposeAsync(
                ctx.Paths.ServerDir, ctx.TunnelEnabled, new[] { "pull" },
                line =>
                {
                    throttle.Write(line);

                    // stdout and stderr are read on different threads and the parser is not thread-safe
                    string? text = null;
                    double percent = 0;
                    lock (parser)
                    {
                        if (parser.Feed(line))
                        {
                            percent = parser.Percent;
                            text = Describe(parser);
                        }
                    }
                    if (text != null)
                        reporter.Report(percent / 100.0, text);
                },
                ct);

            if (result.Success)
            {
                reporter.Report(1, "Файлы сайта скачаны");
                return StageOutcome.Done;
            }

            lastOutput = result.Output;
            kind = FailureAnalyzer.ClassifyOutput(lastOutput);
            ctx.Log.Write($"Скачивание не удалось (попытка {attempt} из {Attempts}, код {result.ExitCode}).");

            // A stopped Docker, a missing compose module or a full disk will not fix themselves in five seconds.
            if (kind is FailureKind.DockerNotRunning or FailureKind.ComposeMissing or FailureKind.DiskFull)
                break;
            if (attempt < Attempts)
                await ctx.Services.Delay.DelayAsync(TimeSpan.FromSeconds(5), ct);
        }

        throw new InstallException(
            kind == FailureKind.Unknown ? FailureKind.Network : kind,
            "docker compose pull failed",
            lastOutput);
    }

    public static string Describe(PullProgressParser parser)
    {
        var text = parser.LayerCount > 0
            ? $"Скачано частей: {parser.CompletedLayers} из {parser.LayerCount}"
            : "Скачиваю сайт...";
        if (parser.HasByteInfo)
            text += $" · {ByteSize.Format(parser.DownloadedBytes)} из {ByteSize.Format(parser.TotalBytes)}";
        return text;
    }

    /// <summary>Keeps byte-progress lines from flooding the log: at most one such line per second.</summary>
    private sealed class LogThrottle
    {
        private readonly SetupLog _log;
        private long _lastProgressTick;

        public LogThrottle(SetupLog log) => _log = log;

        public void Write(string line)
        {
            var isProgress = line.Contains("Downloading", StringComparison.OrdinalIgnoreCase)
                || line.Contains("Extracting", StringComparison.OrdinalIgnoreCase);
            if (isProgress)
            {
                var now = Environment.TickCount64;
                if (now - _lastProgressTick < 1000)
                    return;
                _lastProgressTick = now;
            }
            _log.Write(line);
        }
    }
}

/// <summary>`docker compose up -d`; when the port is taken at this point, the next free one is tried.</summary>
public sealed class StartStage : StageBase
{
    public StartStage() : base(StageId.Start, 10) { }

    public override bool IsApplicable(InstallContext context) => context.SiteRequested;

    public override async Task<StageOutcome> RunAsync(InstallContext ctx, IStageReporter reporter, CancellationToken ct)
    {
        var tried = new HashSet<int> { ctx.Port };

        while (true)
        {
            reporter.Report(0.3, "Запускаю сайт...");
            CommandResult result;

            // compose waits for the database and the API to become healthy, which can take a couple of minutes
            using (var tickerCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var ticker = TickAsync(reporter, tickerCts.Token);
                try
                {
                    result = await ctx.Services.Docker.ComposeAsync(
                        ctx.Paths.ServerDir, ctx.TunnelEnabled, new[] { "up", "-d", "--remove-orphans" },
                        ctx.Log.Write, ct);
                }
                finally
                {
                    tickerCts.Cancel();
                    await ticker;
                }
            }

            if (result.Success)
            {
                reporter.Report(1, "Сайт запущен");
                return StageOutcome.Done;
            }

            if (FailureAnalyzer.IsPortConflict(result.Output))
            {
                var next = PortSelector.Next(ctx.Port, ctx.Services.Ports.IsFree, tried);
                if (next == null)
                    throw new InstallException(FailureKind.PortBusy, "No free port left in the range", result.Output);

                ctx.Log.Write($"Порт {ctx.Port} занят, пробую {next}.");
                reporter.Report(0.5, $"Адрес {ctx.Port} занят, выбираю другой ({next})...");
                ctx.Port = next.Value;
                tried.Add(next.Value);

                var text = EnvFile.ReadAllTextOrNull(ctx.Paths.EnvFile) ?? "";
                await Task.Run(() => EnvFile.WriteAtomic(ctx.Paths.EnvFile, EnvPlanner.SetPort(text, ctx.Port)), ct);
                continue;
            }

            throw new InstallException(FailureAnalyzer.ClassifyOutput(result.Output), "docker compose up failed", result.Output);
        }
    }

    private static async Task TickAsync(IStageReporter reporter, CancellationToken ct)
    {
        try
        {
            for (var seconds = 5; ; seconds += 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                reporter.Report(Math.Min(0.9, 0.3 + seconds / 300.0), $"Запускаю сайт, это может занять пару минут ({seconds} с)...");
            }
        }
        catch (OperationCanceledException) { }
    }
}
