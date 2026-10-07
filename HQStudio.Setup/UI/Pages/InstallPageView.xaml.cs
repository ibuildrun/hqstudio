using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace HQStudio.Setup.UI.Pages;

public partial class InstallPageView : UserControl
{
    private InstallPageViewModel? _vm;

    public InstallPageView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => Attach(e.NewValue as InstallPageViewModel);
        Unloaded += (_, _) => Detach();
    }

    private void Attach(InstallPageViewModel? vm)
    {
        Detach();
        _vm = vm;
        if (_vm == null)
            return;

        _vm.LogAppended += OnLogAppended;
        _vm.Stages.CollectionChanged += OnStagesChanged;
        foreach (var stage in _vm.Stages)
            stage.PropertyChanged += OnStagePropertyChanged;

        LogBox.Text = string.Join(Environment.NewLine, _vm.LogSnapshot) + (_vm.LogSnapshot.Count > 0 ? Environment.NewLine : "");
        LogBox.ScrollToEnd();
    }

    private void Detach()
    {
        if (_vm == null)
            return;
        _vm.LogAppended -= OnLogAppended;
        _vm.Stages.CollectionChanged -= OnStagesChanged;
        foreach (var stage in _vm.Stages)
            stage.PropertyChanged -= OnStagePropertyChanged;
        _vm = null;
    }

    private void OnStagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
            return;
        foreach (StageViewModel stage in e.NewItems)
            stage.PropertyChanged += OnStagePropertyChanged;
    }

    // The running stage is kept in view when the list is scrolled (log expanded, small window).
    private void OnStagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StageViewModel.IsRunning) && e.PropertyName != nameof(StageViewModel.IsFailed))
            return;
        if (sender is not StageViewModel { IsRunning: true } and not StageViewModel { IsFailed: true })
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (StageList.ItemContainerGenerator.ContainerFromItem(sender) is FrameworkElement element)
                element.BringIntoView();
        });
    }

    private void OnLogAppended(string line)
    {
        LogBox.AppendText(line + Environment.NewLine);
        LogBox.ScrollToEnd();
    }
}
