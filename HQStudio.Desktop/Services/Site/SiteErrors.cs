using System.ComponentModel;
using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    /// <summary>Переводит сбои docker на простой русский язык: что случилось и что делать.</summary>
    public static class SiteErrorMapper
    {
        private static readonly string[] DockerNotRunningMarkers =
        {
            "error during connect", "cannot connect to the docker daemon", "docker daemon is not running",
            "failed to connect to the docker api", "dockerdesktoplinuxengine", "is the docker daemon running",
            "open //./pipe/docker", "docker_engine"
        };

        private static readonly string[] PortBusyMarkers =
        {
            "port is already allocated", "address already in use", "ports are not available",
            "only one usage of each socket address", "failed to bind", "bind for"
        };

        private static readonly string[] NoInternetMarkers =
        {
            "no such host", "dial tcp", "tls handshake timeout", "i/o timeout", "client.timeout exceeded",
            "network is unreachable", "temporary failure in name resolution", "no route to host",
            "could not resolve host", "connection reset by peer", "request canceled while waiting for connection",
            "failed to resolve source metadata", "failed to do request"
        };

        private static readonly Regex BoundPort = new(@"Bind for [\d.:\[\]a-f]*:(\d{2,5})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static SiteFailureKind Classify(string? output)
        {
            var text = (output ?? "").ToLowerInvariant();
            if (DockerNotRunningMarkers.Any(text.Contains))
                return SiteFailureKind.DockerNotRunning;
            if (PortBusyMarkers.Any(text.Contains))
                return SiteFailureKind.PortBusy;
            if (NoInternetMarkers.Any(text.Contains))
                return SiteFailureKind.NoInternet;
            return SiteFailureKind.ComposeFailed;
        }

        /// <summary>Последние непустые строки вывода без паролей: для раскрывающегося блока «Подробности».</summary>
        public static string Tail(string? output, IEnumerable<string>? knownSecrets = null, int lines = 12)
        {
            var kept = (output ?? "")
                .Split('\n')
                .Select(l => l.TrimEnd('\r').TrimEnd())
                .Where(l => l.Length > 0)
                .TakeLast(lines);
            return SiteSecretSanitizer.Sanitize(string.Join(Environment.NewLine, kept), knownSecrets);
        }

        /// <param name="action">Что делали, в творительном падеже без глагола: «Запуск сайта».</param>
        public static SiteFailure FromProcess(string action, SiteProcessResult result, IEnumerable<string>? knownSecrets = null)
        {
            var kind = Classify(result.Combined);
            var details = Tail(result.Combined, knownSecrets);
            return kind switch
            {
                SiteFailureKind.DockerNotRunning => DockerNotRunning(details),
                SiteFailureKind.PortBusy => PortBusy(result.Combined, details),
                SiteFailureKind.NoInternet => NoInternet(details),
                _ => ComposeFailed(action, details)
            };
        }

        public static SiteFailure FromException(string action, Exception ex)
        {
            switch (ex)
            {
                case OperationCanceledException:
                    return new SiteFailure(SiteFailureKind.Cancelled, "Отменено", "Действие отменено.", "");
                case Win32Exception:
                    return DockerMissing(ex.Message);
                default:
                    return new SiteFailure(SiteFailureKind.Other, "Что-то пошло не так",
                        $"{action} не получилось. Попробуйте ещё раз; если не помогло, посмотрите подробности ниже.",
                        SiteSecretSanitizer.Sanitize(ex.Message));
            }
        }

        public static SiteFailure DockerMissing(string details = "") => new(
            SiteFailureKind.DockerMissing, "Docker не найден",
            "Не удалось найти программу Docker, а сайт без неё не работает. Запустите установщик HQ Studio ещё раз.",
            details);

        public static SiteFailure DockerNotRunning(string details = "") => new(
            SiteFailureKind.DockerNotRunning, "Docker не запущен",
            "Программа Docker сейчас выключена. Нажмите «Запустить Docker» и подождите, пока она включится.",
            details);

        public static SiteFailure NotInstalled() => new(
            SiteFailureKind.NotInstalled, "Сайт не установлен",
            "Сайт ещё не установлен на этом компьютере. Его устанавливает установщик HQ Studio.", "");

        public static SiteFailure InvalidInput(string message) => new(
            SiteFailureKind.InvalidInput, "Проверьте данные", message, "");

        public static SiteFailure Timeout(string message, string details = "") => new(
            SiteFailureKind.Timeout, "Слишком долго", message, details);

        private static SiteFailure PortBusy(string output, string details)
        {
            var match = BoundPort.Match(output);
            var port = match.Success ? $" (порт {match.Groups[1].Value})" : "";
            return new SiteFailure(SiteFailureKind.PortBusy, "Порт занят",
                $"Сайт не смог занять свой порт{port}: его уже использует другая программа. Закройте её или перезагрузите компьютер и попробуйте ещё раз.",
                details);
        }

        private static SiteFailure NoInternet(string details) => new(
            SiteFailureKind.NoInternet, "Нет интернета",
            "Не получилось скачать нужные файлы. Проверьте подключение к интернету и попробуйте ещё раз.",
            details);

        private static SiteFailure ComposeFailed(string action, string details) => new(
            SiteFailureKind.ComposeFailed, "Не получилось",
            $"{action} не получилось. Подробности можно раскрыть ниже и показать разработчику.",
            details);
    }
}
