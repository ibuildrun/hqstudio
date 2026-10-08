using System.ComponentModel;
using System.IO;

namespace HQStudio.Services.Site
{
    public interface ISiteService
    {
        Task<SiteSnapshot> RefreshAsync(SiteOperation operation, CancellationToken ct);
        Task<SiteOperationResult> StartAsync(Action<string>? status, CancellationToken ct);
        Task<SiteOperationResult> StopAsync(Action<string>? status, CancellationToken ct);
        Task<SiteOperationResult> RestartAsync(Action<string>? status, CancellationToken ct);
        Task<SiteOperationResult> StartDockerAsync(Action<string>? status, CancellationToken ct);
        Task<SiteOperationResult> ApplyKeysAsync(SiteKeysUpdate update, Action<string>? status, CancellationToken ct);
        Task<SiteLogsResult> GetLogsAsync(string service, CancellationToken ct);
        SiteKeysState? ReadKeysState();
    }

    /// <summary>Сроки ожидания. Количество попыток считается из них, а не по часам, поэтому тесты не ждут.</summary>
    public sealed record SiteTimings(
        TimeSpan DockerWait,
        TimeSpan HealthWait,
        TimeSpan TunnelWait,
        TimeSpan PollInterval,
        Func<TimeSpan, CancellationToken, Task> Delay)
    {
        public static SiteTimings Default { get; } = new(
            TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(240), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(3),
            (span, ct) => Task.Delay(span, ct));

        public int Attempts(TimeSpan total) =>
            PollInterval <= TimeSpan.Zero ? 1 : (int)Math.Ceiling(total / PollInterval) + 1;
    }

    /// <summary>Запуск, остановка и настройка сайта через docker compose. Все окна процессов скрыты.</summary>
    public sealed class SiteManager : ISiteService
    {
        private readonly ISiteInstallStore _install;
        private readonly ISiteFiles _files;
        private readonly ISiteProcessRunner _runner;
        private readonly ISiteDockerLocator _locator;
        private readonly ISiteHealthProbe _probe;
        private readonly SiteTimings _timings;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private int _healthFailures;

        public SiteManager(ISiteInstallStore install, ISiteFiles files, ISiteProcessRunner runner,
            ISiteDockerLocator locator, ISiteHealthProbe probe, SiteTimings? timings = null)
        {
            _install = install;
            _files = files;
            _runner = runner;
            _locator = locator;
            _probe = probe;
            _timings = timings ?? SiteTimings.Default;
        }

        public static SiteManager CreateDefault()
        {
            var files = new SiteFiles();
            return new SiteManager(
                new SiteInstallStore(files, SitePaths.FromEnvironment().InstallJson),
                files, new SiteProcessRunner(), new SiteDockerLocator(), new HttpSiteHealthProbe());
        }

        private sealed record Context(SiteInstallInfo Info, string EnvPath, string EnvText, int Port, bool Tunnel)
        {
            public string ServerDir => Info.ServerDir;

            public string Token => (SiteEnvFile.GetValue(EnvText, SiteEnvKeys.TunaToken) ?? "").Trim();

            public string Domain => (SiteEnvFile.GetValue(EnvText, SiteEnvKeys.TunaDomain) ?? "").Trim();

            /// <summary>Известные секреты и любые переменные, похожие на секрет (в том числе давно не используемые).</summary>
            public IEnumerable<string> Secrets => SiteEnvFile.Parse(EnvText)
                .Where(p => SiteEnvKeys.Secrets.Contains(p.Key) || SiteSecretSanitizer.LooksLikeSecretName(p.Key))
                .Select(p => p.Value)
                .Where(v => v.Length > 0)
                .Distinct();
        }

        // ------------------------------------------------------------------ опрос

