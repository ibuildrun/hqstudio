using HQStudio.Models;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditServiceDialog : Window
    {
        public Service Service { get; private set; }
        public bool IsNew { get; }

        public EditServiceDialog(Service? service = null)
        {
            InitializeComponent();
            IsNew = service == null;
            Service = service ?? new Service { Icon = "🔧" };
            
            TitleText.Text = IsNew ? "Новая услуга" : "Редактирование услуги";
            LoadData();
            
            Loaded += (s, e) => NameBox.Focus();
        }

        private void LoadData()
        {
            IconBox.Text = Service.Icon;
            NameBox.Text = Service.Name;
            CategoryBox.Text = Service.Category;
            PriceBox.Text = Service.PriceFrom.ToString();
            DescriptionBox.Text = Service.Description;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название услуги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            Service.Icon = string.IsNullOrEmpty(IconBox.Text) ? "🔧" : IconBox.Text;
            Service.Name = NameBox.Text.Trim();
            Service.Category = CategoryBox.Text.Trim();
            Service.Description = DescriptionBox.Text.Trim();
            
            if (decimal.TryParse(PriceBox.Text, out var price))
                Service.PriceFrom = price;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Cancel_Click(sender, e);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
