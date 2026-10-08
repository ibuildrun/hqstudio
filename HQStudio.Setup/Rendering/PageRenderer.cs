using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HQStudio.Setup.Core;
using HQStudio.Setup.Services;
using HQStudio.Setup.UI;
using HQStudio.Setup.UI.Pages;

namespace HQStudio.Setup.Rendering;

/// <summary>--render-pages: draws every wizard page in its main states to PNG files at 96 dpi and returns an exit code.</summary>
public static class PageRenderer
{
    private const int Width = 884;
    private const int Height = 624;

    private sealed record Scenario(string Name, Action<WizardViewModel> Setup);

    // Pages taller than the window are shown scrolled to the bottom, to review the part that is below the fold.
    private static readonly HashSet<string> ScrolledToEnd = new();

    public static int RenderAll(string directory)
    {
        Ui.AnimationsEnabled = false;
        InstallPageViewModel.SuccessPause = TimeSpan.Zero;
        Directory.CreateDirectory(directory);

        var sandbox = Path.Combine(Path.GetTempPath(), "HQStudio-Setup-Render", Guid.NewGuid().ToString("N"));
        var options = SetupOptions.Parse(new[] { "--simulate" });
        var failures = 0;

        foreach (var scenario in Scenarios())
        {
            try
            {
                var services = ServiceFactory.CreateSimulated(options, _ => { }, sandbox);
                var vm = new WizardViewModel(services, options, () => { });
                scenario.Setup(vm);
                Save(vm, Path.Combine(directory, scenario.Name + ".png"), ScrolledToEnd.Contains(scenario.Name));
            }
            catch (Exception ex)
            {
                failures++;
                File.WriteAllText(Path.Combine(directory, scenario.Name + ".error.txt"), ex.ToString());
            }
        }

        try { Directory.Delete(sandbox, recursive: true); } catch (IOException) { }
        return failures == 0 ? 0 : 1;
    }

