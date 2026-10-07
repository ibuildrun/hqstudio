using System.Runtime.InteropServices;
using System.Windows;
using HQStudio.Setup.Core;
using HQStudio.Setup.Rendering;
using HQStudio.Setup.Services;
using HQStudio.Setup.UI;

namespace HQStudio.Setup;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        UiDispatcher.Initialize(Dispatcher);
        DispatcherUnhandledException += OnUnhandledException;

        var options = SetupOptions.Parse(e.Args);

        if (options.RenderPagesDir != null)
        {
            Shutdown(PageRenderer.RenderAll(options.RenderPagesDir));
            return;
        }

        var mutexName = options.Simulate ? "Local\\HQStudio.Setup.Simulation" : "Local\\HQStudio.Setup";
        _singleInstance = new Mutex(true, mutexName, out var isFirst);
        if (!isFirst)
        {
            BringExistingWindowToFront();
            Shutdown(0);
            return;
        }

        var services = options.Simulate
            ? ServiceFactory.CreateSimulated(options, _ => { })
            : ServiceFactory.CreateReal(options);

        MainWindow? window = null;
        var vm = new WizardViewModel(services, options, () => window?.Close(), CopyText);
        window = new MainWindow(vm);
        window.Closed += (_, _) => Shutdown(0);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _singleInstance?.ReleaseMutex(); } catch (ApplicationException) { }
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static void CopyText(string text)
    {
        try { Clipboard.SetText(text); }
        catch (COMException) { }
    }

    private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var dir = InstallPaths.ForCurrentUser().DataDir;
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "setup-crash.log"), $"[{DateTime.Now:s}] {e.Exception}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        MessageBox.Show(
            "В установщике произошла непредвиденная ошибка. Закройте окно и запустите установку ещё раз. Подробности записаны в файл setup-crash.log в папке HQStudio.",
            "HQ Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
        Shutdown(1);
    }

    private static void BringExistingWindowToFront()
    {
        var handle = FindWindow(null, "Установка HQ Studio");
        if (handle == IntPtr.Zero)
            return;
        ShowWindow(handle, 9); // SW_RESTORE
        SetForegroundWindow(handle);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