        public async Task<SiteSnapshot> RefreshAsync(SiteOperation operation, CancellationToken ct)
        {
            var read = _install.Read();
            if (read.Info == null)
                return Build(new SiteStatusInput(false, read.Problem, SiteDockerState.Unknown, null, null, false, null, 0, operation),
                    "", "", null);

            var (ctx, envFailure) = LoadContext(read.Info);
            if (ctx == null)
            {
                return Build(new SiteStatusInput(true, null, SiteDockerState.Unknown, null, envFailure!.Message, false, null, 0, operation),
                    "", "", null);
            }

            var docker = _locator.FindDocker();
            var state = SiteDockerState.Missing;
            IReadOnlyList<ComposeServiceEntry>? entries = null;
            string? composeProblem = null;

            if (docker != null)
            {
                try
                {
                    var ps = await ComposeAsync(docker, ctx, null, ct, "ps", "--all", "--format", "json").ConfigureAwait(false);
                    if (ps.ExitCode == 0)
                    {
                        state = SiteDockerState.Running;
                        entries = SiteComposePsParser.Parse(ps.Output);
                    }
                    else if (SiteErrorMapper.Classify(ps.Combined) == SiteFailureKind.DockerNotRunning)
                    {
                        state = SiteDockerState.NotRunning;
                    }
                    else
                    {
                        state = SiteDockerState.Running;
                        composeProblem = SiteErrorMapper.FromProcess("Проверка состояния", ps, ctx.Secrets).Message;
                    }
                }
                catch (Win32Exception)
                {
                    state = SiteDockerState.Missing;
                }
            }

            bool? healthOk = null;
            var proxy = entries?.FirstOrDefault(e => e.Service == SiteServiceIds.Proxy);
            if (proxy is { State: "running" })
            {
                healthOk = await _probe.IsHealthyAsync(ctx.Port, ct).ConfigureAwait(false);
                _healthFailures = healthOk == true ? 0 : _healthFailures + 1;
            }
            else
            {
                _healthFailures = 0;
            }

            var input = new SiteStatusInput(true, null, state, entries, composeProblem, ctx.Tunnel, healthOk,
                _healthFailures, operation);
            return Build(input, SiteEnvFile.GetValue(ctx.EnvText, SiteEnvKeys.Version) ?? "",
                $"http://localhost:{ctx.Port}", PublicUrlOf(ctx));
        }

        private static SiteSnapshot Build(SiteStatusInput input, string version, string localUrl, string? publicUrl)
        {
            var services = SiteStatusEvaluator.BuildServices(input);
            var overview = SiteStatusEvaluator.Evaluate(input, services);
            return new SiteSnapshot(input.Installed, input.Docker, overview, services, version, localUrl, publicUrl,
                input.TunnelConfigured, null);
        }

        public SiteKeysState? ReadKeysState()
        {
            var (ctx, _) = LoadContext();
            if (ctx == null)
                return null;
            return new SiteKeysState(ctx.Token.Length > 0, ctx.Domain);
        }

        // ------------------------------------------------------------------ запуск, остановка

        public Task<SiteOperationResult> StartAsync(Action<string>? status, CancellationToken ct) =>
            Exclusive(() => StartCoreAsync(status, ct, "Сайт запущен."));

        public Task<SiteOperationResult> StopAsync(Action<string>? status, CancellationToken ct) =>
            Exclusive(() => StopCoreAsync(status, ct, "Сайт остановлен."));

        public Task<SiteOperationResult> RestartAsync(Action<string>? status, CancellationToken ct) =>
            Exclusive(async () =>
            {
                var stop = await StopCoreAsync(status, ct, "").ConfigureAwait(false);
                return stop.Success ? await StartCoreAsync(status, ct, "Сайт перезапущен.").ConfigureAwait(false) : stop;
            });

        public Task<SiteOperationResult> StartDockerAsync(Action<string>? status, CancellationToken ct) =>
            Exclusive(() => StartDockerCoreAsync(status, ct));

