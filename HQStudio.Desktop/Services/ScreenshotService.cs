using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис для создания скриншотов окон приложения
    /// </summary>
    public static class ScreenshotService
    {
        public static bool IsScreenshotMode => 
            Environment.GetEnvironmentVariable("SCREENSHOT_MODE") == "true";

        public static string OutputDirectory => 
            Environment.GetEnvironmentVariable("SCREENSHOT_OUTPUT") ?? "screenshots";

        /// <summary>
        /// Делает скриншот указанного окна
        /// </summary>
        public static void CaptureWindow(Window window, string filename)
        {
            if (window == null) return;

            try
            {
                // Убедимся что папка существует
                Directory.CreateDirectory(OutputDirectory);

                // Получаем размеры окна
                var width = (int)window.ActualWidth;
                var height = (int)window.ActualHeight;

                if (width <= 0 || height <= 0) return;

                // Создаём RenderTargetBitmap
                var dpi = VisualTreeHelper.GetDpi(window);
                var renderTarget = new RenderTargetBitmap(
                    (int)(width * dpi.DpiScaleX),
                    (int)(height * dpi.DpiScaleY),
                    dpi.PixelsPerInchX,
                    dpi.PixelsPerInchY,
                    PixelFormats.Pbgra32);

                renderTarget.Render(window);

                // Сохраняем как PNG
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                var filepath = Path.Combine(OutputDirectory, filename);
                using var stream = File.Create(filepath);
                encoder.Save(stream);

                Console.WriteLine($"Screenshot saved: {filepath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot error: {ex.Message}");
            }
        }

        /// <summary>
        /// Делает скриншот с задержкой (для анимаций/прелоадеров)
        /// </summary>
        public static async Task CaptureWindowDelayedAsync(Window window, string filename, int delayMs = 500)
        {
            await Task.Delay(delayMs);
            
            // Выполняем на UI потоке
            await window.Dispatcher.InvokeAsync(() => CaptureWindow(window, filename));
        }

        /// <summary>
        /// Завершает приложение после скриншотов
        /// </summary>
        public static void ExitAfterDelay(int delayMs = 1000)
        {
            Task.Delay(delayMs).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown(0);
                });
            });
        }
    }
}
