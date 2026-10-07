using System.Collections.ObjectModel;
using HQStudio.Setup.Core;

namespace HQStudio.Setup.UI.Pages;

public sealed class KeysPageViewModel : PageViewModel
{
    public const string GeminiKeyUrl = "https://aistudio.google.com/apikey";
    public const string TunaUrl = "https://tuna.am";

    private string _geminiKey = "";
    private string _tunaToken = "";
    private string _tunaSubdomain = "";

    public KeysPageViewModel(IWizardHost host) : base(host)
    {
        OpenGeminiCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(GeminiKeyUrl));
        OpenTunaCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(TunaUrl));
    }

    public override WizardStep Step => WizardStep.Keys;
    public override string Title => "Ключи";
    public override string Subtitle => "Необязательный шаг. Если ключей нет, нажмите «Пропустить».";

    public RelayCommand OpenGeminiCommand { get; }
    public RelayCommand OpenTunaCommand { get; }

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
        set { if (Set(ref _tunaSubdomain, value)) { RaiseErrors(); UpdatePrimary(); } }
    }

    public string? GeminiError => Validators.Key(GeminiKey, "ключе");
    public string? TunaTokenError => Validators.Key(TunaToken, "токене");
    public string? TunaSubdomainError => Validators.Subdomain(TunaSubdomain);

    public bool HasGeminiError => GeminiError != null;
    public bool HasTunaTokenError => TunaTokenError != null;
    public bool HasTunaSubdomainError => TunaSubdomainError != null;

    public bool IsValid => GeminiError == null && TunaTokenError == null && TunaSubdomainError == null;

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(GeminiKey) && string.IsNullOrWhiteSpace(TunaToken) && string.IsNullOrWhiteSpace(TunaSubdomain);

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
        answers.GeminiKey = GeminiKey.Trim();
        answers.TunaToken = TunaToken.Trim();
        answers.TunaSubdomain = TunaSubdomain.Trim().ToLowerInvariant();
        Host.Next();
    }

    private void RaiseErrors()
    {
        Raise(nameof(GeminiError));
        Raise(nameof(TunaTokenError));
        Raise(nameof(TunaSubdomainError));
        Raise(nameof(HasGeminiError));
        Raise(nameof(HasTunaTokenError));
        Raise(nameof(HasTunaSubdomainError));
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
        Rows.Add(new SummaryRow("Адрес для всех", string.IsNullOrWhiteSpace(a.TunaToken)
            ? "не нужен, сайт виден только на этом компьютере"
            : string.IsNullOrWhiteSpace(a.TunaSubdomain) ? "будет создан через Tuna" : $"будет создан через Tuna ({a.TunaSubdomain.ToLowerInvariant()})"));
    }
}