        private async Task<SiteOperationResult> StartCoreAsync(Action<string>? status, CancellationToken ct, string doneMessage)
        {
            var (ctx, failure) = LoadContext();
            if (ctx == null)
                return SiteOperationResult.Fail(failure!);
            var docker = _locator.FindDocker();
            if (docker == null)
                return SiteOperationResult.Fail(SiteErrorMapper.DockerMissing());

            try
            {
                status?.Invoke("Проверяю Docker");
                if (!await EngineRunningAsync(docker, ct).ConfigureAwait(false))
                {
                    var started = await StartDockerCoreAsync(status, ct).ConfigureAwait(false);
                    if (!started.Success)
                        return started;
                }

                ctx = SyncPublicUrl(ctx);

                status?.Invoke("Запускаю сайт");
                var up = await ComposeAsync(docker, ctx, line => OnUpLine(line, status), ct,
                    "up", "-d", "--remove-orphans").ConfigureAwait(false);
                if (up.ExitCode != 0)
                    return SiteOperationResult.Fail(SiteErrorMapper.FromProcess("Запуск сайта", up, ctx.Secrets));

                status?.Invoke("Жду, пока сайт ответит");
                if (!await WaitHealthyAsync(ctx.Port, ct).ConfigureAwait(false))
                {
                    return SiteOperationResult.Fail(SiteErrorMapper.Timeout(
                        "Сайт запущен, но пока не отвечает. Подождите минуту и посмотрите состояние ещё раз; если не помогло, откройте «Логи»."));
                }

                var message = doneMessage;
                if (ctx.Tunnel)
                {
                    status?.Invoke("Проверяю адрес в интернете");
                    message += await WaitTunnelAsync(docker, ctx, ct).ConfigureAwait(false)
                        ? $" Адрес в интернете: {SiteEnvFile.PublicUrlOf(ctx.Domain)}"
                        : TunnelNotUpHint;
                }
                return SiteOperationResult.Ok(message);
            }
            catch (Exception ex) when (ex is OperationCanceledException or Win32Exception)
            {
                return SiteOperationResult.Fail(SiteErrorMapper.FromException("Запуск сайта", ex));
            }
        }

        private async Task<SiteOperationResult> StopCoreAsync(Action<string>? status, CancellationToken ct, string doneMessage)
        {
            var (ctx, failure) = LoadContext();
            if (ctx == null)
                return SiteOperationResult.Fail(failure!);
            var docker = _locator.FindDocker();
            if (docker == null)
                return SiteOperationResult.Fail(SiteErrorMapper.DockerMissing());

            try
            {
                if (!await EngineRunningAsync(docker, ct).ConfigureAwait(false))
                    return SiteOperationResult.Ok("Сайт уже остановлен: Docker выключен.");

                status?.Invoke("Останавливаю сайт");
                var stop = await ComposeAsync(docker, ctx, null, ct, "stop").ConfigureAwait(false);
                return stop.ExitCode == 0
                    ? SiteOperationResult.Ok(doneMessage)
                    : SiteOperationResult.Fail(SiteErrorMapper.FromProcess("Остановка сайта", stop, ctx.Secrets));
            }
            catch (Exception ex) when (ex is OperationCanceledException or Win32Exception)
            {
                return SiteOperationResult.Fail(SiteErrorMapper.FromException("Остановка сайта", ex));
            }
        }

        private async Task<SiteOperationResult> StartDockerCoreAsync(Action<string>? status, CancellationToken ct)
        {
            var docker = _locator.FindDocker();
            if (docker == null)
                return SiteOperationResult.Fail(SiteErrorMapper.DockerMissing());

            try
            {
                if (await EngineRunningAsync(docker, ct).ConfigureAwait(false))
                    return SiteOperationResult.Ok("Docker уже работает.");

                var desktop = _locator.FindDockerDesktop();
                if (desktop == null)
                    return SiteOperationResult.Fail(SiteErrorMapper.DockerMissing());

                status?.Invoke("Запускаю Docker. Это может занять пару минут");
                try
                {
                    _runner.StartDetached(desktop, "", null);
                }
                catch (Win32Exception ex)
                {
                    return SiteOperationResult.Fail(SiteErrorMapper.DockerMissing(ex.Message));
                }

                var attempts = _timings.Attempts(_timings.DockerWait);
                for (var attempt = 0; attempt < attempts; attempt++)
                {
                    await _timings.Delay(_timings.PollInterval, ct).ConfigureAwait(false);
                    if (await EngineRunningAsync(docker, ct).ConfigureAwait(false))
                        return SiteOperationResult.Ok("Docker запущен.");

                    var seconds = (int)(_timings.PollInterval.TotalSeconds * (attempt + 1));
                    status?.Invoke($"Жду, пока Docker включится ({seconds} с)");
                }

                return SiteOperationResult.Fail(SiteErrorMapper.Timeout(
                    "Docker не включился за отведённое время. Откройте программу Docker Desktop вручную, дождитесь зелёного значка и повторите."));
            }
            catch (Exception ex) when (ex is OperationCanceledException or Win32Exception)
            {
                return SiteOperationResult.Fail(SiteErrorMapper.FromException("Запуск Docker", ex));
            }
        }

        // ------------------------------------------------------------------ ключи

        public Task<SiteOperationResult> ApplyKeysAsync(SiteKeysUpdate update, Action<string>? status, CancellationToken ct) =>
            Exclusive(() => ApplyKeysCoreAsync(update, status, ct));

