using System.Collections.ObjectModel;
using HQStudio.Setup.Core;

namespace HQStudio.Setup.UI.Pages;

public sealed class KeysPageViewModel : PageViewModel
{
    public const string TunaUrl = "https://tuna.am";
    public const string PunycodeUrl = "https://www.reg.ru/web-tools/punycode";

    public const string TokenHint =
        "Нужен вместе с доменом: так сайт откроют клиенты из любого места. Для своего домена нужна подписка Tuna.";

    public const string DomainHint =
        "Домен можно не вводить сейчас: подключить его можно позже в самой программе, раздел «Сайт» - «Инструкция». " +
        "Без домена сайт работает только на этом компьютере.";

    private string _tunaToken = "";
    private string _tunaDomain = "";

    public KeysPageViewModel(IWizardHost host) : base(host)
    {
        OpenTunaCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(TunaUrl));
        OpenPunycodeCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(PunycodeUrl));
    }

    public override WizardStep Step => WizardStep.Keys;
    public override string Title => "Адрес сайта в интернете";
    public override string Subtitle => "Необязательный шаг. Если домена или токена нет, нажмите «Пропустить».";

    public RelayCommand OpenTunaCommand { get; }
    public RelayCommand OpenPunycodeCommand { get; }

    public string TokenHintText => TokenHint;
    public string DomainHintText => DomainHint;

    public string TunaToken
    {
        get => _tunaToken;
        set { if (Set(ref _tunaToken, value)) { RaiseErrors(); UpdatePrimary(); } }
    }

    public string TunaDomain
    {
        get => _tunaDomain;
        set { if (Set(ref _tunaDomain, value)) { RaiseErrors(); UpdatePrimary(); } }
    }

    private bool HasDomainText => !string.IsNullOrWhiteSpace(TunaDomain);

    public string? TunaTokenError =>
        Validators.Key(TunaToken, "токене")
        ?? (HasDomainText && Validators.Domain(TunaDomain) == null && string.IsNullOrWhiteSpace(TunaToken)
            ? "Чтобы подключить свой домен, нужен токен Tuna"
            : null);

    public string? TunaDomainError => Validators.Domain(TunaDomain);

    public bool HasTunaTokenError => TunaTokenError != null;
    public bool HasTunaDomainError => TunaDomainError != null;

    /// <summary>Non-ASCII letters were typed: offer the punycode converter.</summary>
    public bool DomainNeedsPunycode => HasTunaDomainError && Validators.IsNonAsciiDomain(TunaDomain);

    public bool IsValid => TunaTokenError == null && TunaDomainError == null;

    public bool IsEmpty => string.IsNullOrWhiteSpace(TunaToken) && string.IsNullOrWhiteSpace(TunaDomain);

    public override void OnEntered()
    {
        UpdatePrimary();
        Secondary.Show("Назад", Host.Back);
        Tertiary.Hide();
    }

    private void UpdatePrimary() => Primary.Show(IsEmpty ? "Пропустить" : "Далее", TryAdvance);

    public void TryAdvance()
    {
        RaiseErrors();
        if (!IsValid)
            return;

        var answers = Host.Answers;
        answers.TunaToken = TunaToken.Trim();
        answers.TunaDomain = Validators.NormalizeDomain(TunaDomain);
        Host.Next();
    }

    private void RaiseErrors()
    {
        Raise(nameof(TunaTokenError));
        Raise(nameof(TunaDomainError));
        Raise(nameof(HasTunaTokenError));
        Raise(nameof(HasTunaDomainError));
        Raise(nameof(DomainNeedsPunycode));
        Raise(nameof(IsEmpty));
    }
}

public sealed record SummaryRow(string Label, string Value, bool IsWarning = false);

public sealed class SummaryPageViewModel : PageViewModel
{
    private bool _desktopShortcut = true;

    public SummaryPageViewModel(IWizardHost host) : base(host) { }

    public override WizardStep Step => WizardStep.Summary;
    public override string Title => "Проверьте и установите";
    public override string Subtitle => "Вот что сейчас произойдёт. Если всё верно, нажмите «Установить».";

    public ObservableCollection<SummaryRow> Rows { get; } = new();

    public bool DesktopShortcut
    {
        get => _desktopShortcut;
        set
        {
            if (Set(ref _desktopShortcut, value))
                Host.Answers.DesktopShortcut = value;
        }
    }

    public string Note => Host.Answers.SkipSite
        ? "Установка займёт около минуты."
        : "Установка займёт 5-10 минут и потребует интернет. Окно закрывать не нужно.";

    public override void OnEntered()
    {
        Host.Answers.DesktopShortcut = DesktopShortcut;
        BuildRows();
        Raise(nameof(Note));
        Primary.Show("Установить", Host.Next);
        Secondary.Show("Назад", Host.Back);
        Tertiary.Hide();
    }

    // The tunnel needs both the token and the domain, so only then is there an address for everyone.
    internal static string PublicAddressText(InstallAnswers a)
    {
        var domain = Validators.NormalizeDomain(a.TunaDomain);
        if (string.IsNullOrWhiteSpace(a.TunaToken) || domain.Length == 0)
            return "не задан, сайт виден только на этом компьютере";
        return EnvPlanner.PublicUrlFor(domain);
    }

    internal void BuildRows()
    {
        var a = Host.Answers;
        var paths = Host.Services.Paths;
        Rows.Clear();

        Rows.Add(new SummaryRow("Папка программы", paths.AppDir));

        if (a.SkipSite)
        {
            Rows.Add(new SummaryRow("Сайт на компьютере", "будет установлен позже, когда вы поставите Docker", IsWarning: true));
            return;
        }

        Rows.Add(new SummaryRow("Папка сайта", paths.ServerDir));
        Rows.Add(new SummaryRow("Сайт на компьютере", $"http://localhost:{PortSelector.First}"));
        Rows.Add(new SummaryRow("Главный аккаунт", $"{a.AdminName}, логин: admin"));
        Rows.Add(new SummaryRow("Адрес для всех", PublicAddressText(a)));
    }
}
