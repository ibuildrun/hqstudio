using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ClientsViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private Client? _selectedClient;
        private string _searchText = string.Empty;

        public ObservableCollection<Client> Clients { get; } = new();

        public Client? SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterClients();
            }
        }

        public ICommand AddClientCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand DeleteClientCommand { get; }

        public ClientsViewModel()
        {
            AddClientCommand = new RelayCommand(_ => AddClient());
            EditClientCommand = new RelayCommand(_ => EditClient(), _ => SelectedClient != null);
            DeleteClientCommand = new RelayCommand(_ => DeleteClient(), _ => SelectedClient != null);
            LoadClients();
        }

        private void LoadClients()
        {
            Clients.Clear();
            foreach (var client in _dataService.Clients.OrderByDescending(c => c.CreatedAt))
            {
                Clients.Add(client);
            }
        }

        private void FilterClients()
        {
            Clients.Clear();
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _dataService.Clients
                : _dataService.Clients.Where(c =>
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Car.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CarNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var client in filtered.OrderByDescending(c => c.CreatedAt))
            {
                Clients.Add(client);
            }
        }

        private void AddClient()
        {
            var dialog = new EditClientDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                dialog.Client.Id = _dataService.GetNextId(_dataService.Clients);
                dialog.Client.CreatedAt = DateTime.Now;
                _dataService.Clients.Add(dialog.Client);
                _dataService.SaveData();
                LoadClients();
            }
        }

        private void EditClient()
        {
            if (SelectedClient == null) return;
            
            var dialog = new EditClientDialog(SelectedClient);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                LoadClients();
            }
        }

        private void DeleteClient()
        {
            if (SelectedClient == null) return;
            
            var result = MessageBox.Show(
                $"Удалить клиента \"{SelectedClient.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _dataService.Clients.Remove(SelectedClient);
                _dataService.SaveData();
                LoadClients();
            }
        }
    }
}
