using HQStudio.Setup.Core;

namespace HQStudio.Setup.Install;

/// <summary>Setup log: every line is masked before it reaches the file, the screen or a bug report.</summary>
public sealed class SetupLog
{
    private const long MaxFileBytes = 1_000_000;

    private readonly string? _filePath;
    private readonly object _gate = new();
    private readonly List<string> _lines = new();

    public SetupLog(string? filePath, SecretMasker? masker = null)
    {
        _filePath = filePath;
        Masker = masker ?? new SecretMasker();
        PrepareFile();
    }

    public SecretMasker Masker { get; }

    public string? FilePath => _filePath;

    /// <summary>Raised (possibly from a worker thread) with the masked line.</summary>
    public event Action<string>? LineWritten;

    public void Write(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var masked = LogSanitizer.Sanitize(line.TrimEnd(), Masker);
        lock (_gate)
        {
            _lines.Add(masked);
            if (_lines.Count > 2000)
                _lines.RemoveRange(0, 500);

            if (_filePath != null)
            {
                try
                {
                    File.AppendAllText(_filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {masked}{Environment.NewLine}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        LineWritten?.Invoke(masked);
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
            return _lines.ToList();
    }

    private void PrepareFile()
    {
        if (_filePath == null)
            return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var info = new FileInfo(_filePath);
            if (info.Exists && info.Length > MaxFileBytes)
                File.Delete(_filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
