/*
 * App.xaml.cs - Application Lifecycle Management
 * 
 * This file contains the code-behind for App.xaml and manages the application lifecycle.
 * - Handles application startup, shutdown, and global events
 * - Can contain application-wide settings and initialization logic
 * - Currently uses default WPF Application behavior (minimal implementation)
 * - Future enhancements could include command line argument processing,
 *   global exception handling, or application-wide configuration
 */

using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DesktopPet;

/// <summary>
/// Application class that manages the desktop pet application lifecycle.
/// Inherits from WPF Application to provide standard application behavior.
/// </summary>
public partial class App : Application
{
    // Attach a global exception handler so unexpected UI thread errors don't close the app
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log detailed error to a file and show a friendly message; keep the app running
        try
        {
            LogError(e.Exception, "DispatcherUnhandledException");
        }
        catch { /* ignore logging failures */ }

        var msg = "Something went wrong, but your pet is okay. We'll keep the current image.";
#if DEBUG
        // In DEBUG, append the exception message for quicker diagnosis
        msg += "\n\nDetails: " + e.Exception.Message;
#endif
        MessageBox.Show(msg, "Desktop Pet", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    internal static void LogError(Exception ex, string context)
    {
        // Write to two locations: app base directory (bin/Debug/..), and LocalAppData
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var basePath = Path.Combine(baseDir, "DesktopPet.log");
        var localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet");
        Directory.CreateDirectory(localFolder);
        var localPath = Path.Combine(localFolder, "DesktopPet.log");

        var lines = new[]
        {
            "==== " + DateTime.Now.ToString("u") + " " + context + " ====",
            ex.ToString(),
            string.Empty
        };

        try { File.AppendAllLines(basePath, lines); } catch { /* ignore */ }
        try { File.AppendAllLines(localPath, lines); } catch { /* ignore */ }
        System.Diagnostics.Debug.WriteLine($"Logged error to {basePath} and {localPath}");
    }

    internal static void LogInfo(string message, string? context = null)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var basePath = Path.Combine(baseDir, "DesktopPet.log");
        var line = (context is null)
            ? ($"[INFO] {DateTime.Now:u} {message}")
            : ($"[INFO] {DateTime.Now:u} {context}: {message}");
        try { File.AppendAllLines(basePath, new[] { line }); } catch { /* ignore */ }
        System.Diagnostics.Debug.WriteLine(line);
    }
}

