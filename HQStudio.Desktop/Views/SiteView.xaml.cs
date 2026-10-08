using System.Windows;
using System.Windows.Controls;
using HQStudio.Services;
using HQStudio.Services.Guide;
using HQStudio.Services.Site;
using HQStudio.ViewModels;
using HQStudio.Views.Dialogs;

namespace HQStudio.Views
{
    public partial class SiteView : UserControl
    {
        private Window? _hostWindow;

        public SiteView()
        {
            InitializeComponent();
            IsVisibleChanged += (_, _) => UpdateActivation();
        }

        private SiteViewModel? ViewModel => DataContext as SiteViewModel;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _hostWindow = Window.GetWindow(this);
            if (_hostWindow != null)
            {
                _hostWindow.StateChanged -= HostWindow_StateChanged;
                _hostWindow.StateChanged += HostWindow_StateChanged;
            }
            UpdateActivation();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_hostWindow != null)
                _hostWindow.StateChanged -= HostWindow_StateChanged;
            _hostWindow = null;
            ViewModel?.Deactivate();
        }

        private void HostWindow_StateChanged(object? sender, EventArgs e) => UpdateActivation();

        // Опрос идёт, только пока страницу действительно видно: не свёрнуто окно и не открыта другая страница.
        private void UpdateActivation()
        {
            var vm = ViewModel;
            if (vm == null)
                return;

            var visible = IsLoaded && IsVisible && _hostWindow?.WindowState != WindowState.Minimized;
            if (visible)
                vm.Activate();
            else
                vm.Deactivate();
        }

        private void DismissError_Click(object sender, RoutedEventArgs e) => ViewModel?.DismissError();
    }

    /// <summary>Открывает окна страницы «Сайт» поверх главного окна.</summary>
    public sealed class WpfSiteDialogs : ISiteDialogs
    {
        private static Window? Owner => Application.Current?.MainWindow;

        // Из окна «Ключи» инструкция открывается поверх него, а не за ним.
        private static Window? ActiveOwner =>
            Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Owner;

        public void ShowKeys(ISiteService service, Action? openGuide)
        {
            var vm = new SiteKeysViewModel(service, new WindowsSiteShell(), openGuide);
            new SiteKeysDialog(vm) { Owner = Owner }.ShowDialog();
        }

        public void ShowGuide(string? sectionId)
        {
            // Вторая проверка на случай, если окно попытаются открыть мимо страницы «Сайт».
            if (!AdminAccess.IsCurrentUserAdmin())
                return;

            var vm = new SiteGuideViewModel(SiteGuideContent.Sections, new WindowsSiteShell(), new ToastSiteNotifier(), sectionId);
            new SiteGuideDialog(vm) { Owner = ActiveOwner }.ShowDialog();
        }

        public void ShowLogs(ISiteService service)
        {
            var vm = new SiteLogsViewModel(service, new WindowsSiteShell(), new ToastSiteNotifier());
            new SiteLogsDialog(vm) { Owner = Owner }.ShowDialog();
        }

        public void ShowUpdates() => new UpdateDialog { Owner = Owner }.ShowDialog();

        public bool ConfirmUninstall() => ConfirmDialog.Show(
            "Удалить HQ Studio?",
            "Программа закроется, и откроется окно удаления. Там можно выбрать, оставить ли данные сайта (клиенты и заказы). Продолжить?",
            ConfirmDialog.DialogType.Warning, "Продолжить", "Отмена", Owner);
    }
}
