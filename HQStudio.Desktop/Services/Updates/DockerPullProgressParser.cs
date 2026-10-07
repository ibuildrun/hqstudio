using System.Globalization;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Updates
{
    /// <summary>
    /// Derives a coarse, monotonic 0..100 progress from `docker compose pull` output. Understands the
    /// classic layered lines ("abc123def456: Downloading [==>] 1MB/5MB") and the compose plain output
    /// ("abc123def456 Pull complete", "Image name Pulling", "db Pulled", "[+] Pulling 2/5").
    /// </summary>
    public sealed class DockerPullProgressParser
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
            /// <summary>Layer already present locally: it never needs pulling and must not count as progress.</summary>
            public bool Skipped;

            public double Weight => Phase switch
            {
                Phase.Waiting => 0.0,
                Phase.Downloading => 0.5 * Fraction,
                Phase.Downloaded => 0.5,
                Phase.Extracting => 0.5 + 0.45 * Fraction,
                _ => 1.0
            };
        }

        private readonly Dictionary<string, Layer> _layers = new();
        private readonly HashSet<string> _imagesStarted = new();
        private readonly HashSet<string> _imagesPulled = new();
        private int _summaryDone;
        private int _summaryTotal;

        /// <summary>Highest percent reached so far; never decreases when new layers are discovered.</summary>
        public double Percent { get; private set; }

        /// <summary>Layers that have to be downloaded (layers that "Already exists" are not counted).</summary>
        public int LayerCount => _layers.Values.Count(l => !l.Skipped);
        public int CompletedLayers => _layers.Values.Count(l => !l.Skipped && l.Phase == Phase.Done);

        /// <summary>Feeds one output line. Returns true when <see cref="Percent"/> increased.</summary>
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
                    if (image.Groups["verb"].Value == "Pulling")
                        _imagesStarted.Add(name);
                    else
                    {
                        _imagesStarted.Add(name);
                        _imagesPulled.Add(name);
                    }
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
                    layer.Fraction = Math.Max(layer.Fraction, FractionOf(state) ?? 0.5);
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
                    layer.Fraction = Math.Max(layer.Fraction, FractionOf(state) ?? 0.5);
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
            double layerPercent = 0;
            var pulling = _layers.Values.Where(l => !l.Skipped).ToList();
            if (pulling.Count > 0)
                layerPercent = pulling.Sum(l => l.Weight) / pulling.Count * 100.0;

            double imagePercent = 0;
            if (_imagesStarted.Count > 0)
                imagePercent = (double)_imagesPulled.Count / _imagesStarted.Count * 100.0;

            double summaryPercent = 0;
            if (_summaryTotal > 0)
                summaryPercent = (double)_summaryDone / _summaryTotal * 100.0;

            // Layer weights only show what is known so far, so cap below 100 until an image reports "Pulled".
            var candidate = Math.Max(Math.Min(layerPercent, 99.0), Math.Max(imagePercent, summaryPercent));
            candidate = Math.Min(candidate, 100.0);

            if (candidate > Percent + 0.0001)
            {
                Percent = candidate;
                return true;
            }
            return false;
        }

        private static double? FractionOf(string state)
        {
            var m = Sizes.Match(state);
            if (!m.Success)
                return null;

            var cur = ToBytes(m.Groups["cur"].Value, m.Groups["cu"].Value);
            var total = ToBytes(m.Groups["tot"].Value, m.Groups["tu"].Value);
            if (total <= 0)
                return null;
            return Math.Clamp(cur / total, 0.0, 1.0);
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
}
