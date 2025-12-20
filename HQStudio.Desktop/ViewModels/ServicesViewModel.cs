using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ServicesViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private Service? _selectedService;
        private string _searchText = string.Empty;

        public ObservableCollection<Service> Services { get; } = new();
        
        public Service? SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterServices();
            }
        }

        public ICommand AddServiceCommand { get; }
        public ICommand EditServiceCommand { get; }
        public ICommand DeleteServiceCommand { get; }

        public ServicesViewModel()
        {
            AddServiceCommand = new RelayCommand(_ => AddService());
            EditServiceCommand = new RelayCommand(_ => EditService(), _ => SelectedService != null);
            DeleteServiceCommand = new RelayCommand(_ => DeleteService(), _ => SelectedService != null);
            LoadServices();
        }

        private void LoadServices()
        {
            Services.Clear();
            foreach (var service in _dataService.Services)
            {
                Services.Add(service);
            }
        }

        private void FilterServices()
        {
            Services.Clear();
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _dataService.Services
                : _dataService.Services.Where(s => 
                    s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var service in filtered)
            {
                Services.Add(service);
            }
        }

        private void AddService()
        {
            var dialog = new EditServiceDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                dialog.Service.Id = _dataService.GetNextId(_dataService.Services);
                _dataService.Services.Add(dialog.Service);
                _dataService.SaveData();
                LoadServices();
            }
        }

        private void EditService()
        {
            if (SelectedService == null) return;
            
            var dialog = new EditServiceDialog(SelectedService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                LoadServices();
            }
        }

        private void DeleteService()
        {
            if (SelectedService == null) return;
            
            var result = MessageBox.Show(
                $"Удалить услугу \"{SelectedService.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _dataService.Services.Remove(SelectedService);
                _dataService.SaveData();
                LoadServices();
            }
        }
    }
}
