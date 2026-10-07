using System.Diagnostics;
using System.Windows;

namespace HQStudio.Services.BugReport
{
    /// <summary>
    /// Действия с окружением Windows, вынесенные в интерфейс, чтобы ViewModel не зависела от буфера обмена и браузера.
    /// </summary>
    public interface IShellActions
    {
        void OpenUrl(string url);
        bool CopyToClipboard(string text);
    }

    public sealed class SystemShellActions : IShellActions
    {
        public void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public bool CopyToClipboard(string text)
        {
            try
            {
                // copy: true оставляет текст в буфере и после закрытия приложения.
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch
            {
                // Буфер может быть занят другой программой.
                return false;
            }
        }
    }
}
