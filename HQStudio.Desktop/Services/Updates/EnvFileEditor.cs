using System.IO;
using System.Text;

namespace HQStudio.Services.Updates
{
    /// <summary>Reads and edits single keys of a docker compose .env file without disturbing the rest.</summary>
    public static class EnvFileEditor
    {
        public const string VersionKey = "HQSTUDIO_VERSION";

        private static readonly UTF8Encoding Utf8NoBom = new(false);

        public static string? GetValue(string text, string key)
        {
            foreach (var line in SplitLines(text))
            {
                if (TryMatchKey(line, key, out var value))
                    return value;
            }
            return null;
        }

        /// <summary>
        /// Replaces the first assignment of <paramref name="key"/> or appends it. Other lines, comments,
        /// blank lines and the line-ending style are kept byte-for-byte.
        /// </summary>
        public static string SetValue(string text, string key, string value)
        {
            var newline = text.Contains("\r\n") ? "\r\n" : (text.Contains('\n') ? "\n" : Environment.NewLine);
            var parts = text.Split('\n');
            var assignment = $"{key}={value}";

            for (var i = 0; i < parts.Length; i++)
            {
                var raw = parts[i];
                var hasCr = raw.EndsWith('\r');
                var content = hasCr ? raw[..^1] : raw;
                if (TryMatchKey(content, key, out _))
                {
                    parts[i] = assignment + (hasCr ? "\r" : "");
                    return string.Join('\n', parts);
                }
            }

            if (text.Length == 0)
                return assignment + newline;

            var sb = new StringBuilder(text);
            if (!text.EndsWith('\n'))
                sb.Append(newline);
            sb.Append(assignment).Append(newline);
            return sb.ToString();
        }

        public static string? ReadValue(string path, string key)
        {
            if (!File.Exists(path))
                return null;
            return GetValue(File.ReadAllText(path), key);
        }

        /// <summary>Atomically rewrites the file with the key set (temp file in the same folder, then replace).</summary>
        public static void WriteValue(string path, string key, string value)
        {
            var (text, bom) = ReadWithBom(path);
            WriteAtomic(path, SetValue(text, key, value), bom);
        }

        /// <summary>Atomically writes raw text; used to restore the original file on rollback.</summary>
        public static void WriteAtomic(string path, string content, bool withBom = false)
        {
            var body = Utf8NoBom.GetBytes(content);
            WriteAtomicBytes(path, withBom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray() : body);
        }

        /// <summary>Atomically replaces a file with exact bytes (byte-exact restore of a backed-up file).</summary>
        public static void WriteAtomicBytes(string path, byte[] content)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
            Directory.CreateDirectory(dir);
            var temp = Path.Combine(dir, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(temp, content);
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { }
            }
        }

        public static (string Text, bool HasBom) ReadWithBom(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Utf8NoBom.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
            return (text, hasBom);
        }

        private static IEnumerable<string> SplitLines(string text) =>
            text.Split('\n').Select(l => l.TrimEnd('\r'));

        private static bool TryMatchKey(string line, string key, out string value)
        {
            value = "";
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] == '#')
                return false;
            if (trimmed.StartsWith("export ", StringComparison.Ordinal))
                trimmed = trimmed[7..].TrimStart();

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
                return false;
            if (!string.Equals(trimmed[..eq].Trim(), key, StringComparison.Ordinal))
                return false;

            var raw = trimmed[(eq + 1)..].Trim();
            if (raw.Length >= 2 && (raw[0] == '"' && raw[^1] == '"' || raw[0] == '\'' && raw[^1] == '\''))
                raw = raw[1..^1];
            value = raw;
            return true;
        }
    }
}
