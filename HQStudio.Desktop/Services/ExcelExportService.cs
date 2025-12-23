using HQStudio.Models;
using HQStudio.Views.Dialogs;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace HQStudio.Services
{
    public class ExcelExportService
    {
        public void ExportOrdersToExcel(IEnumerable<Order> orders)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"Заказы_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportOrdersToCsv(orders, saveDialog.FileName);
                
                var openFile = ConfirmDialog.Show(
                    "Экспорт завершён",
                    "Заказы успешно экспортированы!\n\nОткрыть файл?",
                    ConfirmDialog.DialogType.Success,
                    "Открыть", "Закрыть");
                
                if (openFile)
                {
                    OpenFile(saveDialog.FileName);
                }
            }
        }

        public void ExportClientsToExcel(IEnumerable<Client> clients)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"Клиенты_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportClientsToCsv(clients, saveDialog.FileName);
                
                var openFile = ConfirmDialog.Show(
                    "Экспорт завершён",
                    "Клиенты успешно экспортированы!\n\nОткрыть файл?",
                    ConfirmDialog.DialogType.Success,
                    "Открыть", "Закрыть");
                
                if (openFile)
                {
                    OpenFile(saveDialog.FileName);
                }
            }
        }

        private void OpenFile(string filePath)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        private void ExportOrdersToCsv(IEnumerable<Order> orders, string filePath)
        {
            var csv = new StringBuilder();
            
            // BOM для корректного отображения в Excel
            csv.Append('\uFEFF');
            
            // Заголовки
            csv.AppendLine("№ заказа;Клиент;Телефон;Автомобиль;Услуги;Сумма;Статус;Дата создания;Дата завершения;Примечания");
            
            foreach (var order in orders)
            {
                var services = order.Services?.Any() == true 
                    ? string.Join(", ", order.Services.Select(s => s.Name))
                    : "";
                
                var line = string.Join(";",
                    Escape(order.Id.ToString()),
                    Escape(order.Client?.Name ?? order.ClientName ?? ""),
                    Escape(order.Client?.Phone ?? ""),
                    Escape(order.Client?.Car ?? ""),
                    Escape(services),
                    Escape(order.TotalPrice.ToString("N0")),
                    Escape(order.Status),
                    Escape(order.CreatedAt.ToString("dd.MM.yyyy HH:mm")),
                    Escape(order.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? ""),
                    Escape(order.Notes ?? "")
                );
                
                csv.AppendLine(line);
            }
            
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }

        private void ExportClientsToCsv(IEnumerable<Client> clients, string filePath)
        {
            var csv = new StringBuilder();
            
            // BOM для корректного отображения в Excel
            csv.Append('\uFEFF');
            
            // Заголовки
            csv.AppendLine("ID;Имя;Телефон;Автомобиль;Гос. номер;Дата добавления;Примечания");
            
            foreach (var client in clients)
            {
                var line = string.Join(";",
                    Escape(client.Id.ToString()),
                    Escape(client.Name),
                    Escape(client.Phone),
                    Escape(client.Car),
                    Escape(client.CarNumber),
                    Escape(client.CreatedAt.ToString("dd.MM.yyyy HH:mm")),
                    Escape(client.Notes)
                );
                
                csv.AppendLine(line);
            }
            
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }
        
        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            
            if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }
    }
}
