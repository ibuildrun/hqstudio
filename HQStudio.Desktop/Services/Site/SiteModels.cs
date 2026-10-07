namespace HQStudio.Services.Site
{
    /// <summary>Разметка установки: пишется установщиком в %LOCALAPPDATA%\HQStudio\install.json.</summary>
    public sealed record SiteInstallInfo(string ServerDir, string WebUrl, string ApiUrl, string Repo);

    public sealed record SiteInstallReadResult(SiteInstallInfo? Info, string? Problem)
    {
        public bool NotInstalled => Info == null && Problem == null;
    }

    public enum SiteDockerState
    {
        Unknown,
        Missing,
        NotRunning,
        Running
    }

    public enum SiteOperation
    {
        None,
        Starting,
        Stopping,
        Restarting
    }

    /// <summary>Идентификаторы пяти служб в docker-compose.yml.</summary>
    public static class SiteServiceIds
    {
        public const string Db = "db";
        public const string Api = "api";
        public const string Web = "web";
        public const string Proxy = "proxy";
        public const string Tuna = "tuna";

        public static readonly IReadOnlyList<string> Core = new[] { Db, Api, Web, Proxy };
        public static readonly IReadOnlyList<string> All = new[] { Db, Api, Web, Proxy, Tuna };

        public static string Title(string id) => id switch
        {
            Db => "База данных",
            Api => "Сервер (API)",
            Web => "Сайт",
            Proxy => "Прокси",
            Tuna => "Адрес в интернете",
            _ => id
        };
    }

    /// <summary>Одна строка вывода <c>docker compose ps --format json</c>.</summary>
    public sealed record ComposeServiceEntry(string Service, string State, string Health, string Status, int? ExitCode);

    public enum ServiceLevel
    {
        Unknown,
        Ok,
        Starting,
        Stopped,
        Error,
        Absent,
        NotConfigured
    }

    /// <summary>Состояние одной службы для карточки на странице.</summary>
    public sealed record ServiceStatus(string Id, string Title, ServiceLevel Level, string BadgeText, string RawText);

    public enum SitePill
    {
        Checking,
        Running,
        Starting,
        Stopping,
        Stopped,
        DockerDown,
        NotInstalled,
        Error
    }

    /// <summary>Итог для большой плашки в шапке страницы.</summary>
    public sealed record SiteOverview(SitePill Pill, string PillText, string Explanation);

    /// <summary>Снимок состояния сайта на момент опроса.</summary>
    public sealed record SiteSnapshot(
        bool Installed,
        SiteDockerState Docker,
        SiteOverview Overview,
        IReadOnlyList<ServiceStatus> Services,
        string Version,
        string LocalUrl,
        string? PublicUrl,
        bool TunnelConfigured,
        string? Details)
    {
        public static SiteSnapshot Checking { get; } = new(
            false, SiteDockerState.Unknown,
            new SiteOverview(SitePill.Checking, "Проверяю", "Смотрю, как работает сайт."),
            SiteStatusEvaluator.PlaceholderServices(), "", "", null, false, null);
    }

    public enum SiteFailureKind
    {
        None,
        DockerMissing,
        DockerNotRunning,
        PortBusy,
        NoInternet,
        ComposeFailed,
        NotInstalled,
        InvalidInput,
        Timeout,
        Cancelled,
        Other
    }

    /// <summary>Понятное объяснение ошибки и «сырые» последние строки для раскрывающегося блока.</summary>
    public sealed record SiteFailure(SiteFailureKind Kind, string Title, string Message, string Details);

    public sealed record SiteOperationResult(bool Success, string Message, SiteFailure? Failure = null, bool ConfigSaved = false)
    {
        public static SiteOperationResult Ok(string message) => new(true, message);

        public static SiteOperationResult Fail(SiteFailure failure) => new(false, failure.Message, failure);
    }

    /// <summary>Что показывать в окне ключей: значения секретов наружу не отдаются.</summary>
    public sealed record SiteKeysState(bool HasGeminiKey, bool HasTunaToken, string TunaSubdomain);

    /// <summary>Изменения ключей. <c>null</c> - не менять, пустая строка - удалить значение.</summary>
    public sealed record SiteKeysUpdate(string? GeminiKey, string? TunaToken, string? TunaSubdomain)
    {
        public bool IsEmpty => GeminiKey == null && TunaToken == null && TunaSubdomain == null;
    }

    public sealed record SiteLogsResult(bool Success, string Text, SiteFailure? Failure);
}
