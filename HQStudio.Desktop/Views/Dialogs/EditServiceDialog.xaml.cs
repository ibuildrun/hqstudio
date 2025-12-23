using HQStudio.Models;
using HQStudio.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditServiceDialog : Window
    {
        public Service Service { get; private set; }
        public bool IsNew { get; }
        private string _selectedIcon = "🔧";
        private bool _iconManuallySelected = false;

        public EditServiceDialog(Service? service = null)
        {
            InitializeComponent();
            IsNew = service == null;
            Service = service ?? new Service { Icon = "🔧" };
            _selectedIcon = Service.Icon;
            
            TitleText.Text = IsNew ? "Новая услуга" : "Редактирование услуги";
            
            // Загружаем иконки в панель выбора
            IconsGrid.ItemsSource = ServiceIcons.Icons;
            
            LoadData();
            
            Loaded += (s, e) => NameBox.Focus();
        }

        private void LoadData()
        {
            SelectedIconDisplay.Text = Service.Icon;
            _selectedIcon = Service.Icon;
            NameBox.Text = Service.Name;
            CategoryBox.Text = Service.Category;
            PriceBox.Text = Service.PriceFrom > 0 ? Service.PriceFrom.ToString() : "";
            DescriptionBox.Text = Service.Description;
            
            // Если редактируем существующую услугу, считаем что иконка выбрана вручную
            if (!IsNew && !string.IsNullOrEmpty(Service.Icon))
            {
                _iconManuallySelected = true;
            }
        }

        private void SelectIcon_Click(object sender, RoutedEventArgs e)
        {
            IconPickerPanel.Visibility = IconPickerPanel.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        private void IconItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string icon)
            {
                _selectedIcon = icon;
                _iconManuallySelected = true;
                SelectedIconDisplay.Text = icon;
                IconPickerPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Автоматически подбираем иконку только если пользователь не выбрал вручную
            if (!_iconManuallySelected && IsNew)
            {
                var recommendedIcon = ServiceIcons.GetRecommendedIcon(NameBox.Text);
                _selectedIcon = recommendedIcon;
                SelectedIconDisplay.Text = recommendedIcon;
            }
        }

        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            InputValidation.AllowDecimalNumbers(sender, e);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Валидация названия
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                InputValidation.ShowValidationError("Введите название услуги", NameBox);
                return;
            }

            // Валидация цены
            if (!string.IsNullOrWhiteSpace(PriceBox.Text) && !InputValidation.IsValidPrice(PriceBox.Text))
            {
                InputValidation.ShowValidationError("Введите корректную цену (только цифры)", PriceBox);
                return;
            }

            Service.Icon = _selectedIcon;
            Service.Name = NameBox.Text.Trim();
            Service.Category = CategoryBox.Text.Trim();
            Service.Description = DescriptionBox.Text.Trim();
            
            if (decimal.TryParse(PriceBox.Text.Replace(" ", "").Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                Service.PriceFrom = price;
            }

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
                if (IconPickerPanel.Visibility == Visibility.Visible)
                {
                    IconPickerPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Cancel_Click(sender, e);
                }
            }
        }
    }
}