        private async Task<SiteOperationResult> ApplyKeysCoreAsync(SiteKeysUpdate update, Action<string>? status, CancellationToken ct)
        {
            if (update.IsEmpty)
                return SiteOperationResult.Ok("Ничего не изменилось.");

            var token = update.TunaToken?.Trim();
            var domain = update.TunaDomain?.Trim();

            if (token != null && !SiteEnvFile.IsSafeValue(token))
                return SiteOperationResult.Fail(SiteErrorMapper.InvalidInput("Токен Tuna содержит пробелы или недопустимые символы. Скопируйте его заново."));
            if (domain != null)
            {
                var check = SiteEnvFile.CheckDomain(domain);
                if (check == DomainCheck.NonAscii)
                    return SiteOperationResult.Fail(SiteErrorMapper.InvalidInput("Домен нужно записать латиницей (punycode), например xn--80aswg.xn--p1ai."));
                if (check == DomainCheck.Invalid)
                    return SiteOperationResult.Fail(SiteErrorMapper.InvalidInput("Домен записывается так: crm.example.ru. Только строчные латинские буквы, цифры, дефис и точки, без http:// и без пути."));
            }

            var (ctx, failure) = LoadContext();
            if (ctx == null)
                return SiteOperationResult.Fail(failure!);

            var hadToken = ctx.Token.Length > 0;
            var hadTunnel = ctx.Tunnel;
            var oldDomain = ctx.Domain;

            var changes = new Dictionary<string, string>();
            if (token != null)
                changes[SiteEnvKeys.TunaToken] = token;
            if (domain != null)
            {
                changes[SiteEnvKeys.TunaDomain] = domain;
                // API берёт PUBLIC_URL для CORS. Чужое значение при удалении домена не трогаем.
                if (domain.Length > 0)
                    changes[SiteEnvKeys.PublicUrl] = SiteEnvFile.PublicUrlOf(domain);
                else if (oldDomain.Length > 0 &&
                         SiteEnvFile.GetValue(ctx.EnvText, SiteEnvKeys.PublicUrl) == SiteEnvFile.PublicUrlOf(oldDomain))
                    changes[SiteEnvKeys.PublicUrl] = "";
            }

            try
            {
                status?.Invoke("Сохраняю настройки");
                _files.WriteAllTextAtomic(ctx.EnvPath, SiteEnvFile.SetValues(ctx.EnvText, changes));
                if (domain is { Length: > 0 })
                    _files.WriteAllTextAtomic(SitePaths.PublicUrlFile(ctx.ServerDir), SiteEnvFile.PublicUrlOf(domain));
                else if (domain != null && _files.FileExists(SitePaths.PublicUrlFile(ctx.ServerDir)))
                    _files.DeleteFile(SitePaths.PublicUrlFile(ctx.ServerDir));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.Other, "Не удалось сохранить",
                    "Не получилось записать файл настроек сайта. Закройте программы, которые могут его держать, и попробуйте ещё раз.",
                    SiteSecretSanitizer.Sanitize(ex.Message)));
            }

            var (saved, _) = LoadContext();
            ctx = saved ?? ctx;

            var docker = _locator.FindDocker();
            if (docker == null)
                return SavedNotApplied(SiteErrorMapper.DockerMissing());

