using System.Globalization;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Updates
{
    /// <summary>
    /// Semantic version with up to four numeric parts (assembly versions are 1.2.3.0) and optional pre-release.
    /// </summary>
    public readonly struct SemVer : IComparable<SemVer>, IEquatable<SemVer>
    {
        private static readonly Regex Pattern = new(
            @"^\s*[vV]?(?<a>\d+)\.(?<b>\d+)(?:\.(?<c>\d+))?(?:\.(?<d>\d+))?(?:-(?<pre>[0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Revision { get; }
        public string PreRelease { get; }

        private SemVer(int major, int minor, int patch, int revision, string preRelease)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Revision = revision;
            PreRelease = preRelease;
        }

        public static bool TryParse(string? text, out SemVer version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var m = Pattern.Match(text);
            if (!m.Success)
                return false;

            if (!TryInt(m.Groups["a"], out var a) || !TryInt(m.Groups["b"], out var b))
                return false;

            var c = 0;
            var d = 0;
            if (m.Groups["c"].Success && !TryInt(m.Groups["c"], out c)) return false;
            if (m.Groups["d"].Success && !TryInt(m.Groups["d"], out d)) return false;

            version = new SemVer(a, b, c, d, m.Groups["pre"].Success ? m.Groups["pre"].Value : "");
            return true;
        }

        private static bool TryInt(Group g, out int value) =>
            int.TryParse(g.Value, NumberStyles.None, CultureInfo.InvariantCulture, out value);

        public static SemVer Parse(string text) =>
            TryParse(text, out var v) ? v : throw new FormatException($"Not a valid version: '{text}'");

        /// <summary>True when <paramref name="candidate"/> parses and is strictly greater than <paramref name="current"/>.</summary>
        public static bool IsNewer(string? candidate, string? current)
        {
            if (!TryParse(candidate, out var c))
                return false;
            if (!TryParse(current, out var cur))
                return true;
            return c.CompareTo(cur) > 0;
        }

        /// <summary>
        /// Whether an installed version is behind the latest. Unparsable installed values
        /// ("latest", empty, null) count as outdated; an unparsable latest never triggers an update.
        /// </summary>
        public static bool IsOutdated(string? installed, string? latest)
        {
            if (!TryParse(latest, out var l))
                return false;
            if (!TryParse(installed, out var i))
                return true;
            return i.CompareTo(l) < 0;
        }

        public int CompareTo(SemVer other)
        {
            var r = Major.CompareTo(other.Major);
            if (r != 0) return r;
            r = Minor.CompareTo(other.Minor);
            if (r != 0) return r;
            r = Patch.CompareTo(other.Patch);
            if (r != 0) return r;
            r = Revision.CompareTo(other.Revision);
            if (r != 0) return r;
            return ComparePreRelease(PreRelease, other.PreRelease);
        }

        private static int ComparePreRelease(string a, string b)
        {
            // A release without a pre-release tag is greater than any pre-release of the same version.
            if (a.Length == 0 && b.Length == 0) return 0;
            if (a.Length == 0) return 1;
            if (b.Length == 0) return -1;

            var pa = a.Split('.');
            var pb = b.Split('.');
            var n = Math.Min(pa.Length, pb.Length);
            for (var i = 0; i < n; i++)
            {
                var aNum = int.TryParse(pa[i], NumberStyles.None, CultureInfo.InvariantCulture, out var na);
                var bNum = int.TryParse(pb[i], NumberStyles.None, CultureInfo.InvariantCulture, out var nb);
                int r;
                if (aNum && bNum) r = na.CompareTo(nb);
                else if (aNum) r = -1;
                else if (bNum) r = 1;
                else r = string.CompareOrdinal(pa[i], pb[i]);
                if (r != 0) return r;
            }
            return pa.Length.CompareTo(pb.Length);
        }

        public bool Equals(SemVer other) => CompareTo(other) == 0;
        public override bool Equals(object? obj) => obj is SemVer other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Revision, PreRelease);

        public static bool operator ==(SemVer left, SemVer right) => left.Equals(right);
        public static bool operator !=(SemVer left, SemVer right) => !left.Equals(right);
        public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
        public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
        public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
        public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;

        /// <summary>Canonical text: three parts, a fourth only when non-zero, then the pre-release tag.</summary>
        public override string ToString()
        {
            var s = Revision != 0
                ? $"{Major}.{Minor}.{Patch}.{Revision}"
                : $"{Major}.{Minor}.{Patch}";
            return PreRelease.Length > 0 ? $"{s}-{PreRelease}" : s;
        }
    }
}
