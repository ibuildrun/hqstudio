using System.Text.RegularExpressions;

namespace HQStudio.Services.Site
{
    public sealed record SiteStatusInput(
        bool Installed,
        string? InstallProblem,
        SiteDockerState Docker,
        // Строки compose ps; null, если опросить не удалось.
        IReadOnlyList<ComposeServiceEntry>? Entries,
        // Пояснение, если Docker работает, а compose ps завершился ошибкой.
        string? ComposeProblem,
        bool TunnelConfigured,
        // Ответ /api/health; null, если проверку не делали.
        bool? HealthOk,
        // Сколько проверок /api/health подряд не удалось.
        int HealthFailures,
        SiteOperation Operation);

    /// <summary>Превращает вывод docker и проверку здоровья в состояния карточек и общую плашку.</summary>
    public static class SiteStatusEvaluator
    {
        // Столько проверок подряд (по 5 секунд) сайт может не отвечать, прежде чем это станет ошибкой.
        public const int HealthFailureLimit = 12;

        private static readonly Regex ExitedCode = new(@"Exited \((-?\d+)\)", RegexOptions.Compiled);

        public static IReadOnlyList<ServiceStatus> PlaceholderServices() => SiteServiceIds.All
            .Select(id => new ServiceStatus(id, SiteServiceIds.Title(id), ServiceLevel.Unknown, "Проверяю", "-"))
            .ToList();

        public static ServiceStatus ClassifyEntry(string id, ComposeServiceEntry? entry)
        {
            var title = SiteServiceIds.Title(id);
            if (entry == null)
                return new ServiceStatus(id, title, ServiceLevel.Absent, "Не запущен", "Контейнера нет");

            var raw = entry.Status.Length > 0 ? entry.Status : entry.State;
            switch (entry.State)
            {
                case "running":
                    return entry.Health switch
                    {
                        "unhealthy" => new ServiceStatus(id, title, ServiceLevel.Error, "Не отвечает", raw),
                        "starting" => new ServiceStatus(id, title, ServiceLevel.Starting, "Запускается", raw),
                        _ => new ServiceStatus(id, title, ServiceLevel.Ok, "Работает", raw)
                    };
                case "restarting":
                    return new ServiceStatus(id, title, ServiceLevel.Error, "Перезапускается", raw);
                case "created":
                    return new ServiceStatus(id, title, ServiceLevel.Starting, "Запускается", raw);
                case "paused":
                    return new ServiceStatus(id, title, ServiceLevel.Stopped, "На паузе", raw);
                case "dead":
                    return new ServiceStatus(id, title, ServiceLevel.Error, "Сбой", raw);
                case "exited":
                case "removing":
                    return ExitCode(entry) is { } code && code != 0
                        ? new ServiceStatus(id, title, ServiceLevel.Error, "Сбой", raw)
                        : new ServiceStatus(id, title, ServiceLevel.Stopped, "Остановлен", raw);
                default:
                    return new ServiceStatus(id, title, ServiceLevel.Unknown, "Неизвестно", raw);
            }
        }

        public static IReadOnlyList<ServiceStatus> BuildServices(SiteStatusInput input)
        {
            if (input.Entries == null)
            {
                var reason = input.Docker switch
                {
                    SiteDockerState.Missing => "Docker не найден",
                    SiteDockerState.NotRunning => "Docker не запущен",
                    _ => "Нет данных"
                };
                return SiteServiceIds.All
                    .Select(id => id == SiteServiceIds.Tuna && !input.TunnelConfigured
                        ? TunnelNotConfigured()
                        : new ServiceStatus(id, SiteServiceIds.Title(id), ServiceLevel.Unknown, "Нет данных", reason))
                    .ToList();
            }

            var services = new List<ServiceStatus>();
            foreach (var id in SiteServiceIds.Core)
                services.Add(ClassifyEntry(id, Find(input.Entries, id)));

            services.Add(input.TunnelConfigured
                ? ClassifyEntry(SiteServiceIds.Tuna, Find(input.Entries, SiteServiceIds.Tuna))
                : TunnelNotConfigured());
            return services;
        }

