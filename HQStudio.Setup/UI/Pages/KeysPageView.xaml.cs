using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HQStudio.Setup.UI.Pages;

public partial class KeysPageView : UserControl
{
    public KeysPageView() => InitializeComponent();

    // The opened block lies below the fold; scroll so that it is visible.
    private void OwnAddress_Expanded(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => Scroller.ScrollToEnd());
}
