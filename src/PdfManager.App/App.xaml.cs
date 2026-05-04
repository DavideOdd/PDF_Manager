using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PdfManager.App.ViewModels;
using PdfManager.Core.Services;

namespace PdfManager.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "GestorePDF_crash.txt");

    // Runs before anything — catches crashes during XAML init / CLR load
    static App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log($"[FATAL] {e.ExceptionObject}");
            try { MessageBox.Show(e.ExceptionObject?.ToString(), "Errore critico"); } catch { }
        };
        Log("=== App static ctor ===");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log("OnStartup BEGIN");

        DispatcherUnhandledException += (_, ex) =>
        {
            Log($"[UI] {ex.Exception}");
            MessageBox.Show($"{ex.Exception.GetType().Name}:\n{ex.Exception.Message}\n\nLog: {LogPath}",
                "Errore UI", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            ex.SetObserved();
            Log($"[Task] {ex.Exception}");
        };

        try
        {
            Log("Registering encodings...");
            PdfSharpInit.EnsureRegistered();

            Log("Building DI...");
            Services = Bootstrapper.Build();

            Log("Creating MainWindow...");
            var main = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            main.Show();
            Log("MainWindow shown OK");

            var vm = (MainViewModel)main.DataContext;
            foreach (var arg in e.Args)
                if (arg.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    _ = vm.OpenFileAsync(arg);
        }
        catch (Exception ex)
        {
            Log($"[OnStartup CATCH] {ex}");
            MessageBox.Show($"Errore avvio:\n{ex.Message}\n\nLog: {LogPath}",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    internal static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}
