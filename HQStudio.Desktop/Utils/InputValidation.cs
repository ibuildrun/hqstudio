using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Utils
{
    /// <summary>
    /// Утилиты для валидации ввода в текстовые поля
    /// </summary>
    public static class InputValidation
    {
        /// <summary>
        /// Разрешает ввод только цифр
        /// </summary>
        public static void AllowOnlyDigits(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>
        /// Разрешает ввод цифр и одной точки/запятой (для десятичных чисел)
        /// </summary>
        public static void AllowDecimalNumbers(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
                // Разрешаем цифры, одну точку или запятую
                e.Handled = !Regex.IsMatch(newText, @"^\d*[.,]?\d*$");
            }
        }

        /// <summary>
        /// Разрешает ввод телефонного номера (+, цифры, пробелы, дефисы, скобки)
        /// </summary>
        public static void AllowPhoneCharacters(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[\d\+\-\(\)\s]+$");
        }

        /// <summary>
        /// Разрешает ввод гос. номера (буквы, цифры)
        /// </summary>
        public static void AllowLicensePlateCharacters(object sender, TextCompositionEventArgs e)
        {
            // Русские и латинские буквы + цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^[А-Яа-яA-Za-z0-9]+$");
        }

        /// <summary>
        /// Запрещает вставку некорректных данных
        /// </summary>
        public static void PreventInvalidPaste(object sender, DataObjectPastingEventArgs e, Func<string, bool> validator)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!validator(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Валидация email
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true; // Пустой email допустим
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Валидация телефона (минимум 10 цифр)
        /// </summary>
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var digitsOnly = Regex.Replace(phone, @"\D", "");
            return digitsOnly.Length >= 10;
        }

        /// <summary>
        /// Валидация цены (положительное число)
        /// </summary>
        public static bool IsValidPrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return true; // Пустая цена допустима
            var normalized = price.Replace(",", ".").Replace(" ", "");
            return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0;
        }

        /// <summary>
        /// Показывает ошибку валидации
        /// </summary>
        public static void ShowValidationError(string message, TextBox? focusElement = null)
        {
            MessageBox.Show(message, "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            focusElement?.Focus();
        }
    }
}