    private static ScrollViewer? FindScroller(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer { ScrollableHeight: > 0 } viewer)
                return viewer;
            if (FindScroller(child) is { } nested)
                return nested;
        }
        return null;
    }

    private static void Save(WizardViewModel vm, string path, bool scrollToEnd = false)
    {
        var shell = new ShellView { DataContext = vm, Width = Width, Height = Height };
        var host = new Border
        {
            Width = Width,
            Height = Height,
            Background = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x28)),
            Child = shell
        };

        var size = new Size(Width, Height);
        host.Measure(size);
        host.Arrange(new Rect(size));
        host.UpdateLayout();

        if (scrollToEnd && FindScroller(host) is { } viewer)
        {
            viewer.ScrollToVerticalOffset(viewer.ScrollableHeight);
            host.UpdateLayout();
        }

        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(host);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void Fill(InstallAnswers a, bool tuna = true)
    {
        a.FirstName = "Иван";
        a.LastName = "Петров";
        a.Password = "Sunny-Day-2026";
        a.TunaToken = tuna ? "tuna_example_token" : "";
        a.TunaDomain = tuna ? "crm.example.ru" : "";
    }

    private static IReadOnlyList<(StageId, StageState, string?)> Stages(StageState prepare, StageState configure, StageState docker,
        StageState pull, string? pullDetail, StageState start, StageState health, StageState publicUrl, StageState shortcuts) => new[]
    {
        (StageId.Prepare, prepare, (string?)null),
        (StageId.Configure, configure, null),
        (StageId.DockerReady, docker, null),
        (StageId.Pull, pull, pullDetail),
        (StageId.Start, start, null),
        (StageId.Health, health, null),
        (StageId.PublicUrl, publicUrl, null),
        (StageId.Shortcuts, shortcuts, null)
    };

    private static readonly string[] SampleLog =
    {
        "[12:41:07] HQ Studio Setup 1.20.0",
        "[12:41:08] == Подготовка и копирование программы",
        "[12:41:09] Файлов программы: 2, файлов сайта: 3",
        "[12:41:09] == Настройка сайта",
        "[12:41:10] Порт сайта: 8080",
        "[12:41:10] Созданы новые секреты базы данных.",
        "[12:41:10] == Запуск Docker",
        "[12:41:11] == Скачивание сайта",
        " Image postgres:16-alpine Pulling",
        " Image ghcr.io/ibuildrun/hqstudio/api:1.20.0 Pulling",
        " 3c6d4a1b9e2f Pulling fs layer",
        " 3c6d4a1b9e2f Downloading [=====>   ]  14.1MB/29.1MB",
        " 3c6d4a1b9e2f Pull complete",
        " 8d2f1a7c5b3e Downloading [==>      ]  18.4MB/80.0MB"
    };

    private static IEnumerable<Scenario> Scenarios()
    {
        yield return new("01-welcome", vm => vm.ShowForPreview(vm.Welcome));

        yield return new("02-docker-checking", vm =>
        {
            vm.Docker.State = DockerPageState.Checking;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("02-docker-running", vm =>
        {
            vm.Docker.State = DockerPageState.Running;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("03-docker-stopped", vm =>
        {
            vm.Docker.State = DockerPageState.Stopped;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("04-docker-missing", vm =>
        {
            vm.Docker.State = DockerPageState.Missing;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("05-docker-downloading", vm =>
        {
            vm.Docker.State = DockerPageState.Downloading;
            vm.Docker.DownloadPercent = 37;
            vm.Docker.DownloadText = "Скачано 226 МБ из 612 МБ (37%)";
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("06-docker-installing", vm =>
        {
            vm.Docker.State = DockerPageState.Installing;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("06-docker-starting-engine", vm =>
        {
            vm.Docker.State = DockerPageState.StartingEngine;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("07-docker-reboot", vm =>
        {
            vm.Docker.State = DockerPageState.RebootRequired;
            vm.ShowForPreview(vm.Docker);
        });
        yield return new("08-docker-missing-error-skip", vm =>
        {
            vm.Docker.State = DockerPageState.Missing;
            vm.Docker.ErrorText = "Не удалось скачать Docker. Проверьте интернет (если сайт docker.com не открывается, включите VPN) или скачайте Docker вручную.";
            vm.Docker.SkipDocker = true;
            vm.ShowForPreview(vm.Docker);
        });

        yield return new("09-account-empty", vm => vm.ShowForPreview(vm.Account));
        yield return new("10-account-errors", vm =>
        {
            vm.Account.LastName = "Петров";
            vm.Account.Password = "12345";
            vm.Account.PasswordRepeat = "1234";
            vm.Account.ShowAllErrors();
            vm.ShowForPreview(vm.Account);
        });
        yield return new("11-account-filled", vm =>
        {
            vm.Account.FirstName = "Иван";
            vm.Account.LastName = "Петров";
            vm.Account.Password = "Sunny-Day-2026";
            vm.Account.PasswordRepeat = "Sunny-Day-2026";
            vm.ShowForPreview(vm.Account);
        });

        yield return new("12-keys-empty", vm => vm.ShowForPreview(vm.Keys));
        yield return new("13-keys-filled-error", vm =>
        {
            vm.Keys.TunaToken = "tuna_example_token";
            vm.Keys.TunaDomain = "https://crm.example.ru/login";
            vm.ShowForPreview(vm.Keys);
        });
        yield return new("13b-keys-domain-valid", vm =>
        {
            vm.Keys.TunaToken = "tuna_example_token";
            vm.Keys.TunaDomain = "crm.example.ru";
            vm.ShowForPreview(vm.Keys);
        });
        yield return new("13c-keys-domain-non-ascii", vm =>
        {
            vm.Keys.TunaToken = "tuna_example_token";
            vm.Keys.TunaDomain = "кафе.рф";
            vm.ShowForPreview(vm.Keys);
        });
        yield return new("13d-keys-domain-invalid", vm =>
        {
            vm.Keys.TunaDomain = "crm";
            vm.ShowForPreview(vm.Keys);
        });
        yield return new("13e-keys-domain-needs-token", vm =>
        {
            vm.Keys.TunaDomain = "crm.example.ru";
            vm.ShowForPreview(vm.Keys);
        });

        yield return new("14b-summary-no-domain", vm =>
        {
            Fill(vm.Answers, tuna: false);
            vm.ShowForPreview(vm.Summary);
        });

        yield return new("14-summary", vm =>
        {
            Fill(vm.Answers);
            vm.ShowForPreview(vm.Summary);
        });
        yield return new("15-summary-site-later", vm =>
        {
            vm.Answers.SkipSite = true;
            vm.ShowForPreview(vm.Summary);
        });

        yield return new("16-installing-60", vm =>
        {
            Fill(vm.Answers);
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Running,
                    "Скачано частей: 11 из 26 · 214 МБ из 489 МБ",
                    StageState.Pending, StageState.Pending, StageState.Pending, StageState.Pending),
                60, "Шаг 4 из 8: Скачивание сайта", InstallRunState.Running);
            vm.ShowForPreview(vm.Install);
        });
        yield return new("17-installing-log-open", vm =>
        {
            Fill(vm.Answers);
            vm.Install.IsLogExpanded = true;
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Running,
                    "Скачано частей: 11 из 26 · 214 МБ из 489 МБ",
                    StageState.Pending, StageState.Pending, StageState.Pending, StageState.Pending),
                60, "Шаг 4 из 8: Скачивание сайта", InstallRunState.Running, log: SampleLog);
            vm.ShowForPreview(vm.Install);
        });
        yield return new("18-failed-network", vm =>
        {
            Fill(vm.Answers);
            var failure = FailureAnalyzer.Analyze(StageId.Pull, new InstallException(FailureKind.Network, "docker compose pull failed"));
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Failed, null,
                    StageState.Pending, StageState.Pending, StageState.Pending, StageState.Pending),
                44, "Нет доступа к сайту загрузки", InstallRunState.Failed, failure);
            vm.ShowForPreview(vm.Install);
        });
        yield return new("19-failed-port", vm =>
        {
            Fill(vm.Answers);
            var failure = FailureAnalyzer.Analyze(StageId.Start, new InstallException(FailureKind.PortBusy, "No free port left in the range"));
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Done, null,
                    StageState.Failed, StageState.Pending, StageState.Pending, StageState.Pending),
                72, "Адрес сайта занят", InstallRunState.Failed, failure);
            vm.ShowForPreview(vm.Install);
        });
        yield return new("20-installing-warning-nearly-done", vm =>
        {
            Fill(vm.Answers);
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Done, null,
                    StageState.Done, StageState.Done, StageState.Warning, StageState.Running),
                96, "Шаг 8 из 8: Ярлыки", InstallRunState.Running);
            vm.Install.Stages[6].Preview(StageState.Warning, "Свой домен заработает, когда вы добавите его на my.tuna.am/domains и подтвердите DNS.");
            vm.ShowForPreview(vm.Install);
        });

        yield return new("21-done", vm =>
        {
            vm.Done.LoadPreview(true, "http://localhost:8080", null, null, false);
            vm.ShowForPreview(vm.Done);
        });
        yield return new("22-done-public", vm =>
        {
            vm.Done.LoadPreview(true, "http://localhost:8081", "https://crm.example.ru", null, false);
            vm.ShowForPreview(vm.Done);
        });
        yield return new("22b-done-domain-pending", vm =>
        {
            vm.Done.LoadPreview(true, "http://localhost:8080", "https://crm.example.ru",
                "Свой домен заработает, когда вы добавите его на my.tuna.am/domains и подтвердите DNS.", false);
            vm.ShowForPreview(vm.Done);
        });
        yield return new("23-done-site-later", vm =>
        {
            vm.Done.LoadPreview(false, "http://localhost:8080", null, null, false);
            vm.ShowForPreview(vm.Done);
        });
        yield return new("24-confirm-close", vm =>
        {
            Fill(vm.Answers);
            vm.Install.LoadPreview(
                Stages(StageState.Done, StageState.Done, StageState.Done, StageState.Running,
                    "Скачано частей: 11 из 26 · 214 МБ из 489 МБ",
                    StageState.Pending, StageState.Pending, StageState.Pending, StageState.Pending),
                60, "Шаг 4 из 8: Скачивание сайта", InstallRunState.Running);
            vm.ShowForPreview(vm.Install);
            vm.RequestClose();
        });
    }
}
