using HQStudio.Setup.Core;

namespace HQStudio.Setup.Install;

public enum InstallOutcome
{
    Success,
    Failed,
    Cancelled
}

public sealed class StageInfo
{
    public StageInfo(StageId id, string title, double weight)
    {
        Id = id;
        Title = title;
        Weight = weight;
    }

    public StageId Id { get; }
    public string Title { get; }
    public double Weight { get; }
    public StageState State { get; internal set; } = StageState.Pending;

    /// <summary>Current action while running, or the warning/failure text afterwards.</summary>
    public string? Detail { get; internal set; }
}

/// <summary>
/// Runs the install stages in order. A failed or cancelled run can be started again: finished stages are not
/// repeated, the run resumes at the stage that did not finish.
/// </summary>
public sealed class InstallEngine
{
    private readonly InstallContext _context;
    private readonly Dictionary<StageId, IInstallStage> _stages;
    private readonly double _totalWeight;

    public InstallEngine(InstallContext context, IEnumerable<IInstallStage>? stages = null)
    {
        _context = context;
        var applicable = (stages ?? DefaultStages()).Where(s => s.IsApplicable(context)).ToList();
        _stages = applicable.ToDictionary(s => s.Id);
        Stages = applicable.Select(s => new StageInfo(s.Id, s.Title, s.Weight)).ToList();
        _totalWeight = Stages.Sum(s => s.Weight);
    }

    public IReadOnlyList<StageInfo> Stages { get; }
    public FailureInfo? LastFailure { get; private set; }

    public event Action<StageInfo>? StageChanged;

    /// <summary>Overall fraction 0..1 and the text of the current action.</summary>
    public event Action<double, string>? ProgressChanged;

    public static IReadOnlyList<IInstallStage> DefaultStages() => new IInstallStage[]
    {
        new Stages.PrepareStage(),
        new Stages.ConfigureStage(),
        new Stages.DockerReadyStage(),
        new Stages.PullStage(),
        new Stages.StartStage(),
        new Stages.HealthStage(),
        new Stages.PublicUrlStage(),
        new Stages.ShortcutsStage()
    };

    public async Task<InstallOutcome> RunAsync(CancellationToken ct)
    {
        LastFailure = null;

        foreach (var info in Stages)
        {
            if (info.State is StageState.Done or StageState.Warning)
                continue;

            var stage = _stages[info.Id];
            SetState(info, StageState.Running, stage.Title);
            RaiseProgress(info, 0, stage.Title);
            _context.Log.Write($"== {stage.Title}");

            try
            {
                ct.ThrowIfCancellationRequested();
                _context.Services.FailureInjector?.Check(info.Id);

                var reporter = new Reporter(this, info);
                var outcome = await stage.RunAsync(_context, reporter, ct);

                SetState(info, outcome.State, outcome.Note);
                if (outcome.Note != null)
                    _context.Log.Write(outcome.Note);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                SetState(info, StageState.Pending, null);
                _context.Log.Write("Установка отменена пользователем.");
                return InstallOutcome.Cancelled;
            }
            catch (Exception ex)
            {
                var failure = FailureAnalyzer.Analyze(info.Id, ex);
                LastFailure = failure;
                _context.Log.Write($"ОШИБКА на этапе «{stage.Title}»: {failure.Kind}");
                foreach (var line in failure.Technical.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    _context.Log.Write("  " + line.TrimEnd());
                SetState(info, StageState.Failed, failure.Message);
                return InstallOutcome.Failed;
            }
        }

        _context.CompleteSuccessfully();
        ProgressChanged?.Invoke(1.0, "Готово");
        _context.Log.Write("Установка завершена.");
        return InstallOutcome.Success;
    }

    private void SetState(StageInfo info, StageState state, string? detail)
    {
        info.State = state;
        info.Detail = detail;
        StageChanged?.Invoke(info);
    }

    private void RaiseProgress(StageInfo current, double fraction, string? text)
    {
        if (_totalWeight <= 0)
            return;

        var done = Stages.Where(s => s.State is StageState.Done or StageState.Warning).Sum(s => s.Weight);
        var overall = Math.Clamp((done + current.Weight * Math.Clamp(fraction, 0, 1)) / _totalWeight, 0, 1);
        ProgressChanged?.Invoke(overall, text ?? current.Title);
    }

    private sealed class Reporter : IStageReporter
    {
        private readonly InstallEngine _engine;
        private readonly StageInfo _info;

        public Reporter(InstallEngine engine, StageInfo info)
        {
            _engine = engine;
            _info = info;
        }

        public void Report(double fraction, string? text = null)
        {
            if (text != null && _info.State == StageState.Running && text != _info.Detail)
            {
                _info.Detail = text;
                _engine.StageChanged?.Invoke(_info);
            }
            _engine.RaiseProgress(_info, fraction, text);
        }
    }
}
