using HQStudio.Models;
using HQStudio.Utils;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditClientDialog : Window
    {
        public Client Client { get; private set; }
        public bool IsNew { get; }

        public EditClientDialog(Client? client = null)
        {
            InitializeComponent();
            IsNew = client == null;
            Client = client ?? new Client();
            
            TitleText.Text = IsNew ? "Новый клиент" : "Редактирование клиента";
            LoadData();
            
            Loaded += (s, e) => NameBox.Focus();
        }

        private void LoadData()
        {
            NameBox.Text = Client.Name;
            PhoneBox.Text = Client.Phone;
            CarBox.Text = Client.Car;
            CarNumberBox.Text = Client.CarNumber;
            NotesBox.Text = Client.Notes;
        }

        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            InputValidation.AllowPhoneCharacters(sender, e);
        }

        private void CarNumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            InputValidation.AllowLicensePlateCharacters(sender, e);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Валидация имени
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                InputValidation.ShowValidationError("Введите имя клиента", NameBox);
                return;
            }

            // Валидация телефона
            if (!string.IsNullOrWhiteSpace(PhoneBox.Text) && !InputValidation.IsValidPhone(PhoneBox.Text))
            {
                InputValidation.ShowValidationError("Введите корректный номер телефона (минимум 10 цифр)", PhoneBox);
                return;
            }

            Client.Name = NameBox.Text.Trim();
            Client.Phone = PhoneBox.Text.Trim();
            Client.Car = CarBox.Text.Trim();
            Client.CarNumber = CarNumberBox.Text.Trim().ToUpper();
            Client.Notes = NotesBox.Text.Trim();

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
