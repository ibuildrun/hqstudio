using System.Text;

namespace HQStudio.Setup.Core;

/// <summary>Reads and edits single keys of a docker compose .env file; other lines are kept untouched.</summary>
public static class EnvFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static string? Get(string text, string key)
    {
        string? found = null;
        foreach (var raw in text.Split('\n'))
        {
            if (TryMatch(raw.TrimEnd('\r'), key, out var value))
                found = value;
        }
        return found;
    }

    /// <summary>Replaces the first assignment of the key or appends it; line endings of the file are kept.</summary>
    public static string Set(string text, string key, string value)
    {
        var newline = text.Contains("\r\n") ? "\r\n" : (text.Contains('\n') ? "\n" : "\r\n");
        var assignment = $"{key}={Quote(value)}";
        var parts = text.Split('\n');

        for (var i = 0; i < parts.Length; i++)
        {
            var hasCr = parts[i].EndsWith('\r');
            var content = hasCr ? parts[i][..^1] : parts[i];
            if (TryMatch(content, key, out _))
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

    /// <summary>
    /// Values with only safe characters stay bare. Anything else is quoted so that '$', '#', spaces and quotes
    /// typed into a password survive docker compose interpolation unchanged.
    /// </summary>
    public static string Quote(string value)
    {
        if (value.Length == 0 || IsBare(value))
            return value;

        if (!value.Contains('\'') && !value.Contains('\r') && !value.Contains('\n'))
            return "'" + value + "'";

        var sb = new StringBuilder("\"");
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '$': sb.Append("$$"); break;
                case '\r': break;
                case '\n': sb.Append("\\n"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.Append('"').ToString();
    }

    public static void WriteAtomic(string path, string content)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(dir);
        var temp = Path.Combine(dir, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temp, content, Utf8NoBom);
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

    public static string? ReadAllTextOrNull(string path)
    {
        if (!File.Exists(path))
            return null;
        var bytes = File.ReadAllBytes(path);
        var skip = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        return Utf8NoBom.GetString(bytes, skip, bytes.Length - skip);
    }

    private static bool IsBare(string value)
    {
        foreach (var c in value)
        {
            var ok = char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '@' or ':' or '/' or '+' or '=' or ',';
            if (!ok)
                return false;
        }
        return true;
    }

    private static bool TryMatch(string line, string key, out string value)
    {
        value = "";
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] == '#')
            return false;
        if (trimmed.StartsWith("export ", StringComparison.Ordinal))
            trimmed = trimmed[7..].TrimStart();

        var eq = trimmed.IndexOf('=');
        if (eq <= 0 || !string.Equals(trimmed[..eq].Trim(), key, StringComparison.Ordinal))
            return false;

        value = Unquote(trimmed[(eq + 1)..].Trim());
        return true;
    }

    private static string Unquote(string raw)
    {
        if (raw.Length >= 2 && raw[0] == '\'' && raw[^1] == '\'')
            return raw[1..^1];

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            var inner = raw[1..^1];
            var sb = new StringBuilder();
            for (var i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (c == '\\' && i + 1 < inner.Length)
                {
                    var n = inner[++i];
                    sb.Append(n == 'n' ? '\n' : n);
                }
                else if (c == '$' && i + 1 < inner.Length && inner[i + 1] == '$')
                {
                    sb.Append('$');
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        var hash = raw.IndexOf(" #", StringComparison.Ordinal);
        return hash >= 0 ? raw[..hash].TrimEnd() : raw;
    }
}
