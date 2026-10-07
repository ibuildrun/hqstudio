namespace HQStudio.Setup.Core;

public enum SimDockerMode
{
    Running,
    Stopped,
    Missing
}

/// <summary>
/// Command line. Developer flags: --simulate, --simulate-fail=&lt;stage&gt;, --sim-docker=running|stopped|missing,
/// --render-pages &lt;dir&gt;, --payload=&lt;zip&gt;.
/// </summary>
public sealed class SetupOptions
{
    public bool Simulate { get; private set; }
    public StageId? SimulateFail { get; private set; }
    public SimDockerMode SimDocker { get; private set; } = SimDockerMode.Running;
    public string? RenderPagesDir { get; private set; }
    public string? PayloadPath { get; private set; }
    public List<string> Warnings { get; } = new();

    public static SetupOptions Parse(IReadOnlyList<string> args)
    {
        var options = new SetupOptions();

        for (var i = 0; i < args.Count; i++)
        {
            var (name, value) = Split(args[i]);
            switch (name)
            {
                case "--simulate":
                    options.Simulate = true;
                    break;

                case "--simulate-fail":
                    value ??= Next(args, ref i);
                    options.Simulate = true;
                    if (StageNames.TryParse(value, out var stage))
                        options.SimulateFail = stage;
                    else
                        options.Warnings.Add($"Unknown stage for --simulate-fail: '{value}'");
                    break;

                case "--sim-docker":
                    value ??= Next(args, ref i);
                    options.Simulate = true;
                    if (Enum.TryParse<SimDockerMode>(value, ignoreCase: true, out var mode))
                        options.SimDocker = mode;
                    else
                        options.Warnings.Add($"Unknown value for --sim-docker: '{value}'");
                    break;

                case "--render-pages":
                    options.RenderPagesDir = value ?? Next(args, ref i);
                    break;

                case "--payload":
                    options.PayloadPath = value ?? Next(args, ref i);
                    break;
            }
        }

        if (options.RenderPagesDir != null)
            options.Simulate = true;
        return options;
    }

    private static (string Name, string? Value) Split(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq > 0 ? (arg[..eq].ToLowerInvariant(), arg[(eq + 1)..]) : (arg.ToLowerInvariant(), null);
    }

    private static string? Next(IReadOnlyList<string> args, ref int index) =>
        index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal) ? args[++index] : null;
}
