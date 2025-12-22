using HQStudio.Models;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HQStudio.Services
{
    public class PrintService
    {
        public void PrintOrder(Order order)
        {
            var printDialog = new PrintDialog();
            
            if (printDialog.ShowDialog() == true)
            {
                var document = CreateDocument(order, printDialog.PrintableAreaWidth);
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Заказ #{order.Id}");
            }
        }
        
        private FlowDocument CreateDocument(Order order, double pageWidth)
        {
            var doc = new FlowDocument
            {
                PageWidth = pageWidth,
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };
            
            // Заголовок
            var title = new Paragraph(new Run("ЗАКАЗ НА ОБСЛУЖИВАНИЕ"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(title);
            
            // Номер и дата
            var header = new Paragraph();
            header.Inlines.Add(new Run($"Заказ №{order.Id}") { FontWeight = FontWeights.Bold, FontSize = 14 });
            header.Inlines.Add(new Run($"    Дата: {order.CreatedAt:dd.MM.yyyy HH:mm}"));
            header.Margin = new Thickness(0, 0, 0, 10);
            doc.Blocks.Add(header);
            
            // Разделитель
            doc.Blocks.Add(new Paragraph(new Run("─────────────────────────────────────────────────────────────────"))
            {
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15)
            });
            
            // Клиент
            var clientSection = new Paragraph();
            clientSection.Inlines.Add(new Run("КЛИЕНТ:\n") { FontWeight = FontWeights.Bold, FontSize = 13 });
            
            var clientName = order.Client?.Name ?? order.ClientName ?? "Не указано";
            clientSection.Inlines.Add(new Run($"   Имя: {clientName}\n"));
            
            if (!string.IsNullOrEmpty(order.Client?.Phone))
                clientSection.Inlines.Add(new Run($"   Телефон: {order.Client.Phone}\n"));
            
            if (!string.IsNullOrEmpty(order.Client?.Car))
                clientSection.Inlines.Add(new Run($"   Автомобиль: {order.Client.Car}\n"));
            
            if (!string.IsNullOrEmpty(order.Client?.CarNumber))
                clientSection.Inlines.Add(new Run($"   Гос. номер: {order.Client.CarNumber}\n"));
            
            clientSection.Margin = new Thickness(0, 0, 0, 15);
            doc.Blocks.Add(clientSection);
            
            // Услуги
            var servicesSection = new Paragraph();
            servicesSection.Inlines.Add(new Run("УСЛУГИ:\n") { FontWeight = FontWeights.Bold, FontSize = 13 });
            
            if (order.Services?.Any() == true)
            {
                foreach (var service in order.Services)
                {
                    servicesSection.Inlines.Add(new Run($"   • {service.Name} — {service.PriceFrom:N0} ₽\n"));
                }
            }
            else
            {
                servicesSection.Inlines.Add(new Run("   Услуги не указаны\n") { Foreground = Brushes.Gray });
            }
            
            servicesSection.Margin = new Thickness(0, 0, 0, 10);
            doc.Blocks.Add(servicesSection);
            
            // Итого
            var totalSection = new Paragraph();
            totalSection.Inlines.Add(new Run("ИТОГО: ") { FontWeight = FontWeights.Bold, FontSize = 14 });
            totalSection.Inlines.Add(new Run($"{order.TotalPrice:N0} ₽") { FontWeight = FontWeights.Bold, FontSize = 14 });
            totalSection.Margin = new Thickness(0, 0, 0, 15);
            doc.Blocks.Add(totalSection);
            
            // Статус
            doc.Blocks.Add(new Paragraph(new Run($"Статус: {order.Status}"))
            {
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Примечания
            if (!string.IsNullOrEmpty(order.Notes))
            {
                var notesSection = new Paragraph();
                notesSection.Inlines.Add(new Run("ПРИМЕЧАНИЯ:\n") { FontWeight = FontWeights.Bold });
                notesSection.Inlines.Add(new Run($"   {order.Notes}"));
                notesSection.Margin = new Thickness(0, 0, 0, 30);
                doc.Blocks.Add(notesSection);
            }
            
            // Подписи
            doc.Blocks.Add(new Paragraph(new Run("\n\nПодпись клиента: ________________________"))
            {
                Margin = new Thickness(0, 30, 0, 10)
            });
            doc.Blocks.Add(new Paragraph(new Run("Подпись мастера: ________________________"))
            {
                Margin = new Thickness(0, 0, 0, 30)
            });
            
            // Футер
            doc.Blocks.Add(new Paragraph(new Run($"Документ создан {DateTime.Now:dd.MM.yyyy HH:mm}"))
            {
                FontSize = 9,
                Foreground = Brushes.Gray
            });
            
            return doc;
        }
    }
}