        public static SiteOverview Evaluate(SiteStatusInput input, IReadOnlyList<ServiceStatus> services)
        {
            if (!input.Installed)
            {
                return input.InstallProblem != null
                    ? new SiteOverview(SitePill.Error, "Ошибка", input.InstallProblem + " Запустите установщик HQ Studio ещё раз.")
                    : new SiteOverview(SitePill.NotInstalled, "Не установлен",
                        "Сайт ещё не установлен на этом компьютере.");
            }

            if (input.Docker == SiteDockerState.Missing)
            {
                return new SiteOverview(SitePill.DockerDown, "Docker не установлен",
                    "Для работы сайта нужна программа Docker Desktop, но на этом компьютере её не нашлось. Запустите установщик HQ Studio ещё раз.");
            }

            if (input.Docker == SiteDockerState.NotRunning)
            {
                return new SiteOverview(SitePill.DockerDown, "Docker не запущен",
                    "Сайт работает через программу Docker, а она сейчас выключена. Нажмите «Запустить Docker».");
            }

            if (input.Entries == null)
            {
                return input.ComposeProblem != null
                    ? new SiteOverview(SitePill.Error, "Ошибка", input.ComposeProblem)
                    : new SiteOverview(SitePill.Checking, "Проверяю", "Смотрю, как работает сайт.");
            }

            if (input.Operation == SiteOperation.Stopping)
                return new SiteOverview(SitePill.Stopping, "Останавливается", "Останавливаю сайт. Это займёт несколько секунд.");

            var core = services.Where(s => SiteServiceIds.Core.Contains(s.Id)).ToList();
            var starting = input.Operation is SiteOperation.Starting or SiteOperation.Restarting;
            var startingOverview = new SiteOverview(SitePill.Starting, "Запускается",
                "Сайт запускается. Обычно это занимает до пары минут.");

            if (core.Any(s => s.Level == ServiceLevel.Error))
            {
                var names = Names(core.Where(s => s.Level == ServiceLevel.Error));
                return new SiteOverview(SitePill.Error, "Ошибка",
                    $"Не работает: {names}. Нажмите «Перезапустить» или посмотрите «Логи».");
            }

            if (core.All(s => s.Level == ServiceLevel.Ok))
            {
                if (input.HealthOk == true)
                {
                    var tunnel = services.FirstOrDefault(s => s.Id == SiteServiceIds.Tuna);
                    var tunnelProblem = tunnel is { Level: ServiceLevel.Error or ServiceLevel.Stopped or ServiceLevel.Absent };
                    return new SiteOverview(SitePill.Running, "Работает",
                        tunnelProblem
                            ? "Сайт работает на этом компьютере, но адрес в интернете сейчас недоступен."
                            : "Сайт работает. Его можно открыть в браузере на этом компьютере.");
                }

                if (input.HealthFailures >= HealthFailureLimit && !starting)
                {
                    return new SiteOverview(SitePill.Error, "Ошибка",
                        "Службы запущены, но сайт не отвечает. Нажмите «Перезапустить» или посмотрите «Логи».");
                }

                return startingOverview;
            }

            if (starting || core.Any(s => s.Level == ServiceLevel.Starting))
                return startingOverview;

            if (core.All(s => s.Level is ServiceLevel.Stopped or ServiceLevel.Absent))
            {
                return new SiteOverview(SitePill.Stopped, "Остановлен",
                    "Сайт остановлен. Нажмите «Запустить», чтобы включить его.");
            }

            var missing = Names(core.Where(s => s.Level != ServiceLevel.Ok));
            return new SiteOverview(SitePill.Error, "Ошибка",
                $"Часть сайта не работает: {missing}. Нажмите «Перезапустить» или посмотрите «Логи».");
        }

        private static ServiceStatus TunnelNotConfigured() => new(
            SiteServiceIds.Tuna, SiteServiceIds.Title(SiteServiceIds.Tuna), ServiceLevel.NotConfigured,
            "Не настроен", "Нужны токен Tuna и свой домен: кнопка «Ключи»");

        private static ComposeServiceEntry? Find(IReadOnlyList<ComposeServiceEntry> entries, string id) =>
            entries.FirstOrDefault(e => string.Equals(e.Service, id, StringComparison.OrdinalIgnoreCase));

        private static string Names(IEnumerable<ServiceStatus> services) =>
            string.Join(", ", services.Select(s => $"«{s.Title}»"));

        private static int? ExitCode(ComposeServiceEntry entry)
        {
            if (entry.ExitCode.HasValue)
                return entry.ExitCode;
            var match = ExitedCode.Match(entry.Status);
            return match.Success && int.TryParse(match.Groups[1].Value, out var code) ? code : null;
        }
    }
}
