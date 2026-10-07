using System.Globalization;
using System.Text.RegularExpressions;

namespace HQStudio.Setup.Core;

/// <summary>
/// Turns `docker compose pull` output into a monotonic 0..100 percentage plus layer and byte counters.
/// Understands the classic layered lines ("abc123def456: Downloading [==>] 1MB/5MB") and the compose plain
/// output ("abc123def456 Pull complete", "Image name Pulled", "[+] Pulling 2/5").
/// </summary>
public sealed class PullProgressParser
{
    private static readonly Regex AnsiCodes = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);

    private static readonly Regex LayerLine = new(
        @"^\s*(?:[^\w\s]\s*)?(?:Layer\s+)?(?<id>[0-9a-f]{12,64})\s*:?\s+(?<state>[A-Za-z].*?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ImageLine = new(
        @"^\s*(?:[^\w\s]\s*)?(?:Image\s+)?(?<name>[^\s]+)\s+(?<verb>Pulling|Pulled)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SummaryLine = new(
        @"^\s*\[\+\]\s*Pulling\s+(?<done>\d+)/(?<total>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Sizes = new(
        @"(?<cur>\d+(?:\.\d+)?)\s*(?<cu>[kKMGT]?i?B)\s*/\s*(?<tot>\d+(?:\.\d+)?)\s*(?<tu>[kKMGT]?i?B)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private enum Phase { Waiting, Downloading, Downloaded, Extracting, Done }

    private sealed class Layer
    {
        public Phase Phase;
        public double Fraction;
        public double CurrentBytes;
        public double TotalBytes;
        // Already present locally: never downloaded, must not count as progress.
        public bool Skipped;

        public double Weight => Phase switch
        {
            Phase.Waiting => 0.0,
            Phase.Downloading => 0.5 * Fraction,
            Phase.Downloaded => 0.5,
            Phase.Extracting => 0.5 + 0.45 * Fraction,
            _ => 1.0
        };

        public double DownloadedBytes => Phase switch
        {
            Phase.Waiting => 0,
            Phase.Downloading => CurrentBytes,
            _ => TotalBytes > 0 ? TotalBytes : CurrentBytes
        };
    }

    private readonly Dictionary<string, Layer> _layers = new();
    private readonly HashSet<string> _imagesStarted = new();
    private readonly HashSet<string> _imagesPulled = new();
    private int _summaryDone;
    private int _summaryTotal;
    private double _maxDownloaded;
    private double _maxTotal;

    /// <summary>Highest percent reached so far; never decreases when new layers are discovered.</summary>
    public double Percent { get; private set; }

    public int LayerCount => _layers.Values.Count(l => !l.Skipped);
    public int CompletedLayers => _layers.Values.Count(l => !l.Skipped && l.Phase == Phase.Done);

    /// <summary>Bytes downloaded so far (only layers that report sizes); monotonic.</summary>
    public long DownloadedBytes => (long)_maxDownloaded;

    /// <summary>Known download size of the discovered layers; monotonic.</summary>
    public long TotalBytes => (long)_maxTotal;

    public bool HasByteInfo => _maxTotal > 0;

    /// <summary>Feeds one output line. Returns true when anything visible changed.</summary>
    public bool Feed(string? rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
            return false;

        var line = AnsiCodes.Replace(rawLine, "");

        var layer = LayerLine.Match(line);
        if (layer.Success)
        {
            ApplyLayer(layer.Groups["id"].Value, layer.Groups["state"].Value);
        }
        else
        {
            var summary = SummaryLine.Match(line);
            if (summary.Success)
            {
                _summaryDone = int.Parse(summary.Groups["done"].Value, CultureInfo.InvariantCulture);
                _summaryTotal = int.Parse(summary.Groups["total"].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                var image = ImageLine.Match(line);
                if (!image.Success)
                    return false;

                var name = image.Groups["name"].Value;
                _imagesStarted.Add(name);
                if (image.Groups["verb"].Value == "Pulled")
                    _imagesPulled.Add(name);
            }
        }

        return Recalculate();
    }

    private void ApplyLayer(string id, string state)
    {
        if (!_layers.TryGetValue(id, out var layer))
        {
            layer = new Layer();
            _layers[id] = layer;
            layer.Skipped = state.StartsWith("Already exists", StringComparison.OrdinalIgnoreCase);
        }

        if (state.StartsWith("Pulling fs layer", StringComparison.OrdinalIgnoreCase) ||
            state.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase))
        {
            if (layer.Phase == Phase.Waiting) layer.Fraction = 0;
        }
        else if (state.StartsWith("Downloading", StringComparison.OrdinalIgnoreCase))
        {
            if (layer.Phase <= Phase.Downloading)
            {
                layer.Phase = Phase.Downloading;
                if (TryParseSizes(state, out var cur, out var total))
                {
                    layer.CurrentBytes = Math.Max(layer.CurrentBytes, cur);
                    layer.TotalBytes = Math.Max(layer.TotalBytes, total);
                    layer.Fraction = Math.Max(layer.Fraction, Math.Clamp(cur / total, 0.0, 1.0));
                }
                else
                {
                    layer.Fraction = Math.Max(layer.Fraction, 0.5);
                }
            }
        }
        else if (state.StartsWith("Verifying Checksum", StringComparison.OrdinalIgnoreCase) ||
                 state.StartsWith("Download complete", StringComparison.OrdinalIgnoreCase))
        {
            if (layer.Phase <= Phase.Downloaded) layer.Phase = Phase.Downloaded;
        }
        else if (state.StartsWith("Extracting", StringComparison.OrdinalIgnoreCase))
        {
            if (layer.Phase <= Phase.Extracting)
            {
                layer.Phase = Phase.Extracting;
                layer.Fraction = TryParseSizes(state, out var cur, out var total)
                    ? Math.Max(layer.Fraction, Math.Clamp(cur / total, 0.0, 1.0))
                    : Math.Max(layer.Fraction, 0.5);
            }
        }
        else if (state.StartsWith("Pull complete", StringComparison.OrdinalIgnoreCase) ||
                 state.StartsWith("Already exists", StringComparison.OrdinalIgnoreCase))
        {
            layer.Phase = Phase.Done;
        }
    }

    private bool Recalculate()
    {
        var changed = false;
        var pulling = _layers.Values.Where(l => !l.Skipped).ToList();

        double layerPercent = 0;
        if (pulling.Count > 0)
            layerPercent = pulling.Sum(l => l.Weight) / pulling.Count * 100.0;

        double imagePercent = _imagesStarted.Count > 0 ? (double)_imagesPulled.Count / _imagesStarted.Count * 100.0 : 0;
        double summaryPercent = _summaryTotal > 0 ? (double)_summaryDone / _summaryTotal * 100.0 : 0;

        // Layer weights only describe what is known so far, so stay below 100 until an image reports "Pulled".
        var candidate = Math.Min(Math.Max(Math.Min(layerPercent, 99.0), Math.Max(imagePercent, summaryPercent)), 100.0);
        if (candidate > Percent + 0.0001)
        {
            Percent = candidate;
            changed = true;
        }

        var downloaded = pulling.Sum(l => l.DownloadedBytes);
        var total = pulling.Where(l => l.TotalBytes > 0).Sum(l => l.TotalBytes);
        if (downloaded > _maxDownloaded) { _maxDownloaded = downloaded; changed = true; }
        if (total > _maxTotal) { _maxTotal = total; changed = true; }
        if (_maxDownloaded > _maxTotal) _maxTotal = _maxDownloaded;

        return changed;
    }

    private static bool TryParseSizes(string state, out double current, out double total)
    {
        current = total = 0;
        var m = Sizes.Match(state);
        if (!m.Success)
            return false;

        current = ToBytes(m.Groups["cur"].Value, m.Groups["cu"].Value);
        total = ToBytes(m.Groups["tot"].Value, m.Groups["tu"].Value);
        return total > 0;
    }

    private static double ToBytes(string number, string unit)
    {
        var value = double.Parse(number, CultureInfo.InvariantCulture);
        // Docker prints decimal units (kB, MB); binary ones only with an explicit "i".
        var step = unit.Contains('i') ? 1024.0 : 1000.0;
        var power = char.ToUpperInvariant(unit[0]) switch
        {
            'K' => 1,
            'M' => 2,
            'G' => 3,
            'T' => 4,
            _ => 0
        };
        return value * Math.Pow(step, power);
    }
}
