using System.Collections.ObjectModel;
using HQStudio.Setup.Core;

namespace HQStudio.Setup.UI.Pages;

public sealed class KeysPageViewModel : PageViewModel
{
    public const string GeminiKeyUrl = "https://aistudio.google.com/apikey";
    public const string TunaUrl = "https://tuna.am";
    public const string TunaDomainsUrl = "https://my.tuna.am/domains";
    public const string OwnDomainGuideUrl = "https://tuna.am/docs/tunnels/guides/connect-self-domain";
    public const string PunycodeUrl = "https://www.reg.ru/web-tools/punycode";

    public const string SubdomainHint =
        "На бесплатном тарифе Tuna сама выдаёт имя вида brave-otter-4821: скопируйте его в личном кабинете (my.tuna.am/domains). " +
        "Любое другое имя - только по подписке. Если не уверены, оставьте пустым: адрес будет временным.";

    public const string DomainHint =
        "Если у вас есть свой домен, например crm.example.ru, сайт будет открываться по нему. " +
        "Нужна подписка Tuna: домен сначала добавляют на my.tuna.am/domains и подтверждают в настройках DNS.";

    private string _geminiKey = "";
    private string _tunaToken = "";
    private string _tunaSubdomain = "";
    private string _tunaDomain = "";
    private bool _showAdvanced;

    public KeysPageViewModel(IWizardHost host) : base(host)
    {
        OpenGeminiCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(GeminiKeyUrl));
        OpenTunaCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(TunaUrl));
        OpenTunaDomainsCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(TunaDomainsUrl));
        OpenOwnDomainGuideCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(OwnDomainGuideUrl));
        OpenPunycodeCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(PunycodeUrl));
    }

    public override WizardStep Step => WizardStep.Keys;
    public override string Title => "Ключи";
    public override string Subtitle => "Необязательный шаг. Если ключей нет, нажмите «Пропустить».";

    public RelayCommand OpenGeminiCommand { get; }
    public RelayCommand OpenTunaCommand { get; }
    public RelayCommand OpenTunaDomainsCommand { get; }
    public RelayCommand OpenOwnDomainGuideCommand { get; }
    public RelayCommand OpenPunycodeCommand { get; }

    public string SubdomainHintText => SubdomainHint;
    public string DomainHintText => DomainHint;

    public string GeminiKey
    {
        get => _geminiKey;
        set { if (Set(ref _geminiKey, value)) { RaiseErrors(); UpdatePrimary(); } }
    }

    public string TunaToken
    {
        get => _tunaToken;
        set { if (Set(ref _tunaToken, value)) { RaiseErrors(); UpdatePrimary(); } }
    }

    public string TunaSubdomain
    {
        get => _tunaSubdomain;
        set
        {
            if (!Set(ref _tunaSubdomain, value))
                return;
            if (!string.IsNullOrWhiteSpace(value))
                ShowAdvanced = true;
            RaiseErrors();
            UpdatePrimary();
        }
    }

    public string TunaDomain
    {
        get => _tunaDomain;
        set
        {
            if (!Set(ref _tunaDomain, value))
                return;
            if (!string.IsNullOrWhiteSpace(value))
                ShowAdvanced = true;
            RaiseErrors();
            UpdatePrimary();
        }
    }

    /// <summary>The "own name or domain" block is folded away until it is needed.</summary>
    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set => Set(ref _showAdvanced, value);
    }

    private bool HasDomainText => !string.IsNullOrWhiteSpace(TunaDomain);

    /// <summary>With an own domain the subdomain is not used, so its field is switched off.</summary>
    public bool SubdomainEnabled => !HasDomainText;

    public string? GeminiError => Validators.Key(GeminiKey, "ключе");

    public string? TunaTokenError =>
        Validators.Key(TunaToken, "токене")
        ?? (HasDomainText && Validators.Domain(TunaDomain) == null && string.IsNullOrWhiteSpace(TunaToken)
            ? "Чтобы подключить свой домен, нужен токен Tuna"
            : null);

    public string? TunaSubdomainError => HasDomainText ? null : Validators.Subdomain(TunaSubdomain);
    public string? TunaDomainError => Validators.Domain(TunaDomain);

    public bool HasGeminiError => GeminiError != null;
    public bool HasTunaTokenError => TunaTokenError != null;
    public bool HasTunaSubdomainError => TunaSubdomainError != null;
    public bool HasTunaDomainError => TunaDomainError != null;

    /// <summary>Non-ASCII letters were typed: offer the punycode converter.</summary>
    public bool DomainNeedsPunycode => HasTunaDomainError && Validators.IsNonAsciiDomain(TunaDomain);

    public bool IsValid =>
        GeminiError == null && TunaTokenError == null && TunaSubdomainError == null && TunaDomainError == null;

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(GeminiKey) && string.IsNullOrWhiteSpace(TunaToken)
        && string.IsNullOrWhiteSpace(TunaSubdomain) && string.IsNullOrWhiteSpace(TunaDomain);

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
        {
            if (HasTunaSubdomainError || HasTunaDomainError)
                ShowAdvanced = true;
            return;
        }

        var answers = Host.Answers;
        answers.GeminiKey = GeminiKey.Trim();
        answers.TunaToken = TunaToken.Trim();
        answers.TunaDomain = Validators.NormalizeDomain(TunaDomain);
        answers.TunaSubdomain = HasDomainText ? "" : TunaSubdomain.Trim().ToLowerInvariant();
        Host.Next();
    }

    private void RaiseErrors()
    {
        Raise(nameof(GeminiError));
        Raise(nameof(TunaTokenError));
        Raise(nameof(TunaSubdomainError));
        Raise(nameof(TunaDomainError));
        Raise(nameof(HasGeminiError));
        Raise(nameof(HasTunaTokenError));
        Raise(nameof(HasTunaSubdomainError));
        Raise(nameof(HasTunaDomainError));
        Raise(nameof(DomainNeedsPunycode));
        Raise(nameof(SubdomainEnabled));
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

    internal static string PublicAddressText(InstallAnswers a)
    {
        if (string.IsNullOrWhiteSpace(a.TunaToken))
            return "не нужен, сайт виден только на этом компьютере";
        if (!string.IsNullOrWhiteSpace(a.TunaDomain))
            return $"свой домен {Validators.NormalizeDomain(a.TunaDomain)} через Tuna";
        return string.IsNullOrWhiteSpace(a.TunaSubdomain)
            ? "будет создан через Tuna"
            : $"будет создан через Tuna ({a.TunaSubdomain.ToLowerInvariant()})";
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
        Rows.Add(new SummaryRow("ИИ на сайте", string.IsNullOrWhiteSpace(a.GeminiKey) ? "не подключён, можно добавить позже" : "будет включён (ключ Gemini указан)"));
        Rows.Add(new SummaryRow("Адрес для всех", PublicAddressText(a)));
    }
}
