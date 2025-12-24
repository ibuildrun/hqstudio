using HQStudio.Models;
using HQStudio.ViewModels;
using HQStudio.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HQStudio.Views
{
    public partial class ClientsView : UserControl
    {
        private Border? _lastSelectedIndicator;
        
        public ClientsView()
        {
            InitializeComponent();
        }

        private void Client_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cardBorder && cardBorder.DataContext is Client client)
            {
                if (DataContext is ClientsViewModel vm)
                {
                    // Сбрасываем предыдущее выделение
                    if (_lastSelectedIndicator != null)
                    {
                        _lastSelectedIndicator.Background = Brushes.Transparent;
                    }
                    
                    // Находим индикатор в текущей карточке
                    var grid = cardBorder.Child as Grid;
                    if (grid != null)
                    {
                        var indicator = grid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "SelectionIndicator");
                        if (indicator != null)
                        {
                            indicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                            _lastSelectedIndicator = indicator;
                        }
                    }
                    
                    // Устанавливаем выбранного клиента
                    vm.SelectedClient = client;
                    
                    // Двойной клик - редактирование
                    if (e.ClickCount == 2)
                    {
                        var dialog = new EditClientDialog(client);
                        dialog.Owner = Window.GetWindow(this);
                        
                        if (dialog.ShowDialog() == true)
                        {
                            Services.DataService.Instance.SaveData();
                            var search = vm.SearchText;
                            vm.SearchText = "";
                            vm.SearchText = search;
                        }
                    }
                }
            }
        }
    }
}
