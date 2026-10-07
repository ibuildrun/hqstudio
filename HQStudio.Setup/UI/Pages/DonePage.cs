using HQStudio.Setup.Core;
using HQStudio.Setup.Install;

namespace HQStudio.Setup.UI.Pages;

public sealed class DonePageViewModel : PageViewModel
{
    private string _localUrl = AppConfigWriter.LocalUrl(PortSelector.First);
    private string? _publicUrl;
    private string? _publicUrlNote;
    private bool _siteReady = true;
    private bool _keptDatabase;
    private bool _copied;

    public DonePageViewModel(IWizardHost host) : base(host)
    {
        OpenSiteCommand = new RelayCommand(() => Host.Services.Shell.OpenUrl(LocalUrl));
        OpenPublicCommand = new RelayCommand(() =>
        {
            if (PublicUrl != null)
                Host.Services.Shell.OpenUrl(PublicUrl);
        });
        CopyPublicCommand = new RelayCommand(CopyPublic);
    }

    public override WizardStep Step => WizardStep.Done;
    public override string Title => SiteReady ? "Всё готово" : "Программа установлена";
    public override bool ShowHeader => false;

    public RelayCommand OpenSiteCommand { get; }
    public RelayCommand OpenPublicCommand { get; }
    public RelayCommand CopyPublicCommand { get; }

    public string Headline => SiteReady ? "HQ Studio установлена" : "Программа установлена";

    public string Lead => SiteReady
        ? "Программа и сайт работают на этом компьютере."
        : "Сайт пока не установлен, потому что Docker будет установлен позже.";

    public bool SiteReady
    {
        get => _siteReady;
        private set
        {
            if (Set(ref _siteReady, value))
            {
                Raise(nameof(Headline));
                Raise(nameof(Lead));
                Raise(nameof(Title));
                Raise(nameof(SiteSkipped));
            }
        }
    }

    public bool SiteSkipped => !SiteReady;

    public string LocalUrl
    {
        get => _localUrl;
        private set => Set(ref _localUrl, value);
    }

    public string? PublicUrl
    {
        get => _publicUrl;
        private set
        {
            if (Set(ref _publicUrl, value))
                Raise(nameof(HasPublicUrl));
        }
    }

    public bool HasPublicUrl => !string.IsNullOrEmpty(PublicUrl);

    public string? PublicUrlNote
    {
        get => _publicUrlNote;
        private set
        {
            if (Set(ref _publicUrlNote, value))
                Raise(nameof(HasPublicUrlNote));
        }
    }

    public bool HasPublicUrlNote => !string.IsNullOrEmpty(PublicUrlNote);

    public bool KeptDatabase
    {
        get => _keptDatabase;
        private set => Set(ref _keptDatabase, value);
    }

    public bool Copied
    {
        get => _copied;
        private set
        {
            if (Set(ref _copied, value))
                Raise(nameof(CopyText));
        }
    }

    public string CopyText => Copied ? "Скопировано" : "Копировать";

    public string LoginHint => KeptDatabase
        ? "Для входа: логин admin. Сайт уже был установлен, поэтому пароль остался прежним."
        : "Для входа: логин admin и пароль, который вы задали.";

    public string SkippedHint =>
        "Когда установите Docker, откройте HQ Studio, перейдите на страницу «Сайт» и завершите установку сайта.";

    public void Load(InstallContext context)
    {
        SiteReady = context.SiteInstalled;
        LocalUrl = AppConfigWriter.LocalUrl(context.Port);
        PublicUrl = context.PublicUrl;
        PublicUrlNote = context.PublicUrlNote;
        KeptDatabase = context.KeptExistingDatabase;
        Raise(nameof(LoginHint));
    }

    public override void OnEntered()
    {
        Primary.Show("Запустить HQ Studio", LaunchApp);
        Secondary.Show("Закрыть", Host.CloseNow);
        Tertiary.Hide();
    }

    public override bool OnEscape()
    {
        Host.CloseNow();
        return true;
    }

    private void LaunchApp()
    {
        Host.Services.Shell.Launch(Host.Services.Paths.AppExe);
        Host.CloseNow();
    }

    private void CopyPublic()
    {
        if (PublicUrl == null)
            return;

        Host.CopyToClipboard(PublicUrl);
        Copied = true;
        _ = ResetCopiedAsync();
    }

    private async Task ResetCopiedAsync()
    {
        await Task.Delay(2000);
        Copied = false;
    }

    internal void LoadPreview(bool siteReady, string localUrl, string? publicUrl, string? note, bool keptDatabase)
    {
        SiteReady = siteReady;
        LocalUrl = localUrl;
        PublicUrl = publicUrl;
        PublicUrlNote = note;
        KeptDatabase = keptDatabase;
        Raise(nameof(LoginHint));
    }
}
