namespace HQStudio.Setup.UI.Pages;

public sealed record FeatureItem(string IconKey, string Title, string Text);

public sealed class WelcomePageViewModel : PageViewModel
{
    public WelcomePageViewModel(IWizardHost host) : base(host) { }

    public override WizardStep Step => WizardStep.Welcome;
    public override string Title => "Установка HQ Studio";
    public override bool ShowHeader => false;

    public IReadOnlyList<FeatureItem> Features { get; } = new[]
    {
        new FeatureItem("IconMonitor", "Программа", "HQ Studio для работы с клиентами и заказами"),
        new FeatureItem("IconGlobe", "Сайт на компьютере", "Сайт студии, который работает у вас дома или в офисе"),
        new FeatureItem("IconExternal", "Ярлыки", "Значки в меню «Пуск» и на рабочем столе")
    };

    public override void OnEntered()
    {
        Primary.Show("Начать", Host.Next);
        Secondary.Hide();
        Tertiary.Hide();
    }

    public override bool OnEscape() => false;
}
