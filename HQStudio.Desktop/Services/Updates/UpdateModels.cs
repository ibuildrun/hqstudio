namespace HQStudio.Services.Updates
{
    public static class UpdateLinks
    {
        public const string DockerInstallUrl = "https://docs.docker.com/desktop/setup/install/windows-install/";
    }

    /// <summary>Failure with a message that is safe to show to the end user as is (Russian).</summary>
    public sealed class UpdateException : Exception
    {
        public string? HelpUrl { get; }

        public UpdateException(string userMessage, Exception? inner = null, string? helpUrl = null)
            : base(userMessage, inner)
        {
            HelpUrl = helpUrl;
        }
    }

    /// <summary>Overall progress of one operation (Percent is 0..100).</summary>
    public sealed record UpdateProgress(string Stage, double Percent, string? Detail = null);

    /// <summary>Outcome of an app/site update. Services report failures this way instead of throwing.</summary>
    public sealed record UpdateOperationResult(
        bool Success,
        string Message,
        string? HelpUrl = null,
        bool RolledBack = false,
        bool RestartPending = false);

    public sealed record DownloadProgress(long BytesReceived, long? TotalBytes);

    /// <summary>Invokes the handler inline, so reports keep their order (unlike <see cref="Progress{T}"/>).</summary>
    public sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public SyncProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
    }

    public static class ByteSize
    {
        public static string Format(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
            < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:F1} МБ",
            _ => $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} ГБ"
        };
    }
}
