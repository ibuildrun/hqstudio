using System.ComponentModel;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace HQStudio.Setup.Core;

public enum FailureKind
{
    Unknown,
    DockerNotInstalled,
    DockerNotRunning,
    ComposeMissing,
    Network,
    PortBusy,
    DiskFull,
    FilesLocked,
    PayloadMissing,
    PayloadCorrupt,
    HealthTimeout,
    TunnelFailed,
    UserDeclined
}

/// <summary>A stage failure with a known cause. <see cref="Details"/> holds raw tool output for the log and the analyzer.</summary>
public sealed class InstallException : Exception
{
    public FailureKind Kind { get; }
    public string? Details { get; }

    public InstallException(FailureKind kind, string message, string? details = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Details = details;
    }
}

public sealed record FailureInfo(
    StageId Stage,
    FailureKind Kind,
    string Title,
    string Message,
    string Hint,
    string Technical);

public static class FailureAnalyzer
{
    private static readonly Regex DockerDown = new(
        @"cannot connect to the docker daemon|error during connect|docker daemon is not running|dockerDesktopLinuxEngine|docker_engine|failed to connect to the docker api|is the docker daemon running",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ComposeMissing = new(
        @"'compose' is not a docker command|docker: unknown command: docker compose|unknown shorthand flag: 'f' in -f",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PortConflict = new(
        @"port is already allocated|address already in use|ports are not available|bind for .* failed|Only one usage of each socket address|forbidden by its access permissions|port .* is already in use",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DiskFull = new(
        @"no space left on device|not enough space on the disk|disk is full|There is not enough space|insufficient disk space",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NetworkProblem = new(
        @"no such host|TLS handshake timeout|dial tcp|i/o timeout|connection refused|connection reset|Temporary failure in name resolution|network is unreachable|request canceled while waiting|context deadline exceeded|Client\.Timeout|unexpected EOF|failed to resolve reference|failed to do request|error pulling image|toomanyrequests|read: connection|proxyconnect",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Unhealthy = new(
        @"is unhealthy|dependency failed to start|health check failed",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsPortConflict(string? output) => !string.IsNullOrEmpty(output) && PortConflict.IsMatch(output);

    public static FailureKind ClassifyOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return FailureKind.Unknown;
        if (ComposeMissing.IsMatch(output)) return FailureKind.ComposeMissing;
        if (DiskFull.IsMatch(output)) return FailureKind.DiskFull;
        if (PortConflict.IsMatch(output)) return FailureKind.PortBusy;
        if (DockerDown.IsMatch(output)) return FailureKind.DockerNotRunning;
        if (Unhealthy.IsMatch(output)) return FailureKind.HealthTimeout;
        if (NetworkProblem.IsMatch(output)) return FailureKind.Network;
        return FailureKind.Unknown;
    }

    public static FailureInfo Analyze(StageId stage, Exception ex)
    {
        var kind = FailureKind.Unknown;
        string? details = null;
        var message = ex.Message;

        switch (ex)
        {
            case InstallException ie:
                kind = ie.Kind;
                details = ie.Details;
                if (kind == FailureKind.Unknown)
                    kind = ClassifyOutput((ie.Details ?? "") + "\n" + ie.Message);
                break;
            case IOException io when IsDiskFull(io):
                kind = FailureKind.DiskFull;
                break;
            case IOException io when IsSharingViolation(io):
                kind = FailureKind.FilesLocked;
                break;
            case UnauthorizedAccessException:
                kind = FailureKind.FilesLocked;
                break;
            case HttpRequestException or SocketException or TaskCanceledException:
                kind = FailureKind.Network;
                break;
            case Win32Exception { NativeErrorCode: 1223 }:
                kind = FailureKind.UserDeclined;
                break;
            default:
                kind = ClassifyOutput(ex.Message);
                break;
        }

        var technical = $"{ex.GetType().Name}: {message}";
        if (!string.IsNullOrWhiteSpace(details))
            technical += Environment.NewLine + Tail(details!, 40);

        var (title, text, hint) = Describe(kind, stage, message);
        return new FailureInfo(stage, kind, title, text, hint, technical);
    }

    public static (string Title, string Message, string Hint) Describe(FailureKind kind, StageId stage, string rawMessage) => kind switch
    {
        FailureKind.DockerNotInstalled => (
            "Docker не найден",
            "Для работы сайта нужна программа Docker Desktop, но на компьютере её не видно.",
            "Установите Docker Desktop, запустите установщик ещё раз или выберите «Установлю Docker позже»."),
        FailureKind.DockerNotRunning => (
            "Docker не запущен",
            "Docker Desktop выключен или ещё не успел запуститься.",
            "Запустите Docker Desktop, дождитесь значка кита в трее и нажмите «Повторить»."),
        FailureKind.ComposeMissing => (
            "В Docker нет нужного модуля",
            "Установленная версия Docker слишком старая: в ней нет команды docker compose.",
            "Обновите Docker Desktop до свежей версии и нажмите «Повторить»."),
        FailureKind.Network => (
            "Нет доступа к сайту загрузки",
            "Не удалось скачать файлы сайта: интернет пропал или сервис ghcr.io и Docker Hub недоступен из вашей сети.",
            "Проверьте интернет. Если интернет работает, включите VPN и нажмите «Повторить»."),
        FailureKind.PortBusy => (
            "Адрес сайта занят",
            "Не нашлось свободного порта для сайта: порты 8080-8090 заняты другими программами.",
            "Закройте программы, которые могут занимать порты (другие сайты и серверы), или перезагрузите компьютер, затем нажмите «Повторить»."),
        FailureKind.DiskFull => (
            "Не хватает места на диске",
            "На диске закончилось свободное место, поэтому файлы не удалось записать.",
            "Освободите хотя бы 5 ГБ на диске C: и нажмите «Повторить»."),
        FailureKind.FilesLocked => (
            "Файлы программы заняты",
            "Не удалось записать файлы: их использует другая программа (возможно, HQ Studio уже запущена).",
            "Закройте HQ Studio и нажмите «Повторить»."),
        FailureKind.PayloadMissing => (
            "В установщике нет файлов программы",
            "Эта сборка установщика не содержит встроенного архива payload.zip. Так бывает только в сборке для разработчиков.",
            "Скачайте готовый установщик HQ Studio со страницы релизов."),
        FailureKind.PayloadCorrupt => (
            "Установщик повреждён",
            "Встроенный архив с файлами программы не читается.",
            "Скачайте установщик заново и запустите его ещё раз."),
        FailureKind.HealthTimeout => (
            "Сайт запустился, но не отвечает",
            "Контейнеры запущены, однако сайт не ответил за отведённое время.",
            "Подождите минуту и нажмите «Повторить». Если не помогает, нажмите «Сообщить об ошибке»."),
        FailureKind.TunnelFailed => (
            "Не удалось получить публичный адрес",
            "Сервис Tuna не выдал адрес: возможно, токен неверный.",
            "Проверьте токен на сайте tuna.am и нажмите «Повторить»."),
        FailureKind.UserDeclined => (
            "Запрос Windows отклонён",
            "Для этого действия Windows просит разрешение, а оно не было дано.",
            "Нажмите «Повторить» и разрешите изменения, когда появится окно Windows."),
        _ => (
            "Что-то пошло не так",
            string.IsNullOrWhiteSpace(rawMessage) ? "Этап не завершился." : "Этап не завершился. Подробности есть в технических деталях.",
            "Нажмите «Повторить». Если ошибка повторяется, нажмите «Сообщить об ошибке».")
    };

    private static bool IsDiskFull(IOException ex)
    {
        // ERROR_DISK_FULL 0x70, ERROR_HANDLE_DISK_FULL 0x27
        var code = ex.HResult & 0xFFFF;
        return code is 0x70 or 0x27;
    }

    private static bool IsSharingViolation(IOException ex)
    {
        // ERROR_SHARING_VIOLATION 0x20, ERROR_LOCK_VIOLATION 0x21
        var code = ex.HResult & 0xFFFF;
        return code is 0x20 or 0x21;
    }

    public static string Tail(string text, int lines)
    {
        var all = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(Environment.NewLine, all.Length <= lines ? all : all[^lines..]);
    }
}