            try
            {
                if (!await EngineRunningAsync(docker, ct).ConfigureAwait(false))
                    return SiteOperationResult.Ok("Настройки сохранены. Они заработают, когда вы запустите сайт.");

                status?.Invoke("Применяю настройки");
                var up = await ComposeAsync(docker, ctx, line => OnUpLine(line, status), ct,
                    "up", "-d", "--remove-orphans").ConfigureAwait(false);
                if (up.ExitCode != 0)
                    return SavedNotApplied(SiteErrorMapper.FromProcess("Применение настроек", up, ctx.Secrets));

                if (!ctx.Tunnel)
                {
                    if (hadToken)
                    {
                        // Контейнер tuna с выключенным профилем не считается «лишним», поэтому убираем его явно.
                        status?.Invoke("Отключаю адрес в интернете");
                        await _runner.RunAsync(docker,
                            SiteComposeCommand.Build(ctx.ServerDir, true, "rm", "--stop", "--force", SiteServiceIds.Tuna),
                            null, ct).ConfigureAwait(false);
                    }
                    return SiteOperationResult.Ok(NoTunnelMessage(ctx, hadTunnel));
                }

                status?.Invoke("Проверяю адрес в интернете");
                return SiteOperationResult.Ok(await WaitTunnelAsync(docker, ctx, ct).ConfigureAwait(false)
                    ? $"Настройки сохранены и применены. Адрес в интернете: {SiteEnvFile.PublicUrlOf(ctx.Domain)}"
                    : "Настройки сохранены и применены." + TunnelNotUpHint);
            }
            catch (Exception ex) when (ex is OperationCanceledException or Win32Exception)
            {
                return SavedNotApplied(SiteErrorMapper.FromException("Применение настроек", ex));
            }
        }

        // Туннель работает только при токене и домене вместе: подсказываем, чего не хватает.
        private static string NoTunnelMessage(Context ctx, bool hadTunnel)
        {
            if (hadTunnel)
                return "Настройки сохранены и применены. Адрес в интернете отключён.";
            if (ctx.Token.Length > 0)
                return "Настройки сохранены и применены. Чтобы сайт открывался из интернета, добавьте ещё и свой домен.";
            if (ctx.Domain.Length > 0)
                return "Настройки сохранены и применены. Чтобы сайт открывался из интернета, добавьте ещё и токен Tuna.";
            return "Настройки сохранены и применены.";
        }

        private static SiteOperationResult SavedNotApplied(SiteFailure failure)
        {
            var explained = failure with { Message = "Настройки сохранены, но применить их не получилось. " + failure.Message };
            return new SiteOperationResult(false, explained.Message, explained, ConfigSaved: true);
        }

        // ------------------------------------------------------------------ журналы

        public async Task<SiteLogsResult> GetLogsAsync(string service, CancellationToken ct)
        {
            if (!SiteServiceIds.All.Contains(service))
                return new SiteLogsResult(false, "", SiteErrorMapper.InvalidInput("Неизвестная служба."));

            var (ctx, failure) = LoadContext();
            if (ctx == null)
                return new SiteLogsResult(false, "", failure);

            if (service == SiteServiceIds.Tuna && !ctx.Tunnel)
                return new SiteLogsResult(true, "Адрес в интернете не настроен, журнала нет. Укажите токен Tuna и свой домен в окне «Ключи».", null);

            var docker = _locator.FindDocker();
            if (docker == null)
                return new SiteLogsResult(false, "", SiteErrorMapper.DockerMissing());

            try
            {
                var logs = await ComposeAsync(docker, ctx, null, ct, "logs", "--no-color", "--tail", "300", service).ConfigureAwait(false);
                if (logs.ExitCode != 0)
                    return new SiteLogsResult(false, "", SiteErrorMapper.FromProcess("Чтение журнала", logs, ctx.Secrets));

                var text = SiteSecretSanitizer.Sanitize(logs.Combined, ctx.Secrets).Trim();
                return new SiteLogsResult(true, text.Length == 0 ? "Журнал пока пуст." : text, null);
            }
            catch (Exception ex) when (ex is OperationCanceledException or Win32Exception)
            {
                return new SiteLogsResult(false, "", SiteErrorMapper.FromException("Чтение журнала", ex));
            }
        }

        // ------------------------------------------------------------------ общие части

        private async Task<SiteOperationResult> Exclusive(Func<Task<SiteOperationResult>> action)
        {
            if (!await _gate.WaitAsync(0).ConfigureAwait(false))
            {
                return SiteOperationResult.Fail(new SiteFailure(SiteFailureKind.Other, "Подождите",
                    "Предыдущее действие ещё выполняется. Дождитесь его окончания.", ""));
            }

            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private (Context? Context, SiteFailure? Failure) LoadContext(SiteInstallInfo? known = null)
        {
            var info = known;
            if (info == null)
            {
                var read = _install.Read();
                if (read.Info == null)
                {
                    return (null, read.Problem != null
                        ? new SiteFailure(SiteFailureKind.Other, "Файл установки повреждён", read.Problem, "")
                        : SiteErrorMapper.NotInstalled());
                }
                info = read.Info;
            }

            var envPath = SitePaths.EnvFile(info.ServerDir);
            string envText;
            try
            {
                if (!_files.FileExists(envPath))
                    return (null, MissingEnv());
                envText = _files.ReadAllText(envPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return (null, MissingEnv());
            }

            var port = int.TryParse(SiteEnvFile.GetValue(envText, SiteEnvKeys.Port), out var p) && p is > 0 and < 65536
                ? p
                : SiteEnvKeys.DefaultPort;
            // Туннель нужен только при токене и своём домене вместе.
            var tunnel = !string.IsNullOrWhiteSpace(SiteEnvFile.GetValue(envText, SiteEnvKeys.TunaToken)) &&
                         !string.IsNullOrWhiteSpace(SiteEnvFile.GetValue(envText, SiteEnvKeys.TunaDomain));
            return (new Context(info, envPath, envText, port, tunnel), null);
        }

        private static SiteFailure MissingEnv() => new(SiteFailureKind.Other, "Нет файла настроек",
            "Не найден файл настроек сайта. Запустите установщик HQ Studio ещё раз.", "");

        private Task<SiteProcessResult> ComposeAsync(string docker, Context ctx, Action<string>? onLine,
            CancellationToken ct, params string[] command) =>
            _runner.RunAsync(docker, SiteComposeCommand.Build(ctx.ServerDir, ctx.Tunnel, command), onLine, ct);

        private async Task<bool> EngineRunningAsync(string docker, CancellationToken ct)
        {
            var result = await _runner.RunAsync(docker, new[] { "version", "--format", "{{.Server.Version}}" }, null, ct)
                .ConfigureAwait(false);
            return result.ExitCode == 0;
        }

        private async Task<bool> WaitHealthyAsync(int port, CancellationToken ct)
        {
            var attempts = _timings.Attempts(_timings.HealthWait);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                if (await _probe.IsHealthyAsync(port, ct).ConfigureAwait(false))
                    return true;
                if (attempt < attempts - 1)
                    await _timings.Delay(_timings.PollInterval, ct).ConfigureAwait(false);
            }
            return false;
        }

        private const string TunnelNotUpHint =
            " Адрес в интернете пока не заработал: проверьте токен Tuna и что домен добавлен и подтверждён в my.tuna.am/domains. Подробности: «Логи», служба «Адрес в интернете».";

        /// <summary>
        /// Ждёт, пока контейнер tuna стабильно работает (два опроса подряд): упавший или перезапускающийся контейнер
        /// значит неверный токен или неподтверждённый домен.
        /// </summary>
        private async Task<bool> WaitTunnelAsync(string docker, Context ctx, CancellationToken ct)
        {
            var attempts = _timings.Attempts(_timings.TunnelWait);
            var stable = 0;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var ps = await ComposeAsync(docker, ctx, null, ct, "ps", "--all", "--format", "json").ConfigureAwait(false);
                var tuna = ps.ExitCode == 0
                    ? SiteComposePsParser.Parse(ps.Output).FirstOrDefault(e => e.Service == SiteServiceIds.Tuna)
                    : null;
                stable = tuna is { State: "running" } ? stable + 1 : 0;
                if (stable >= 2)
                    return true;
                if (attempt < attempts - 1)
                    await _timings.Delay(_timings.PollInterval, ct).ConfigureAwait(false);
            }
            return false;
        }

        /// <summary>Приводит PUBLIC_URL в .env к адресу на своём домене: API берёт его для CORS при создании контейнера.</summary>
        private Context SyncPublicUrl(Context ctx)
        {
            if (ctx.Domain.Length == 0)
                return ctx;

            var expected = SiteEnvFile.PublicUrlOf(ctx.Domain);
            if (SiteEnvFile.GetValue(ctx.EnvText, SiteEnvKeys.PublicUrl) == expected)
                return ctx;

            try
            {
                var text = SiteEnvFile.SetValue(ctx.EnvText, SiteEnvKeys.PublicUrl, expected);
                _files.WriteAllTextAtomic(ctx.EnvPath, text);
                _files.WriteAllTextAtomic(SitePaths.PublicUrlFile(ctx.ServerDir), expected);
                return ctx with { EnvText = text };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Адрес для CORS не должен мешать запуску сайта.
                return ctx;
            }
        }

        private static string? PublicUrlOf(Context ctx) =>
            ctx.Domain.Length == 0 ? null : SiteEnvFile.PublicUrlOf(ctx.Domain);

        private static void OnUpLine(string line, Action<string>? status)
        {
            if (status != null && line.Contains("Pull", StringComparison.OrdinalIgnoreCase))
                status("Скачиваю файлы сайта (нужен интернет)");
        }
    }
}
